using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace ConsoleVideoPlayer.Img2Text
{
	public class Converter
	{
		private const int ThreadCount = 8;

		public static string ProcessImage(string imagePath, int? targetWidth = null, int? targetHeight = null)
		{
			var lookup = new PixelLookup(imagePath);

			var working  = new StringBuilder();
			var previous = (top: SKColor.Empty, btm: SKColor.Empty);

			for (var y = 0; y < targetHeight; y += 2)
			{
				for (var x = 0; x < targetWidth; x++)
				{
					var topCol = lookup.ColourAtCoord(x, y);
					var btmCol = lookup.ColourAtCoord(x, y + 1);

					var tChanged = topCol.Red != previous.top.Red || topCol.Green != previous.top.Green
																  || topCol.Blue  != previous.top.Blue;
					var bChanged = btmCol.Red != previous.btm.Red || btmCol.Green != previous.btm.Green
																  || btmCol.Blue  != previous.btm.Blue;

					if (tChanged || bChanged)
					{
						// start an ansi escape sequence
						working.Append("\u001b[");
						// if necessary set the FG colour
						if (tChanged) working.Append($"38;2;{topCol.Red};{topCol.Green};{topCol.Blue}");
						// if both need setting separate with a semicolon
						if (tChanged && bChanged) working.Append(';');
						// if necessary set the BG colour
						if (bChanged) working.Append($"48;2;{btmCol.Red};{btmCol.Green};{btmCol.Blue}");
						// end the ansi escape sequence
						working.Append('m');
					}

					working.Append('▀');

					previous = (topCol, btmCol);
				}

				working.Append('\n');
			}

			return working.ToString();
		}

		public static Queue<string> ConvertAllImagesToAscii(string imageDirectory, int targetWidth, int targetHeight)
		{
			// prepare files for processing
			Console.Write("Creating ASCII art           ");
			var files = new DirectoryInfo(imageDirectory) // the directory
					   .EnumerateFiles()                  // get all files
					   .OrderBy(f => Convert.ToInt32(f.Name[new Range(6, f.Name.Length - 4)]))
					   .ToArray(); // put them in order!!!

			// prepare what work is to be done by what thread
			var threadFileLists = new List<(int, FileSystemInfo)>[ThreadCount];
			for (var i = 0; i < files.Length; i++)
			{
				// initialise lists the first time around
				// ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
				// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
				threadFileLists[i % ThreadCount] ??= new();

				threadFileLists[i % ThreadCount].Add((i, files[i]));
			}

			// fire off the threads to begin work
			var sw = Stopwatch.StartNew();
			var tasks = threadFileLists
					   .Select(threadList
								   => Task.Run(() => FrameConverterThread(targetWidth, targetHeight, threadList)))
					   .ToArray();


			// wait for the tasks to finish
			Task.WaitAll(tasks);
			sw.Stop();

			// join all lists together
			var taskResults = tasks.Select(t => t.GetAwaiter().GetResult()).ToArray();
			var allFrames   = new string[taskResults.Aggregate(0, (curr, _) => curr + 1)];
			for (var i = 0; i < allFrames.Length; i++)
				allFrames[i] = taskResults[i % ThreadCount][i / ThreadCount].Item2;

			var time = sw.Elapsed;
			Console.WriteLine($"Done in {time.Minutes}m {time.Seconds}s");

			return new Queue<string>(allFrames);
		}

		private static (int, string)[] FrameConverterThread(int targetWidth, int targetHeight,
															IEnumerable<(int, FileSystemInfo)> frames)
		{
			var working = new List<(int, string)>();

			foreach (var (num, file) in frames)
				working.Add((num, ProcessImage(file.FullName, targetWidth, targetHeight)));

			return working.ToArray();
		}
	}
}