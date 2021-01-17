using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class Converter
	{
		public static readonly char[] IntensityChars = {' ', '░', '▒', '▓', '█'};

		public string ImagePath { get; init; }

		public KeyValuePair<Coordinate, ColouredCharacter>[] FullProcessImage(int targetWidth, int targetHeight)
			=> ColoursToCharacters(GetColoursOfFrame(targetWidth, targetHeight));

		/// <summary>
		///     Gets the colours of the frame
		/// </summary>
		public KeyValuePair<Coordinate, Color>[] GetColoursOfFrame(int? targetWidth = null, int? targetHeight = null)
		{
			var img = new MagickImage(ImagePath);
			targetWidth  ??= img.BaseWidth;
			targetHeight ??= img.BaseHeight;
			var processor    = new ImageProcessor {Image = new MagickImage(ImagePath)};
			var resizedImage = ResizeImage(targetWidth.Value, targetHeight.Value, processor);

			var working = new List<KeyValuePair<Coordinate, Color>>();
			for (var y = 0; y < targetHeight; y++)
			for (var x = 0; x < targetWidth; x++)
				working.Add(new KeyValuePair<Coordinate, Color>(new Coordinate(x, y),
				                                                resizedImage.ColourFromPixelCoordinate(x, y)));

			return working.ToArray();
		}

		public void WriteResizedImage(int    targetWidth, int targetHeight, string outDir,
		                              string fileName = null)
		{
			Directory.CreateDirectory(outDir);

			var imgProc = new ImageProcessor {Image = new MagickImage(ImagePath)};

			fileName ??= new FileInfo(imgProc.Image.FileName).Name;

			var resized = ResizeImage(targetWidth, targetHeight,
			                          imgProc);
			resized.Image.Write(Path.Combine(outDir, fileName));
		}

		private ImageProcessor ResizeImage(int targetWidth, int targetHeight, ImageProcessor processor)
		{
			var resizedImage = new ImageProcessor
				{Image = processor.ResizeImage(targetWidth, targetHeight)};
			return resizedImage;
		}

		/// <summary>
		///     Based on the intensity of each color in the provided set gets a suitable character from IntensityChars for each
		/// </summary>
		public KeyValuePair<Coordinate, ColouredCharacter>[] ColoursToCharacters(
			IEnumerable<KeyValuePair<Coordinate, Color>> colours)
		{
			var working = new List<KeyValuePair<Coordinate, ColouredCharacter>>();
			foreach (var (coordinate, colour) in colours)
			{
				var intensity = (colour.R + colour.G + colour.B) / 3;

				var intensityScaleFactor = 255 / IntensityChars.Length;
				// ReSharper disable once PossibleLossOfFraction
				var scaledIntensity       = (int) Math.Round((double) (intensity / intensityScaleFactor));
				var offsetScaledIntensity = scaledIntensity - 1;
				var limitedIntensity      = Math.Min(Math.Max(offsetScaledIntensity, 0), IntensityChars.Length - 1);
				var intensityChar         = IntensityChars[limitedIntensity];

				working.Add(new KeyValuePair<Coordinate, ColouredCharacter>(coordinate, new ColouredCharacter
				{
					Character = intensityChar,
					Colour    = colour.ClosestConsoleColor()
				}));
			}

			return working.ToArray();
		}
	}

	public class ColouredCharacter
	{
		public char         Character;
		public ConsoleColor Colour;
	}

	public class Coordinate
	{
		public int X;
		public int Y;

		public Coordinate(int x, int y)
		{
			X = x;
			Y = y;
		}
	}
}