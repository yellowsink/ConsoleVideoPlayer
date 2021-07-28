using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
	public class Converter
	{
		private const int ThreadCount = 8;

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
			// prepare files for processing
			Console.Write("Creating ASCII art           ");
			var files = new DirectoryInfo(imageDirectory) // the directory
					   .EnumerateFiles()                  // get all files
					   .OrderBy(f => Convert.ToInt32(f.Name[new Range(6, f.Name.Length - 4)]))
					   .ToArray(); // put them in order!!!

			var padAmount = files.Length.ToString().Length;


			// prepare what work is to be done by what thread
			var threadFileLists = new List<(int, FileSystemInfo)>[ThreadCount];
			for (var i = 0; i < files.Length; i++)
			{
				// initialise lists the first time around
				// ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
				threadFileLists[i % ThreadCount] ??= new();

				threadFileLists[i % ThreadCount].Add((i, files[i]));
			}

			// fire off the threads to begin work
			var tasks = new List<Task<(int, string)[]>>();
			Stopwatch.Start();
			foreach (var threadList in threadFileLists)
				tasks.Add(Task.Run(() => FrameConverterThread(targetWidth, targetHeight, threadList)));


			// wait for the tasks to finish
			Task.WaitAll(tasks.ToArray());
			Stopwatch.Stop();

			// join all lists together
			var allFrames = tasks.Select(t => t.GetAwaiter().GetResult()) // get result of all tasks
								 .SelectMany(a => a) // join all the arrays into one
								 .OrderBy(p => p.Item1) // sort them by the frame number
								 .Select(p => p.Item2) // just get the frames
								 .ToArray(); // finally, compute the results of this query to an array

			var time = Stopwatch.Elapsed;
			Console.WriteLine($"Done in {time.Minutes}m {time.Seconds}s");

			return allFrames;
		}

		private static (int, string)[] FrameConverterThread(int targetWidth, int targetHeight,
															IEnumerable<(int, FileSystemInfo)> frames)
		{
			var working = new List<(int, string)>();

			foreach (var (num, file) in frames)
				working.Add((num, new Converter { ImagePath = file.FullName }.ProcessImage(targetWidth, targetHeight)));

			return working.ToArray();
		}

		private static ImageProcessor ResizeImage(int targetWidth, int targetHeight, ImageProcessor processor)
			=> new() { Image = processor.ResizeImage(targetWidth, targetHeight) };
	}
}