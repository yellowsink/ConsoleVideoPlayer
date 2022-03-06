using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ConsoleVideoPlayer.Player;

public static class Player
{
	private static readonly int PadLength = long.MaxValue.ToString().Length;

	private const int DebugDebtQueueLength = 15;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string FormatLong(long val) => val.ToString().PadLeft(PadLength, '0');
	
	public static void PlayAsciiFrames(LinkedList<string> frames, double frameRate, bool debug)
	{
		Console.CursorVisible = false;

		var debugDebtQueue = new long[DebugDebtQueueLength];
		var debugDebtMax   = 0L;
		void DebugFunc(long timeDebt)
		{
			for (var i = 0; i < debugDebtQueue.Length - 1; i++) debugDebtQueue[i + 1] = debugDebtQueue[i];
			debugDebtQueue[0] = timeDebt;

			var sum = 0L;
			
			// ReSharper disable once ForCanBeConvertedToForeach
			// ReSharper disable once LoopCanBeConvertedToQuery
			for (var i = 0; i < debugDebtQueue.Length; i++) sum += debugDebtQueue[i];

			Console.Write("\u001b[32;40mTIME DEBT | curr: " + FormatLong(timeDebt) + " | last " + DebugDebtQueueLength
						+ " mean: "                         + FormatLong(sum / DebugDebtQueueLength) + " | max: "
						+ FormatLong(debugDebtMax = Math.Max(debugDebtMax, timeDebt)));
		}

		GenericPlay(frames, Console.Write, frameRate, debug ? DebugFunc : null);

		Console.CursorVisible = true;
	}

	public static void PlayViuFrames(LinkedList<string> filePaths, double frameRate, int targetHeight)
	{
		// scale values to represent viu better
		targetHeight /= 2;

		void RenderFunc(string path)
			=> Process.Start("viu", $"{path} -h {targetHeight}")?.WaitForExit();

		GenericPlay(filePaths, RenderFunc, frameRate);
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static void GenericPlay<T>(LinkedList<T> list, Action<T> renderFunc, double frameRate, Action<long>? debugFunc = null)
	{
		var frameTime = (long) (10_000_000 / frameRate);

		long timeDebt = 0;

		Console.CursorVisible = false;

		while (list.First != null)
		{
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