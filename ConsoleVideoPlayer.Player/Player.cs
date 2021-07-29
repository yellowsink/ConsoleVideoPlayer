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
		public static void PlayAsciiFrames(Queue<string> frames, double frameRate)
		{
			Console.CursorVisible = false;

			GenericPlay(frames, Console.Write, frameRate);

			Console.CursorVisible = true;
		}

		/// <summary>
		///     Renders all file paths as images using viu - very slow
		/// </summary>
		public static void PlayViuFrames(Queue<string> filePaths, double frameRate, int targetHeight)
		{
			// scale values to represent viu better
			targetHeight /= 2;

			void RenderFunc(string path)
				=> Process.Start("viu", $"{path} -h {targetHeight}")?.WaitForExit();

			GenericPlay(filePaths, RenderFunc, frameRate);
		}

		/// <summary>
		///     Executes an arbitrary function for all items in the queue, and keeps in time with the framerate
		/// </summary>
		private static void GenericPlay<T>(Queue<T> queue, Action<T> renderFunc, double frameRate)
		{
			var frameTime = (long) (10_000_000 / frameRate);

			long timeDebt = 0;

			Console.CursorVisible = false;

			// this takes longer with more items, so we scale it here
			var freeInterval = queue.Count * 0.2;
			
			while (queue.Count > 0)
			{
				var value = queue.Dequeue();

				var now = DateTime.UtcNow.Ticks;

				Console.CursorLeft = 0;
				Console.CursorTop  = 0;
				renderFunc(value);

				// every 20 % of the queue size items, try to free up ram
				//if (queue.Count % freeInterval == 0)
					queue.TrimExcess();

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