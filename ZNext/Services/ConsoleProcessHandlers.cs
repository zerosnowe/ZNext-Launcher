using System;
using System.Diagnostics;

namespace ZNext.Services;

internal readonly record struct ConsoleProcessHandlers(
	DataReceivedEventHandler OutputReceived,
	DataReceivedEventHandler ErrorReceived,
	EventHandler Exited);
