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
	public static class Converter
	{
		private const int ThreadCount = 8;

		private static readonly Stopwatch Stopwatch = new();

		private static void BuildAnsiEscape(SKColor       top, SKColor btm, (SKColor top, SKColor btm) prev,
											StringBuilder target)
		{
			var topChanged = top != prev.btm;
			var btmChanged = btm != prev.btm;

			if (topChanged || btmChanged)
			{
				// start an ansi escape sequence
				target.Append("\u001b[");
				// if necessary set the FG colour
				if (topChanged) target.Append($"38;2;{top.Red};{top.Green};{top.Blue}");
				// if both need setting separate with a semicolon
				if (topChanged && btmChanged) target.Append(';');
				// if necessary set the BG colour
				if (btmChanged) target.Append($"48;2;{btm.Red};{btm.Green};{btm.Blue}");
				// end the ansi escape sequence
				target.Append('m');
			}

			target.Append('▀');
		}

		public static string ProcessImage(string imagePath)
		{
			var lookup = new PixelLookup(imagePath);
			var (imgWidth, imgHeight) = lookup.Dimensions;

			var working  = new StringBuilder();
			var previous = (top: SKColor.Empty, btm: SKColor.Empty);

			for (var y = 0; y < imgHeight; y += 2)
			{
				for (var x = 0; x < imgWidth; x++)
				{
					var top = lookup.ColourAtCoord(x, y);
					var btm = lookup.ColourAtCoord(x, y + 1);

					BuildAnsiEscape(top, btm, previous, working);

					previous = (top, btm);
				}

				working.Append('\n');
			}

			return working.ToString();
		}

		public static async Task<Queue<string>> ConvertAllImagesToAscii(string imageDirectory)
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
			Stopwatch.Start();
			var tasks = threadFileLists
					   .Select(threadList => Task.Run(() => threadList
														   .Select(f => (f.Item1, ProcessImage(f.Item2.FullName)))
														   .ToArray()))
					   .ToArray();

			// wait for the tasks to finish
			await Task.WhenAll(tasks);
			Stopwatch.Stop();

			// join all lists together
			var allFrames = tasks.SelectMany(t => t.GetAwaiter().GetResult()) // get result of all tasks
								 .OrderBy(p => p.Item1) // sort them by the frame number
								 .Select(p => p.Item2) // just get the frames
								 .ToArray(); // finally, compute the results of this query to an array

			var time = Stopwatch.Elapsed;
			Console.WriteLine($"Done in {time.Minutes}m {time.Seconds}s");

			return new Queue<string>(allFrames);
		}
	}
}