using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ConsoleVideoPlayer.Player;

public static class Player
{	
	public static void PlayAsciiFrames(LinkedList<string> frames, double frameRate, bool debug, int frameSkip)
	{
		var stats = new RunningStats();
		void DebugFunc(long? timeDebt)
		{
			if (timeDebt == null)
			{
				stats.AddDropped();
				return;
			}

			stats.Add(timeDebt.Value);
			Console.Write(stats.Render(timeDebt.Value));
		}

		
		Console.CursorVisible = false;

		GenericPlay(frames, Console.Write, frameRate, frameSkip, debug ? DebugFunc : null);

		Console.CursorVisible = true;
	}

	public static void PlayViuFrames(LinkedList<string> filePaths, double frameRate, int targetHeight, int frameSkip)
	{
		// scale values to represent viu better
		targetHeight /= 2;

		void RenderFunc(string path)
			=> Process.Start("viu", $"{path} -h {targetHeight}")?.WaitForExit();

		GenericPlay(filePaths, RenderFunc, frameRate, frameSkip);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void GenericPlay<T>(LinkedList<T> list, Action<T> renderFunc, double frameRate, int frameSkip,
									   Action<long?>? debugFunc = null)
	{
		var frameTime = (long) (10_000_000 / frameRate);

		long timeDebt = 0;

		var skipCounter = 0;

		Console.CursorVisible = false;

		while (list.First != null)
		{
			if (timeDebt > frameTime)
			{
				if (frameSkip == -1 || skipCounter < frameSkip)
				{
					skipCounter++;
					timeDebt -= frameTime;
					list.RemoveFirst();
					debugFunc?.Invoke(null);
					continue;
				}

				skipCounter = 0;
			}

			var now = DateTime.UtcNow.Ticks;

			Console.Write("\u001b[H");
			renderFunc(list.First.Value);
			list.RemoveFirst();
			debugFunc?.Invoke(timeDebt);

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