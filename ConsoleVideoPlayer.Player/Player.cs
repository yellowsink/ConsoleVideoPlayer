using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ConsoleVideoPlayer.Player
{
	public static class Player
	{
		/// <summary>
		///     Renders all frames
		/// </summary>
		public static void PlayAsciiFrames(IEnumerable<string> frames, double frameRate)
		{
			Console.CursorVisible = false;

			GenericPlay(frames, Console.Write, frameRate);

			Console.CursorVisible = true;
		}

		/// <summary>
		///     Renders all file paths as images using viu - very slow
		/// </summary>
		public static void PlayViuFrames(IEnumerable<string> filePaths, double frameRate, int targetHeight)
		{
			// scale values to represent viu better
			targetHeight /= 2;

			void RenderFunc(string path)
				=> Process.Start("viu", $"{path} -h {targetHeight}")?.WaitForExit();

			GenericPlay(filePaths, RenderFunc, frameRate);
		}

		/// <summary>
		///     Executes an arbitrary function for all items in the enumerable, and keeps in time with the framerate
		/// </summary>
		private static void GenericPlay<T>(IEnumerable<T> iterator, Action<T> renderFunc, double frameRate)
		{
			var frameTime = (long) (10_000_000 / frameRate);

			long timeDebt = 0;

			Console.CursorVisible = false;

			foreach (var iterable in iterator)
			{
				var now = DateTime.UtcNow.Ticks;

				Console.CursorLeft = 0;
				Console.CursorTop  = 0;
				renderFunc(iterable);

				// measure the time rendering took
				var renderTime = DateTime.UtcNow.Ticks - now;
				// the amount of time we need to compensate for
				var makeupTarget = renderTime + timeDebt;
				// timeDebt has been accounted for, reset it!
				timeDebt = 0;
				// the maximum possible correction to apply this frame
				var correction = Math.Min(makeupTarget, frameTime);
				// if we can't fully make up time try to do it later
				if (makeupTarget > frameTime)
					timeDebt += makeupTarget - frameTime;

				var toWait = frameTime - correction;

				// wait for it!
				Thread.Sleep(new TimeSpan(toWait));
			}
		}
	}
}