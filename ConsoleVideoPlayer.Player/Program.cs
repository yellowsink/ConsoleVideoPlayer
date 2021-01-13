using System;
using System.Threading.Tasks;
using ConsoleVideoPlayer.VideoProcessor;
using Xabe.FFmpeg;

namespace ConsoleVideoPlayer.Player
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			MainAsync(args).GetAwaiter()
			               .GetResult(); // Do it like this instead of .Wait() to stop exceptions from being wrapped into an AggregateException
		}

		private static async Task MainAsync(string[] args) => Console.WriteLine("Hello World!");

		private static async Task<IMediaInfo> PreProcess(string path)
		{
			var processor = new PreProcessor {VideoPath = path};
			await processor.PopulateMetadata();
			processor.CleanupTempDir();
			await processor.ExtractAudio();
			await processor.SplitVideoIntoImages();

			return processor.Metadata;
		}
	}
}