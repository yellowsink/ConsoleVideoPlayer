using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleVideoPlayer.MediaProcessor;

namespace ConsoleVideoPlayer.Player;

public static class Player
{	
	public static async Task PlayAsciiFrames(ConversionStream cstream, double frameRate, bool debug, int frameSkip)
	{
		var stats = new RunningStats();
		void DebugFunc(long? timeDebt)
		{
			if (timeDebt == null)
			{
				stats.AddDropped();
				return;
			}

			stats.Running = cstream.IsRunning;

			stats.Add(timeDebt.Value);
			Console.Write(stats.Render(timeDebt.Value));
		}

		
		Console.CursorVisible = false;

		await GenericPlay(cstream, Console.Write, frameRate, frameSkip, debug ? DebugFunc : null);

		Console.CursorVisible = true;
	}

	public static void PlayViuFrames(LinkedList<string> filePaths, double frameRate, int frameSkip)
	{
		// scale values to represent viu better

		void RenderFunc(string path)
			=> Process.Start("viu", $"{path}").WaitForExit();

	//	GenericPlay(filePaths, RenderFunc, frameRate, frameSkip);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static async Task GenericPlay(ConversionStream cstream, Action<string> renderFunc, double frameRate, int frameSkip,
									   Action<long?>? debugFunc = null)
	{
		var frameTime = (long) (10_000_000 / frameRate);

		long timeDebt = 0;

		var skipCounter = 0;

		Console.CursorVisible = false;

		while (cstream.Count > 0)
		{
			if (timeDebt > frameTime)
			{
				if (frameSkip == -1 || skipCounter < frameSkip)
				{
					skipCounter++;
					timeDebt -= frameTime;
					await cstream.GetAsync();
					debugFunc?.Invoke(null);
					continue;
				}

				//else
				skipCounter = 0;
			}

			var now = DateTime.UtcNow.Ticks;

			Console.Write("\u001b[H");
			renderFunc(await cstream.GetAsync());
			debugFunc?.Invoke(timeDebt);

			// collect gc every 240 frames = 8 seconds at 30fps
			// tested various other places to put GC.Collect() but in the hot path is sadly the only effective solution
			if (cstream.Count % 240 == 0) 
				GC.Collect();

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