using QoiSharp;

namespace ConsoleVideoPlayer.MediaProcessor;

public struct Color
{
	public readonly byte Red   = 0;
	public readonly byte Green = 0;
	public readonly byte Blue  = 0;

	public Color() { }

	public Color(byte red, byte green, byte blue)
	{
		Red   = red;
		Green = green;
		Blue  = blue;
	}

	public static Color Empty = new(0, 0, 0);

	public static bool operator ==(Color first, Color second)
		=> first.Red == second.Red && first.Blue == second.Blue && first.Green == second.Green;

	public static bool operator !=(Color first, Color second) => !(first == second);
}

public class PixelLookup
{
	public PixelLookup(string path)
	{
		var qoiImg = QoiDecoder.Decode(File.ReadAllBytes(path));

		Width  = qoiImg.Width;
		Height = qoiImg.Height;
		
		_pixels = new Color[qoiImg.Height * qoiImg.Width];

		var j = 0;

		for (var i = 0; i < qoiImg.Data.Length; i += (int) qoiImg.Channels)
			_pixels[j++] = new Color(qoiImg.Data[i], qoiImg.Data[i + 1], qoiImg.Data[i + 2]);
	}

	private readonly Color[] _pixels;
	public readonly  int     Width;
	public readonly  int     Height;

	public Color AtCoord(int x, int y) => _pixels[Width * y + x];
}