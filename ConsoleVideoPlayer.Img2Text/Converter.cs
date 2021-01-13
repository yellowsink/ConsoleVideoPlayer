using System;
using System.Collections.Generic;
using System.Drawing;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class Converter
	{
		public static readonly char[] IntensityChars = {' ', '░', '▓', '▒', '█'};

		public string ImagePath { get; init; }

		private BlockSize CalculateBlockSize(int targetWidth, int targetHeight)
		{
			var img           = new MagickImage(ImagePath);
			var currentWidth  = img.BaseWidth;
			var currentHeight = img.BaseHeight;

			var residualWidth  = currentWidth  % targetWidth;  // residual pixels - if there's one pixel at the bottom
			var residualHeight = currentHeight % targetHeight; // that doesn't divide it gets added to the last block

			var blockWidth  = currentWidth  / targetWidth;
			var blockHeight = currentHeight / targetHeight;

			return new BlockSize
			{
				BaseWidth  = blockWidth,
				BaseHeight = blockHeight,
				LastWidth  = blockWidth  + residualWidth,
				LastHeight = blockHeight + residualHeight
			};
		}

		/// <summary>
		///     Gets the colours of the frame
		/// </summary>
		public KeyValuePair<Coordinate, Color>[] GetColoursOfFrame(int? targetWidth = null, int? targetHeight = null)
		{
			var img = new MagickImage(ImagePath);
			targetWidth  ??= img.BaseWidth;
			targetHeight ??= img.BaseWidth;
			var blockSize = CalculateBlockSize(targetWidth.Value, targetHeight.Value);
			var processor = new ImageProcessor {ImagePath = ImagePath};

			var working = new List<KeyValuePair<Coordinate, Color>>();
			for (var i = 0; i < targetWidth; i++)
			for (var j = 0; j < targetHeight; j++)
			{
				var lastX       = i + 1 == targetWidth; // these 4 lines calculate logic for catching residual pixels
				var lastY       = j + 1 == targetHeight;
				var blockWidth  = lastX ? blockSize.BaseWidth : blockSize.LastWidth;
				var blockHeight = lastY ? blockSize.BaseHeight : blockSize.LastHeight;

				var startX = i * blockWidth;
				var startY = j * blockHeight;
				var endX   = startX + blockWidth;
				var endY   = startY + blockHeight;

				working.Add(new KeyValuePair<Coordinate, Color>(new Coordinate(i, j),
				                                                processor.AverageColourInArea(
					                                                startX, startY, endX, endY)));
			}

			return working.ToArray();
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

				// ReSharper disable once PossibleLossOfFraction
				var scaledIntensity       = (int) Math.Round((double) (intensity / IntensityChars.Length));
				var offsetScaledIntensity = scaledIntensity - 1;
				var intensityChar         = IntensityChars[offsetScaledIntensity];

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

	internal class BlockSize
	{
		/// <summary>
		///     The height of each block
		/// </summary>
		public int BaseHeight;

		/// <summary>
		///     The width of each block
		/// </summary>
		public int BaseWidth;

		/// <summary>
		///     The height of the last block to account for unclean dividing
		/// </summary>
		public int LastHeight;

		/// <summary>
		///     The width of the last block to account for unclean dividing
		/// </summary>
		public int LastWidth;
	}
}