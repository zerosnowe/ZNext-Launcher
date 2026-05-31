using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ZNext.Services;

internal sealed class ProcessLifetimeService : IDisposable
{
	private const uint TH32CS_SNAPPROCESS = 2u;
	private const int JobObjectExtendedLimitInformation = 9;
	private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 8192u;
	private static readonly nint InvalidHandleValue = new IntPtr(-1);

	private nint _jobHandle;

	public void Initialize()
	{
		try
		{
			if (_jobHandle != nint.Zero)
			{
				return;
			}

			nint jobHandle = CreateJobObject(nint.Zero, null);
			if (jobHandle == nint.Zero)
			{
				return;
			}

			JOBOBJECT_EXTENDED_LIMIT_INFORMATION info = new();
			info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
			int infoLength = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
			nint infoPtr = Marshal.AllocHGlobal(infoLength);
			try
			{
				Marshal.StructureToPtr(info, infoPtr, fDeleteOld: false);
				if (!SetInformationJobObject(jobHandle, JobObjectExtendedLimitInformation, infoPtr, (uint)infoLength))
				{
					CloseHandle(jobHandle);
					return;
				}
			}
			finally
			{
				Marshal.FreeHGlobal(infoPtr);
			}

			_jobHandle = jobHandle;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ProcessLifetimeService.Initialize failed: " + ex.Message);
		}
	}

	public void TryAssign(Process? process)
	{
		try
		{
			if (_jobHandle == nint.Zero || process == null || process.HasExited)
			{
				return;
			}

			AssignProcessToJobObject(_jobHandle, process.Handle);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("ProcessLifetimeService.TryAssign failed: " + ex.Message);
		}
	}

	public bool TryTerminateChildProcesses(Process? process)
	{
		if (process == null || process.HasExited)
		{
			return false;
		}

		bool result = false;
		foreach (int childProcessId in GetChildProcessIds(process.Id))
		{
			try
			{
				using Process childProcess = Process.GetProcessById(childProcessId);
				if (!childProcess.HasExited)
				{
					childProcess.Kill(entireProcessTree: true);
					result = true;
				}
			}
			catch
			{
			}
		}

		return result;
	}

	public void Dispose()
	{
		if (_jobHandle == nint.Zero)
		{
			return;
		}

		CloseHandle(_jobHandle);
		_jobHandle = nint.Zero;
	}

	private static List<int> GetChildProcessIds(int parentPid)
	{
		List<int> result = new();
		nint snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0u);
		if (snapshot == IntPtr.Zero || snapshot == InvalidHandleValue)
		{
			return result;
		}

		try
		{
			PROCESSENTRY32 entry = new()
			{
				dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>()
			};
			if (!Process32First(snapshot, ref entry))
			{
				return result;
			}

			do
			{
				if (entry.th32ParentProcessID == (uint)parentPid && entry.th32ProcessID != 0)
				{
					result.Add((int)entry.th32ProcessID);
				}
			}
			while (Process32Next(snapshot, ref entry));
		}
		finally
		{
			CloseHandle(snapshot);
		}

		return result;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct PROCESSENTRY32
	{
		public uint dwSize;
		public uint cntUsage;
		public uint th32ProcessID;
		public nint th32DefaultHeapID;
		public uint th32ModuleID;
		public uint cntThreads;
		public uint th32ParentProcessID;
		public int pcPriClassBase;
		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExeFile;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public long PerProcessUserTimeLimit;
		public long PerJobUserTimeLimit;
		public uint LimitFlags;
		public nuint MinimumWorkingSetSize;
		public nuint MaximumWorkingSetSize;
		public uint ActiveProcessLimit;
		public nint Affinity;
		public uint PriorityClass;
		public uint SchedulingClass;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct IO_COUNTERS
	{
		public ulong ReadOperationCount;
		public ulong WriteOperationCount;
		public ulong OtherOperationCount;
		public ulong ReadTransferCount;
		public ulong WriteTransferCount;
		public ulong OtherTransferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
		public IO_COUNTERS IoInfo;
		public nuint ProcessMemoryLimit;
		public nuint JobMemoryLimit;
		public nuint PeakProcessMemoryUsed;
		public nuint PeakJobMemoryUsed;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool Process32First(nint hSnapshot, ref PROCESSENTRY32 lppe);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool Process32Next(nint hSnapshot, ref PROCESSENTRY32 lppe);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateJobObject(nint lpJobAttributes, string? lpName);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetInformationJobObject(nint hJob, int jobObjectInfoClass, nint lpJobObjectInfo, uint cbJobObjectInfoLength);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool AssignProcessToJobObject(nint hJob, nint hProcess);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool CloseHandle(nint hObject);
}
