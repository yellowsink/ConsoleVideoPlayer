using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine.Options;
using ConsoleVideoPlayer.VideoProcessor;
using Xabe.FFmpeg;

namespace ConsoleVideoPlayer.Player
{
	internal class Program
	{
		private static OptionSet _set;

		private static void Main(string[] args)
		{
			MainAsync(args).GetAwaiter()
			               .GetResult(); // Do it like this instead of .Wait() to stop exceptions from being wrapped into an AggregateException
		}

		private static async Task MainAsync(IEnumerable<string> args)
		{
			var processedArgs = ProcessArgs(args);
			if (processedArgs.Help || string.IsNullOrWhiteSpace(processedArgs.VideoPath))
			{
				Help();
				return;
			}

			await PreProcess(processedArgs.VideoPath);
		}

		private static void Help()
		{
			var stringWriter = new StringWriter();
			_set.WriteOptionDescriptions(stringWriter);
			Console.WriteLine(stringWriter.ToString());
		}

		private static Args ProcessArgs(IEnumerable<string> rawArgs)
		{
			var processedArgs = new Args();
			_set = new OptionSet
			{
				{
					"h|help", "show this message and exit",
					v => processedArgs.Help = v != null
				},
				{
					"v|video", "specify where the video to play is",
					v => processedArgs.VideoPath = v
				},
				{
					"t|temp", "specify where to save temporary files",
					v => processedArgs.TempFolderPath = v
				}
			};
			try
			{
				_set.Parse(rawArgs);
			}
			catch (OptionException)
			{
				processedArgs.Help = true;
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