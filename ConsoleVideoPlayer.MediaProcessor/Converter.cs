using System.Runtime.CompilerServices;
using System.Text;

namespace ConsoleVideoPlayer.MediaProcessor;

public static class Converter
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AnsiEscape(Color top, Color btm, Color prevTop, Color prevBtm, StringBuilder target)
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
		var prevTop = Color.Empty;
		var prevBtm = Color.Empty;

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