using System.Runtime.CompilerServices;
using System.Text;
using SkiaSharp;

namespace ConsoleVideoPlayer.MediaProcessor;

public static class Converter
{
	public static char Create8CellBraille(bool[] dots)
	{
		if (dots.Length != 8) throw new ArgumentException("must be 8 dots", nameof(dots));

		uint final = 0x2800;

		for (var i = 0; i < 8; i++)
			if (dots[i])
			{
				final += 1u << i switch
				{
					// 0 3
					// 1 4
					// 2 5
					// 6 7
					0 => 0,
					1 => 3,
					2 => 1,
					3 => 4,
					4 => 2,
					5 => 5,
					6 => 6,
					7 => 7,
					// shush compiler
					_ => 0
				};
			}

		return (char) final;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}