using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class Converter
	{
		private static readonly Stopwatch Stopwatch = new();
		
		public string ImagePath { get; init; }

		public string ProcessImage(int? targetWidth = null, int? targetHeight = null)
		{
			var img = new MagickImage(ImagePath);
			targetWidth  ??= img.BaseWidth;
			targetHeight ??= img.BaseHeight;
			var processor    = new ImageProcessor { Image = new MagickImage(ImagePath) };
			var resizedImage = ResizeImage(targetWidth.Value, targetHeight.Value, processor);

			var working = new StringBuilder();
			for (var y = 0; y < targetHeight; y += 2)
			{
				for (var x = 0; x < targetWidth; x++)
				{
					var topCol = resizedImage.ColourFromPixelCoordinate(x, y);
					var btmCol = resizedImage.ColourFromPixelCoordinate(x, y + 1);

					var topR = topCol.R.ToString();
					var topG = topCol.G.ToString();
					var topB = topCol.B.ToString();
					var btmR = btmCol.R.ToString();
					var btmG = btmCol.G.ToString();
					var btmB = btmCol.B.ToString();

					working.Append($"\u001b[38;2;{topR};{topG};{topB};48;2;{btmR};{btmG};{btmB}m"); // Add ANSI escape sequence for colour :)
					working.Append('▀');
				}

				working.Append('\n');
			}

			return working.ToString();
		}

		public static string[] ConvertAllImagesToAscii(string imageDirectory, int targetWidth, int targetHeight)
		{
			Console.Write("Creating ASCII art           ");

			var working = new List<string>();
			var files = new DirectoryInfo(imageDirectory) // the directory
					   .EnumerateFiles()                  // get all files
					   .OrderBy(f => Convert.ToInt32(f.Name[new Range(6, f.Name.Length - 4)]))
					   .ToArray(); // put them in order!!!

			var padAmount = files.Length.ToString().Length;

			Stopwatch.Start();
			for (var i = 0; i < files.Length; i++)
			{
				var converter = new Converter { ImagePath = files[i].FullName };
				var ascii     = converter.ProcessImage(targetWidth, targetHeight);
				working.Add(ascii);

				// every 10th
				if (i % 10 != 0) continue;
				Console.Write($"{i.ToString().PadLeft(padAmount, '0')} / " + $"{files.Length} "
																		   + $"[{(100 * i / files.Length).ToString().PadLeft(3, '0')}%]");
				Console.CursorLeft -= 10 + padAmount * 2;
			}
			Stopwatch.Stop();

			for (var i = 0; i < 10   + padAmount * 2; i++) Console.Write(' ');
			Console.CursorLeft -= 10 + padAmount * 2;

			var time = Stopwatch.Elapsed;
			Console.WriteLine($"Done in {time.Minutes}m {time.Seconds}s");

			return working.ToArray();
		}

		private static ImageProcessor ResizeImage(int targetWidth, int targetHeight, ImageProcessor processor)
			=> new() { Image = processor.ResizeImage(targetWidth, targetHeight) };
	}
}