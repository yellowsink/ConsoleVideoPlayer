using System;
using System.Drawing;

namespace ConsoleVideoPlayer.Img2Text
{
	public static class ConsoleColorParser
	{
		public static ConsoleColor ClosestConsoleColor(this Color colour)
			=> ClosestConsoleColor(colour.R, colour.G, colour.B);

		// Thanks StackOverflow user "Glenn Slayden"! Answer link: https://stackoverflow.com/a/12340136/8388655
		public static ConsoleColor ClosestConsoleColor(byte r, byte g, byte b)
		{
			ConsoleColor ret = 0;
			double       rr  = r, gg = g, bb = b, delta = double.MaxValue;

			foreach (ConsoleColor cc in Enum.GetValues(typeof(ConsoleColor)))
			{
				var n = Enum.GetName(typeof(ConsoleColor), cc);
				var c = Color.FromName(n == "DarkYellow" ? "Orange" : n); // bug fix
				var t = Math.Pow(c.R - rr, 2.0) + Math.Pow(c.G - gg, 2.0) + Math.Pow(c.B - bb, 2.0);
				if (t == 0.0)
					return cc;
				if (t < delta)
				{
					delta = t;
					ret   = cc;
				}
			}

			return ret;
		}
	}
}