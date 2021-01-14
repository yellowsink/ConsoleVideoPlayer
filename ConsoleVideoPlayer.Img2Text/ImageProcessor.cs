using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class ImageProcessor
	{
		/// <summary>
		///     The path of the image in use
		/// </summary>
		public string ImagePath { get; init; }

		/// <summary>
		///     Gets the colour of the pixel at the specified coordinates
		/// </summary>
		public Color GetColourFromPixelCoordinate(int x, int y)
		{
			var img    = new MagickImage(ImagePath);
			var pixels = img.GetPixels();
			var pixel  = pixels.GetPixel(x, y);
			return ColourFromPixel(pixel);
		}

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

		/// <summary>
		///     Averages a set of colours
		/// </summary>
		public Color AverageColours(IEnumerable<Color> colours)
		{
			var colourArray = colours.ToArray();
			var alphaTotal = colourArray.Select(c => Convert.ToInt32(c.A))
			                            .Aggregate(0, (current, item) => current + item);
			var redTotal = colourArray.Select(c => Convert.ToInt32(c.R))
			                          .Aggregate(0, (current, item) => current + item);
			var greenTotal = colourArray.Select(c => Convert.ToInt32(c.G))
			                            .Aggregate(0, (current, item) => current + item);
			var blueTotal = colourArray.Select(c => Convert.ToInt32(c.B))
			                           .Aggregate(0, (current, item) => current + item);

			var alphaAverage = alphaTotal / colourArray.Length;
			var redAverage   = redTotal   / colourArray.Length;
			var greenAverage = greenTotal / colourArray.Length;
			var blueAverage  = blueTotal  / colourArray.Length;

			return Color.FromArgb(alphaAverage, redAverage, greenAverage, blueAverage);
		}

		/// <summary>
		///     Gets the average colour of all pixels in an area
		/// </summary>
		/// <param name="startX">The inclusive area start X coordinate</param>
		/// <param name="startY">The inclusive area start Y coordinate</param>
		/// <param name="endX">The exclusive area end X coordinate</param>
		/// <param name="endY">The exclusive area end Y coordinate</param>
		/// <returns>The average colour of all pixels in that area</returns>
		public Color AverageColourInArea(int startX, int startY, int endX, int endY)
		{
			var img    = new MagickImage(ImagePath);
			var pixels = img.GetPixels();
			var pixelsInArea = pixels.Where(p =>
				                                p.X >= startX
				                             && p.X < endX
				                             && p.Y >= startY
				                             && p.Y < endY);

			return AverageColours(pixelsInArea.Select(ColourFromPixel).ToList());
		}
	}
}