using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using ConsoleVideoPlayer.Img2Text;
using ConsoleVideoPlayer.VideoProcessor;
using Xabe.FFmpeg;

namespace ConsoleVideoPlayer.Player
{
	internal static class Program
	{
		private static readonly Stopwatch Stopwatch = new();

		private static string _tempDir;

		private static async Task Main(string[] args)
		{
#if DEBUG
			PrintTimerInfo();
			Console.ReadKey(); // let me get a damn debugger on this
#endif

			var processedArgs = ProcessArgs(args);
			if (string.IsNullOrWhiteSpace(processedArgs.VideoPath))
				return;

			var saveAscii = !string.IsNullOrWhiteSpace(processedArgs.AsciiSavePath);

			_tempDir = processedArgs.TempFolderPath
					?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
									@"ConsoleVideoPlayer-Temp");

			string[] frames;
			string   audioPath;
			double   frameRate;

			if (!processedArgs.UseSavedFrames)
			{
				var (meta, tempAPath) = await PreProcess(processedArgs.VideoPath);
				audioPath             = tempAPath;
				frameRate             = meta.VideoStreams.First().Framerate;

				if (processedArgs.UseViu)
				{
					Console.Write("\nReady to play video! Press enter to begin playback.");
					Console.ReadLine();
					ViuPlay(audioPath, frameRate, processedArgs.Height);
					return;
				}

				frames = Converter.ConvertAllImagesToAscii(Path.Combine(_tempDir, "raw_frames"),
														   processedArgs.Width,
														   processedArgs.Height);

				if (saveAscii)
				{
					await AsciiSave(audioPath, frames, frameRate, processedArgs);
					return;
				}

				Console.Write("\nReady to play video! Press enter to begin playback.");
				Console.ReadLine();
			}
			else { (frames, frameRate, audioPath) = await ReadSaved(processedArgs); }

			AsciiPlay(audioPath, frames, frameRate);
		}

		private static async Task<(string[], double, string)> ReadSaved(Args processedArgs)
		{
			var savedFrames = FrameIO.ReadFrames(processedArgs.VideoPath);
			var frames      = savedFrames.Frames;
			var frameRate   = savedFrames.Framerate;
			var audioPath   = Path.Join(_tempDir, "audio.wav");
			Directory.CreateDirectory(_tempDir);
			await File.WriteAllBytesAsync(audioPath, savedFrames.Audio);
			return (frames, frameRate, audioPath);
		}

		private static async Task AsciiSave(string audioPath, string[] frames, double frameRate, Args processedArgs)
		{
			var audioBytes = await File.ReadAllBytesAsync(audioPath);
			new SavedFrames
			{
				Frames    = frames,
				Framerate = frameRate,
				Audio     = audioBytes
			}.Save(processedArgs.AsciiSavePath);
			Console.WriteLine($"\nSaved the converted video to {processedArgs.AsciiSavePath}.");
			Console.CursorVisible = true;
			Directory.Delete(_tempDir, true);
		}

		private static void AsciiPlay(string audioPath, IEnumerable<string> frames, double frameRate)
		{
			Console.Clear();

			// disable warning as i don't want to await this - i want the execution to just continue!
#pragma warning disable 4014
			new NetCoreAudio.Player().Play(audioPath);
#pragma warning restore 4014

			Player.PlayAsciiFrames(frames, frameRate);

			Directory.Delete(_tempDir, true);
		}

		private static void ViuPlay(string audioPath, double frameRate, int targetHeight)
		{
			Console.Clear();

			// disable warning as i don't want to await this - i want the execution to just continue!
#pragma warning disable 4014
			new NetCoreAudio.Player().Play(audioPath);
#pragma warning restore 4014

			var files = new DirectoryInfo(Path.Combine(_tempDir, "raw_frames")).EnumerateFiles()
			   .OrderBy(f => Convert.ToInt32(f.Name[new Range(6, f.Name.Length - 4)]))
			   .Select(f => f.FullName)
			   .ToArray();

			Player.PlayViuFrames(files, frameRate, targetHeight);

			Directory.Delete(_tempDir, true);
		}

		private static Args ProcessArgs(IEnumerable<string> rawArgs)
		{
			var processedArgs = new Args();
			Parser.Default.ParseArguments<Args>(rawArgs).WithParsed(o => { processedArgs = o; });

			if (processedArgs.UseViu && processedArgs.UseSavedFrames)
			{
				Console.WriteLine("Cannot use viu and play saved frames together");
				Environment.Exit(1);
			}

			if (processedArgs.UseViu && !string.IsNullOrWhiteSpace(processedArgs.AsciiSavePath))
			{
				Console.WriteLine("Cannot use viu and save frames together");
				Environment.Exit(2);
			}

			// width and height must be multiples of 2 or stuff breaks
			processedArgs.Width  += processedArgs.Width  % 2;
			processedArgs.Height += processedArgs.Height % 2;

			return processedArgs;
		}

		private static async Task<(IMediaInfo, string)> PreProcess(string path)
		{
			Stopwatch.Start();
			var processor = new PreProcessor { VideoPath = path, TempFolder = _tempDir };
			Console.Write("Reading metadata             ");
			await processor.PopulateMetadata();
			Console.WriteLine($"Done in {Stopwatch.ElapsedMilliseconds}ms");

			Stopwatch.Restart();
			Console.Write("Preparing to pre-process     ");
			PreProcessor.CleanupTempDir(_tempDir);
			Directory.CreateDirectory(_tempDir);
			Console.WriteLine($"Done in {Stopwatch.ElapsedMilliseconds}ms");

			Stopwatch.Restart();
			Console.Write("Extracting Audio             ");
			var audioPath = await processor.ExtractAudio();
			Console.WriteLine($"Done in {Stopwatch.ElapsedMilliseconds / 1000}s");

			Stopwatch.Restart();
			Console.Write("Splitting into images        ");
			await processor.SplitVideoIntoImages();
			Console.WriteLine($"Done in {Stopwatch.ElapsedMilliseconds / 1000}s");

			Stopwatch.Stop();

			return (processor.Metadata, audioPath);
		}

#if DEBUG
		private static void PrintTimerInfo()
		{
			// ticks per second is equivalent to hertz
			var freq            = Stopwatch.Frequency;
			var isHighPrecision = Stopwatch.IsHighResolution;

			Console.WriteLine($"Timer frequency: {freq / 1_000_000_000}GHz ({freq / 1_000_000}MHz), High precision: {(isHighPrecision ? "Yes" : "No")}");
		}
#endif
	}
}