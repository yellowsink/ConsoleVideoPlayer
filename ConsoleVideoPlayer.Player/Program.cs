using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

			var targetWidth  = 64;
			var targetHeight = 48;

			var preProcessResult = await PreProcess(processedArgs.VideoPath);
			// ResizeAllImages(Path.Combine(TempDir, "Split Images"), Path.Combine(TempDir, "Resized Images"), targetWidth, targetHeight, true);
			var asciiArt = ConvertAllImagesToAscii(Path.Combine(TempDir, "Split Images"), targetWidth, targetHeight);
			var monochromeFrames = FramesToMonochromeStrings(asciiArt, targetWidth, targetHeight);

			Console.Write("Ready to play video! Press enter to begin playback.");
			Console.ReadLine();
			Console.Clear();


#pragma warning disable 4014
			new NetCoreAudio.Player().Play(preProcessResult.AudioPath);
#pragma warning restore 4014

			var firstVideoStream = preProcessResult.Metadata.VideoStreams.First();
			PlayAllFramesMonochrome(monochromeFrames, firstVideoStream.Framerate);
			//PlayAllFrames(asciiArt, firstVideoStream.Framerate, targetWidth, targetHeight);
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

		private class PreProcessResult
		{
			public IMediaInfo Metadata  { get; set; }
			public string     AudioPath { get; set; }
		}

		private static async Task<PreProcessResult> PreProcess(string path)
		{
			var processor = new PreProcessor {VideoPath = path, TempFolder = TempDir};
			Console.WriteLine("Reading metadata");
			await processor.PopulateMetadata();
			Console.WriteLine("Preparing to pre-process");
			processor.CleanupTempDir(TempDir);
			Directory.CreateDirectory(TempDir);
			Console.Write("Extracting Audio... ");
			var audioPath = await processor.ExtractAudio();
			Console.WriteLine("Done");
			Console.Write("Splitting into images, this may take a while... ");
			await processor.SplitVideoIntoImages();
			Console.WriteLine("Done");
			Console.WriteLine("pre-processing complete");

			return new PreProcessResult {Metadata = processor.Metadata, AudioPath = audioPath};
		}

		private static void ResizeAllImages(string inputDir, string outDir, int targetWidth, int targetHeight,
		                                    bool   overwrite = false)
		{
			if (overwrite)
				try
				{
					Directory.Delete(outDir, true);
				}
				catch
				{
					// ignored
				}

			foreach (var file in new DirectoryInfo(inputDir).EnumerateFiles())
			{
				var converter = new Converter {ImagePath = file.FullName};
				converter.WriteResizedImage(targetWidth, targetHeight, outDir);
			}
		}

		private static KeyValuePair<Coordinate, ColouredCharacter>[][] ConvertAllImagesToAscii(
			string imageDirectory, int targetWidth, int targetHeight)
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

		private static void WriteAsciiFrame(IEnumerable<KeyValuePair<Coordinate, ColouredCharacter>> frame, int width,
		                                    int                                                      height)
		{
			var currentFrame = frame as KeyValuePair<Coordinate, ColouredCharacter>[] ?? frame.ToArray();
			for (var y = 0; y < height; y++)
			{
				for (var x = 0; x < width; x++)
					WriteColouredChar(currentFrame.First(f => f.Key.X == x && f.Key.Y == y).Value);
				Console.WriteLine();
			}
		}

		private static void PlayAllFrames(IEnumerable<IEnumerable<KeyValuePair<Coordinate, ColouredCharacter>>> frames,
		                                  double frameRate, int width, int height)
		{
			var frameTimeRawSeconds   = 1 / frameRate;
			var frameTimeSeconds      = (int) Math.Floor(frameTimeRawSeconds);
			var frameTimeMilliseconds = (int) ((frameTimeRawSeconds - frameTimeSeconds) * 1000);

			var frameTime = new TimeSpan(0, 0, 0, frameTimeSeconds, frameTimeMilliseconds);

			foreach (var frame in frames)
			{
				Console.Clear();
				WriteAsciiFrame(frame, width, height);
				Thread.Sleep(frameTime);
			}
		}

		private static void PlayAllFramesMonochrome(IEnumerable<string> frames, double frameRate,
		                                            int                 latencyCorrectionMs = 13)
		{
			var frameTimeRawSeconds   = 1 / frameRate;
			var frameTimeSeconds      = (int) Math.Floor(frameTimeRawSeconds);
			var frameTimeMilliseconds = (int) ((frameTimeRawSeconds - frameTimeSeconds) * 1000);

			var frameTime = new TimeSpan(0, 0, 0, frameTimeSeconds, frameTimeMilliseconds);
			var correctedFrameTime = frameTime.TotalMilliseconds > latencyCorrectionMs
				                         ? frameTime.Subtract(new TimeSpan(0, 0, 0, 0, latencyCorrectionMs))
				                         : frameTime;

			Console.CursorVisible = false;

			foreach (var frame in frames)
			{
				Console.CursorLeft = 0;
				Console.CursorTop  = 0;
				Console.Write(frame);
				Thread.Sleep(correctedFrameTime);
			}

			Console.CursorVisible = true;
		}

		private static string[] FramesToMonochromeStrings(
			IEnumerable<IEnumerable<KeyValuePair<Coordinate, ColouredCharacter>>> frames, int width, int height,
			bool                                                                  ratioCorrection = true)
		{
			Console.Write("Optimising frames for playback... ");

			var working = new List<string>();
			foreach (var frame in frames)
			{
				var currentFrame  = frame as KeyValuePair<Coordinate, ColouredCharacter>[] ?? frame.ToArray();
				var stringBuilder = new StringBuilder();
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
					{
						stringBuilder.Append(currentFrame.First(f => f.Key.X == x && f.Key.Y == y).Value.Character);
						if (ratioCorrection)
							stringBuilder.Append(currentFrame.First(f => f.Key.X == x && f.Key.Y == y).Value.Character);
					}

					stringBuilder.AppendLine();
				}

				working.Add(stringBuilder.ToString());
			}

			Console.WriteLine("Done");

			return working.ToArray();
		}
	}
}