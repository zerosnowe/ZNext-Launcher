using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ZNext.Services;

internal static class DpiAwareImageSourceFactory
{
	private const double DefaultRasterizationScale = 2.0;

	// Cache for recently loaded images to avoid repeated decoding
	private static readonly Dictionary<string, WeakReference<SoftwareBitmapSource>> _imageCache = new();
	private static readonly object _cacheLock = new();

	public static async Task<SoftwareBitmapSource?> CreateSquareThumbnailAsync(
		string filePath,
		FrameworkElement targetElement,
		double fallbackLogicalSize)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			return null;
		}

		int pixelSize = ResolvePhysicalPixelSize(targetElement, fallbackLogicalSize);
		FileInfo fileInfo = new FileInfo(filePath);
		string cacheKey = $"{fileInfo.FullName}:{fileInfo.LastWriteTimeUtc.Ticks}:{fileInfo.Length}:{pixelSize}";

		lock (_cacheLock)
		{
			if (_imageCache.TryGetValue(cacheKey, out WeakReference<SoftwareBitmapSource>? weakRef)
				&& weakRef.TryGetTarget(out SoftwareBitmapSource? cachedSource)
				&& cachedSource != null)
			{
				return cachedSource;
			}
		}

		using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using IRandomAccessStream randomAccessStream = fileStream.AsRandomAccessStream();
		BitmapDecoder decoder = await BitmapDecoder.CreateAsync(randomAccessStream).AsTask();

		uint sourceWidth = decoder.PixelWidth;
		uint sourceHeight = decoder.PixelHeight;
		if (sourceWidth == 0 || sourceHeight == 0)
		{
			return null;
		}

		uint cropSize = Math.Min(sourceWidth, sourceHeight);
		BitmapTransform transform = new BitmapTransform
		{
			Bounds = new BitmapBounds
			{
				X = (sourceWidth - cropSize) / 2,
				Y = (sourceHeight - cropSize) / 2,
				Width = cropSize,
				Height = cropSize
			},
			ScaledWidth = (uint)pixelSize,
			ScaledHeight = (uint)pixelSize,
			InterpolationMode = BitmapInterpolationMode.Fant
		};

		SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
			BitmapPixelFormat.Bgra8,
			BitmapAlphaMode.Premultiplied,
			transform,
			ExifOrientationMode.RespectExifOrientation,
			ColorManagementMode.ColorManageToSRgb).AsTask();

		// SoftwareBitmapSource must be created on UI thread
		SoftwareBitmapSource source = new SoftwareBitmapSource();
		await source.SetBitmapAsync(softwareBitmap);

		// Add to cache
		lock (_cacheLock)
		{
			_imageCache[cacheKey] = new WeakReference<SoftwareBitmapSource>(source);
			// Clean up old entries if cache grows too large
			if (_imageCache.Count > 20)
			{
				var keysToRemove = _imageCache.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();
				foreach (var key in keysToRemove)
				{
					_imageCache.Remove(key);
				}
			}
		}

		return source;
	}

	private static int ResolvePhysicalPixelSize(FrameworkElement element, double fallbackLogicalSize)
	{
		double logicalSize = ResolveLogicalSize(element.ActualWidth, element.Width, fallbackLogicalSize);
		double rasterizationScale = element.XamlRoot?.RasterizationScale ?? DefaultRasterizationScale;
		return Math.Max(1, (int)Math.Ceiling(logicalSize * rasterizationScale));
	}

	private static double ResolveLogicalSize(double actualSize, double requestedSize, double fallbackSize)
	{
		if (actualSize > 0)
		{
			return actualSize;
		}
		if (!double.IsNaN(requestedSize) && requestedSize > 0)
		{
			return requestedSize;
		}

		return fallbackSize;
	}
}
