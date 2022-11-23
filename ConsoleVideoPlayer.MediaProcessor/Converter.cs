using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleVideoPlayer.MediaProcessor;

public static class Converter
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AnsiEscape(Color top, Color btm, Color prevTop, Color prevBtm, StringBuilder target, bool first)
	{
		var tChanged = top != prevTop || first;
		var bChanged = btm != prevBtm || first;

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
		var prevTop = new Color(0, 0, 0);
		var prevBtm = new Color(0, 0, 0);
		var first   = true;

		var working = new StringBuilder();

		for (var y = 0; y < lookup.Height; y += 2)
		{
			for (var x = 0; x < lookup.Width; x++)
			{
				var top = lookup.AtCoord(x, y);
				var btm = lookup.AtCoord(x, y + 1);

				AnsiEscape(top, btm, prevTop, prevBtm, working, first);

				prevTop = top;
				prevBtm = btm;
				first   = false;
			}

			working.AppendLine();
		}

		return working.ToString();
	}
}