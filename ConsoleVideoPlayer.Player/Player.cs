using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ConsoleVideoPlayer.Img2Text;

namespace ConsoleVideoPlayer.Player
{
	public static class Player
	{
		/// <summary>
		///     Renders all frames
		/// </summary>
		public static async Task PlayAsciiFrames(IStringStream frames, double frameRate)
		{
			Console.CursorVisible = false;

			await GenericPlay(frames, Console.Write, frameRate);

			Console.CursorVisible = true;
		}

		/// <summary>
		///     Renders all file paths as images using viu - very slow
		/// </summary>
		public static Task PlayViuFrames(IStringStream filePaths, double frameRate, int targetHeight)
		{
			// scale values to represent viu better
			targetHeight /= 2;

			void RenderFunc(string path)
				=> Process.Start("viu", $"{path} -h {targetHeight}")?.WaitForExit();

			return GenericPlay(filePaths, RenderFunc, frameRate);
		}

		/// <summary>
		///     Executes an arbitrary function for all items in the queue, and keeps in time with the framerate
		/// </summary>
		private static async Task GenericPlay(IStringStream queue, Action<string> renderFunc, double frameRate)
		{
			var frameTime = (long) (10_000_000 / frameRate);

			long timeDebt = 0;

			Console.CursorVisible = false;

			while (!queue.Empty)
			{
				var now   = DateTime.UtcNow.Ticks;
				
				var value = await queue.Read();

				Console.CursorLeft = 0;
				Console.CursorTop  = 0;
				renderFunc(value);

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