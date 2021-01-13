using System.Collections.Generic;
using System.Drawing;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class Converter
	{
		public string ImagePath { get; init; }

		public BlockSize CalculateBlockSize(int targetWidth, int targetHeight)
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

		public Color[] GetColoursOfFrame(int? targetWidth = null, int? targetHeight = null)
		{
			var img = new MagickImage(ImagePath);
			targetWidth  ??= img.BaseWidth;
			targetHeight ??= img.BaseWidth;
			var blockSize = CalculateBlockSize(targetWidth.Value, targetHeight.Value);
			var processor = new ImageProcessor {ImagePath = ImagePath};

			var working = new List<Color>();
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

				working.Add(processor.AverageColourInArea(startX, startY, endX, endY));
			}

			return working.ToArray();
		}
	}

	public class BlockSize
	{
		public int BaseHeight;
		public int BaseWidth;
		public int LastHeight;
		public int LastWidth;
	}
}