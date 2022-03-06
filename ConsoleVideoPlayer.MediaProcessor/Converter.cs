using System.Diagnostics;
using System.Text;
using SkiaSharp;

namespace ConsoleVideoPlayer.MediaProcessor;

public static class Converter
{
	public const int ThreadCount = 8;

	private static void AnsiEscape(SKColor top, SKColor btm, SKColor prevTop, SKColor prevBtm, StringBuilder target)
	{
		var tChanged = top != prevTop;
		var bChanged = btm != prevBtm;

		if (tChanged || bChanged)
		{
			target.Append("\u001b[");

			if (tChanged) target.Append($"38;2;{top.Red};{top.Green};{top.Blue}");

			if (tChanged && bChanged) target.Append(';');
			
			if (bChanged) target.Append($"48;2;{btm.Red};{btm.Green};{btm.Blue}");

			target.Append('m');
		}

		target.Append('▀');
	}

	public static string ProcessImage(string path)
	{
		var lookup  = new PixelLookup(path);
		var prevTop = SKColor.Empty;
		var prevBtm = SKColor.Empty;

		var working = new StringBuilder();

		for (var y = 0; y < lookup.Height; y += 2)
		{
			for (var x = 0; x < lookup.Width; x++)
			{
				var top = lookup.AtCoord(x, y);
				var btm = lookup.AtCoord(x, y + 1);
				
				AnsiEscape(top, btm, prevTop, prevBtm, working);

				prevTop = top;
				prevBtm = btm;
			}

			working.AppendLine();
		}

		return working.ToString();
	}

	public static async Task<LinkedList<string>> ConvertAllImages(string imageDir)
	{
		Console.Write("Creating ASCII art           ");
		var fileLists = new DirectoryInfo(imageDir).EnumerateFiles()
												   .OrderBy(f => int.Parse(f.Name[6..^4]))
												   .Select((f, i) => (i, f.FullName))
												   .ToArray()
												   .Split(ThreadCount);

		var sw = Stopwatch.StartNew();

		var tasks = fileLists.Select(l => Task.Run(() => l.Select(p => (p.i, ProcessImage(p.FullName))).ToArray()))
							 .ToArray();

		var results = await Task.WhenAll(tasks);
		sw.Stop();

		Console.WriteLine($"Done in {sw.Elapsed.Minutes}m {sw.Elapsed.Seconds + sw.Elapsed.Milliseconds / 1000.0:F2}s");

		return new LinkedList<string>(results.SelectMany(m => m).OrderBy(p => p.i).Select(p => p.Item2));
	}
}