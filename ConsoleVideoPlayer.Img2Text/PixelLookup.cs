using SkiaSharp;

namespace ConsoleVideoPlayer.Img2Text;

public class PixelLookup
{
	public PixelLookup(string path) => _image = SKBitmap.Decode(path);

	private readonly SKBitmap _image;

	public (int width, int height) Dimensions => (_image.Width, _image.Height);

	private SKColor[]? _pixelsCache;
	private SKColor[]  Pixels => _pixelsCache ??= _image.Pixels;

	public SKColor ColourAtCoord(int x, int y) => Pixels[_image.Width * y + x];
}