using System;
using System.Linq;
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

		private static Args ProcessArgs(string[] rawArgs)
		{
			// I hate dealing with arguments ugh
			var processedArgs = new Args();
			if (rawArgs.Contains("-h")
			 || rawArgs.Contains("--help"))
				processedArgs.Help = true; // display help
			else
				for (var i = 0; i < rawArgs.Length; i++)
				{
					var arg     = rawArgs[i];
					var nextArg = rawArgs[i + 1];

					switch (arg)
					{
						case "-v": // -v is used to specify the video path
							processedArgs.VideoPath = nextArg;
							i++;
							break;
						case "-t": // -t is used to specify the temp folder path
							processedArgs.TempFolderPath = nextArg;
							i++;
							break;
					}
				}

			return processedArgs;
		}

		/// <summary>
		///     Pre-processes the video: extracts audio, splits into images, gets metadata
		/// </summary>
		/// <param name="path">The path of the video to process</param>
		/// <returns>The video metadata</returns>
		private static async Task<IMediaInfo> PreProcess(string path)
		{
			Console.WriteLine("Reading metadata");
			var processor = new PreProcessor {VideoPath = path};
			await processor.PopulateMetadata();
			Console.WriteLine("Preparing to pre-process");
			processor.CleanupTempDir();
			Console.Write("Extracting Audio... ");
			await processor.ExtractAudio();
			Console.WriteLine("Done");
			Console.Write("Splitting into images... ");
			await processor.SplitVideoIntoImages();
			Console.WriteLine("Done");
			Console.WriteLine("pre-processing complete");

			return processor.Metadata;
		}
	}
}