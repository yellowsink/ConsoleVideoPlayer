using SkiaSharp;

namespace ConsoleVideoPlayer.MediaProcessor;

public class PixelLookup
{
	private readonly SKBitmap _image;

	private SKColor[]? _pixelCache;
	public PixelLookup(string path) { _image = SKBitmap.Decode(path); }

	public SKColor[] Pixels => _pixelCache ??= _image.Pixels;
	public int       Width  => _image.Width;
	public int       Height => _image.Height;

	public SKColor AtCoord(int x, int y) => Pixels[Width * y + x];
}