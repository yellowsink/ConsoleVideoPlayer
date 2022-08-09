using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleVideoPlayer.MediaProcessor;

namespace ConsoleVideoPlayer.Player;

public static class Player
{
	// amount of frames to process in each batch
	public const int FrameBatchSize = 50;

	// how many (at most!) frames must be remaining before the next batch is queued for processing
	public const int FrameProcessThres = 2;

	public static async Task PlayAsciiFrames(IFrameStream cstream, double frameRate, bool debug, int frameSkip)
	{
		var stats = new RunningStats();

		void DebugFunc(long? timeDebt)
		{
			if (timeDebt == null)
			{
				stats.AddDropped();
				return;
			}

			stats.Running = cstream.Status;

			stats.Add(timeDebt.Value);
			Console.Write(stats.Render(timeDebt.Value));
		}

		await GenericPlay(cstream, Console.Write, frameRate, frameSkip, debug ? DebugFunc : null);
	}

	public static async Task PlayViuFrames(IFrameStream filePaths, double frameRate, int frameSkip)
	{
		void RenderFunc(string path)
			=> Process.Start("viu", $"{path}").WaitForExit();

		await GenericPlay(filePaths, RenderFunc, frameRate, frameSkip);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static async Task GenericPlay(IFrameStream cstream,   Action<string> renderFunc, double frameRate,
										  int          frameSkip, Action<long?>? debugFunc = null)
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

			// keep conversion stream in check
			if (cstream.ReadyCount < FrameProcessThres)
				cstream.SafelyProcessMore(FrameBatchSize);

			var now = DateTime.UtcNow.Ticks;

			Console.Write("\u001b[H");
			renderFunc(await cstream.GetAsync());
			debugFunc?.Invoke(timeDebt);

			// collect gc every 240 frames = 8 seconds at 30fps
			// tested various other places to put GC.Collect() but in the hot path is sadly the only effective solution
			/*if (cstream.Count % 240 == 0) 
				GC.Collect();*/ // handled in conv stream now

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
		
		Console.CursorVisible = true;
	}
}