using System.Runtime.CompilerServices;
using System.Text;
using SkiaSharp;

namespace ConsoleVideoPlayer.MediaProcessor;

public static class Converter
{
	// follows ITU-R rec 709 luma
	// here is a table of coefficents
	// Y601 = 0.2990R + 0.5870G + 0.1140B (classic)
	// Y709 = 0.2126R + 0.7152G + 0.0722B (HDTV, this impl)
	// Y240 = 0.2120R + 0.7010G + 0.0870B (1035i HDTV)
	// Y145 = Y240
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte Rec709Luma(byte r, byte g, byte b) => (byte) (0.2126 * r + 0.7152 * g + 0.0722 * b);

	//private static readonly double LumaScale = 1.0 / Rec709Luma(255, 255, 255);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	public static byte FindAvgLuma(SKColor[] frame/*, float bias*/)
	{
		//TODO: how would one implement the bias?
		
		uint lumaSum = 0;
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var l in frame)
			lumaSum += Rec709Luma(l.Red, l.Blue, l.Green);

		return (byte) (lumaSum / frame.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AnsiEscape((byte r, byte g, byte b) fg,     (byte r, byte g, byte b) bg,
								   (byte r, byte g, byte b) prevFg, (byte r, byte g, byte b) prevBg,
								   StringBuilder            target,  char                     c)
	{
		var tChanged = fg != prevFg;
		var bChanged = bg != prevBg;

		if (tChanged || bChanged)
		{
			target.Append("\u001b[");

			if (tChanged) target.Append($"38;2;{fg.r};{fg.g};{fg.b}");

			if (tChanged && bChanged) target.Append(';');

			if (bChanged) target.Append($"48;2;{bg.r};{bg.g};{bg.b}");

			target.Append('m');
		}

		target.Append(c);
	}

	public static string ProcessImage(string path)
	{
		var lookup  = new PixelLookup(path);
		var prevFg = ((byte) 0, (byte) 0, (byte) 0);
		var prevBg = ((byte) 0, (byte) 0, (byte) 0);

		//var averageLuma = FindAvgLuma(lookup.Pixels/*, 0.66*/);
		
		var working = new StringBuilder();

		for (var y = 0; y < lookup.Height; y += 4)
		{
			for (var x = 0; x < lookup.Width; x += 2)
			{
				var pixels = new[]
				{
					// the useless `+ 0`s are to make jetbrains formatter be less idiotic by being uniform
					lookup.AtCoord(x + 0, y + 0),
					lookup.AtCoord(x + 1, y + 0),
					lookup.AtCoord(x + 0, y + 1),
					lookup.AtCoord(x + 1, y + 1),
					lookup.AtCoord(x + 0, y + 2),
					lookup.AtCoord(x + 1, y + 2),
					lookup.AtCoord(x + 0, y + 3),
					lookup.AtCoord(x + 1, y + 3),
				};

				var averageLuma = FindAvgLuma(pixels);

				var aboveLen  = 0;
				var aboveAvgR = 0u;
				var aboveAvgG = 0u;
				var aboveAvgB = 0u;
				var belowLen  = 0;
				var belowAvgR = 0u;
				var belowAvgG = 0u;
				var belowAvgB = 0u;

				var cells = new bool[8];

				for (var i = 0; i < pixels.Length; i++)
				{
					var r     = pixels[i].Red;
					var g     = pixels[i].Green;
					var b     = pixels[i].Blue;
					if (Rec709Luma(r, g, b) >= averageLuma)
					{
						aboveLen++;
						aboveAvgR += r;
						aboveAvgG += g;
						aboveAvgB += b;
					}
					else
					{
						cells[i] = true;
						belowLen++;
						belowAvgR += r;
						belowAvgG += g;
						belowAvgB += b;
					}
				}

				var bChar = Create8CellBraille(cells);

				var fg = belowLen == 0
							 ? ((byte) 0, (byte) 0, (byte) 0)
							 : ((byte) (belowAvgR / belowLen), (byte) (belowAvgG / belowLen),
								(byte) (belowAvgB / belowLen));

				var bg = aboveLen == 0
							 ? fg
							 : ((byte) (aboveAvgR / aboveLen), (byte) (aboveAvgG / aboveLen),
								(byte) (aboveAvgB / aboveLen));

				if (belowLen == 0)
					fg = bg;
				
				AnsiEscape(fg, bg, prevFg, prevBg, working, bChar);

				prevFg = fg;
				prevBg = bg;
			}

			working.AppendLine();
		}

		return working.ToString();
	}
}