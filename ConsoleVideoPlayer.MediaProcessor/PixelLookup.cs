using QoiSharp;

namespace ConsoleVideoPlayer.MediaProcessor;

internal class PixelLookup
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