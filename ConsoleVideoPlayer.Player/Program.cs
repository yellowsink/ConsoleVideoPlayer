using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using ConsoleVideoPlayer.Img2Text;
using ConsoleVideoPlayer.VideoProcessor;
using Xabe.FFmpeg;

namespace ConsoleVideoPlayer.Player
{
	internal class Program
	{
		private static string TempDir;

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

			TempDir = processedArgs.TempFolderPath ??
			          Path.Combine(
			                       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			                       @"Temp\Cain Atkinson\ConsoleVideoPlayer");

			var metadata         = await PreProcess(processedArgs.VideoPath);
			var asciiArt         = ConvertAllImagesToAscii(Path.Combine(TempDir, "Split Images"));
			var firstVideoStream = metadata.VideoStreams.First();
			PlayAllFrames(asciiArt, firstVideoStream.Framerate);
		}

		private static void Help()
		{
			Console.WriteLine(@"ConsoleVideoPlayer Help
-v, --video:                 Specify the location of the video to play
-t, --tempfolder (optional): Specify a temporary folder to use
-h, --help:                  Show this page");
			Console.WriteLine(
			                  "The width and height set are in 16:9. Videos at other ratios will be STRETCHED NOT LETTERBOXED");
		}

		private static Args ProcessArgs(IEnumerable<string> rawArgs)
		{
			var processedArgs = new Args();
			Parser.Default.ParseArguments<Args>(rawArgs).WithParsed(o => { processedArgs = o; });
			return processedArgs;
		}

		private static async Task<IMediaInfo> PreProcess(string path)
		{
			var processor = new PreProcessor {VideoPath = path, TempFolder = TempDir};
			Console.WriteLine("Reading metadata");
			await processor.PopulateMetadata();
			Console.WriteLine("Preparing to pre-process");
			processor.CleanupTempDir(TempDir);
			Directory.CreateDirectory(TempDir);
			Console.Write("Extracting Audio... ");
			await processor.ExtractAudio();
			Console.WriteLine("Done");
			Console.Write("Splitting into images, this may take a while... ");
			await processor.SplitVideoIntoImages();
			Console.WriteLine("Done");
			Console.WriteLine("pre-processing complete");

			return processor.Metadata;
		}

		private static KeyValuePair<Coordinate, ColouredCharacter>[][] ConvertAllImagesToAscii(
			string imageDirectory, int targetWidth = 160, int targetHeight = 90)
		{
			Console.Write("Converting all images to ASCII art, this may take a while... ");

			var working = new List<IEnumerable<KeyValuePair<Coordinate, ColouredCharacter>>>();
			foreach (var file in new DirectoryInfo(imageDirectory).EnumerateFiles())
			{
				var converter = new Converter {ImagePath = file.FullName};
				var ascii     = converter.FullProcessImage(targetWidth, targetHeight);
				working.Add(ascii);
			}

			Console.WriteLine("Done");

			return working.Select(a => a.ToArray()).ToArray();
		}

		private static void WriteColouredChar(ColouredCharacter colouredChar)
		{
			Console.ForegroundColor = colouredChar.Colour;
			Console.Write(colouredChar.Character);
			Console.ResetColor();
		}

		private static void WriteAsciiFrame(IEnumerable<KeyValuePair<Coordinate, ColouredCharacter>> frame)
		{
			var currentRow = 0;

			foreach (var (coordinate, character) in frame)
			{
				var lastChar             = coordinate.X == 0;
				if (lastChar) currentRow = coordinate.Y;

				WriteColouredChar(character);

				if (lastChar) Console.WriteLine();
			}
		}

		private static void PlayAllFrames(
			IEnumerable<IEnumerable<KeyValuePair<Coordinate, ColouredCharacter>>> frames, double frameRate)
		{
			var frameTimeRawSeconds   = 1 / frameRate;
			var frameTimeSeconds      = (int) Math.Floor(frameTimeRawSeconds);
			var frameTimeMilliseconds = (int) ((frameTimeRawSeconds - frameTimeSeconds) * 1000);

			var frameTime = new TimeSpan(0, 0, 0, frameTimeSeconds, frameTimeMilliseconds);

			foreach (var frame in frames)
			{
				Console.Clear();
				WriteAsciiFrame(frame);
				Thread.Sleep(frameTime);
			}
		}
	}
}