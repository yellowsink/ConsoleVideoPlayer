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
		var frameTime   = (long) (10_000_000 / frameRate);
		var startTime   = DateTime.UtcNow.Ticks;
		var timeDebt    = 0L;
		var skipCounter = 0;

		Console.CursorVisible = false;

		for (var frameCount = 1; cstream.Count > 0; frameCount++)
		{
			// keep conversion stream in check
			if (cstream.ReadyCount < FrameProcessThres)
				cstream.SafelyProcessMore(FrameBatchSize);
			
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

			Console.Write("\u001b[H");
			renderFunc(await cstream.GetAsync());
			debugFunc?.Invoke(timeDebt);

			// measure current time
			var current  = DateTime.UtcNow.Ticks - startTime;
			
			// the full amount of time we are behind by
			var amountBehind = current - (frameCount * frameTime) + timeDebt;
			
			// timeDebt has been accounted for, reset it!
			timeDebt = 0;
			
			// ideally how long we need to wait for (how far *ahead* we are if you will)
			var waitTime = frameTime - amountBehind;

			if (waitTime > 0)
				Thread.Sleep(new TimeSpan(waitTime));
			else
				// if we can't fully make up time try to do it later
				timeDebt += -waitTime;
		}
		
		Console.CursorVisible = true;
	}
}