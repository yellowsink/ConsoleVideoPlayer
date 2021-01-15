using System.Drawing;
using System.Linq;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class ImageProcessor
	{
		private Color[] _pixels; // Keep this in memory rather than fetching it each time.

		/// <summary>
		///     The path of the image in use
		/// </summary>
		public MagickImage Image { get; init; }

		/// <summary>
		///     Gets the colour of the pixel at the specified coordinates
		/// </summary>
		public Color ColourFromPixelCoordinate(int x, int y)
		{
			PopulatePixelsIfEmpty();
			var pixel = _pixels[CoordinateToIndex(x, y, Image.Width)];
			return pixel;
		}

		public void PopulatePixelsIfEmpty() => _pixels ??= Image.GetPixels().Select(ColourFromPixel).ToArray();

		private static int CoordinateToIndex(int x, int y, int imageWidth) => x / imageWidth + y + x % imageWidth;

		/// <summary>
		///     Gets the colour of an ImageMagick pixel
		/// </summary>
		private static Color ColourFromPixel(IPixel<ushort> pixel)
		{
			var colour = pixel.ToColor();

			int ScaleColour(ushort unscaled)
			{
				var scaleFactor = ushort.MaxValue / 255;
				return unscaled / scaleFactor;
			}

			return Color.FromArgb(ScaleColour(colour.A),
			                      ScaleColour(colour.R),
			                      ScaleColour(colour.G),
			                      ScaleColour(colour.B));
		}

		public MagickImage ResizeImage(int targetWidth, int targetHeight)
		{
			Image.Scale(new MagickGeometry(targetWidth, targetHeight));
			return Image;
		}
	}
}