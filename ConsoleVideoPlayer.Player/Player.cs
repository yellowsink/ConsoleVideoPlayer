using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleVideoPlayer.MediaProcessor;

namespace ConsoleVideoPlayer.Player;

public static class Player
{
	// amount of frames to process in each batch
	public const int FrameBatchSize = 50;

	// how many (at most!) frames must be remaining before the next batch is queued for processing
	public const int FrameProcessThres = 2;

	public static Task PlayAsciiFrames(IFrameStream cstream, double frameRate, bool debug, int frameSkip)
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

		return GenericPlay(cstream, Console.Write, frameRate, frameSkip, debug ? DebugFunc : null);
	}

	public static Task PlayKittenFrames(IFrameStream filePaths, double frameRate, int frameSkip)
		=> GenericPlay(filePaths,
					   path =>
					   {
						   var b64Path = Convert.ToBase64String(Encoding.Default.GetBytes(path));

						   // a=T = magical param that makes it work (?)
						   // f=100 = png
						   // t=f = read from file
						   Console.Write($"\u001b_Gq=1,a=T,f=100,t=f;{b64Path}\u001b\\");
					   },
					   frameRate,
					   frameSkip);

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private static Task GenericPlay(IFrameStream   cstream, Action<string> renderFunc, double frameRate, int frameSkip,
									Action<long?>? debugFunc = null)
		=> TimeDebt.TimeDebt.TrackTimeAsync(frameRate,
											async timeDebt =>
											{
												Console.Write("\u001b[H");
												renderFunc(await cstream.GetAsync());
												debugFunc?.Invoke(timeDebt);
											},
											_ => cstream.Count > 0,
											(Func<Task>?) (frameSkip == 0
															   ? null
															   : async () =>
															   {
																   await cstream.GetAsync();
																   debugFunc?.Invoke(null);
															   }),
											() =>
											{
												if (cstream.ReadyCount < FrameProcessThres)
													cstream.SafelyProcessMore(FrameBatchSize);
												return Task.CompletedTask;
											});
}