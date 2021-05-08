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
		private static string _tempDir;

		private static void Main(string[] args) =>
			MainAsync(args).GetAwaiter()
				.GetResult(); // Do it like this instead of .Wait() to stop exceptions from being wrapped into an AggregateException

		private static async Task MainAsync(IEnumerable<string> args)
		{
			var processedArgs = ProcessArgs(args);
			if (string.IsNullOrWhiteSpace(processedArgs.VideoPath))
				return;

			var targetWidth  = processedArgs.Width;
			var targetHeight = processedArgs.Height;

			var saveAscii = !string.IsNullOrWhiteSpace(processedArgs.AsciiSavePath);

			_tempDir = processedArgs.TempFolderPath ??
			           Path.Combine(
				           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				           @"ConsoleVideoPlayer-Temp");
			
			string[] frames;
			string audioPath;
			double frameRate;

			if (!processedArgs.UseSavedFrames)
			{
				var preProcessResult = await PreProcess(processedArgs.VideoPath);
				frames = ConvertAllImagesToAscii(Path.Combine(_tempDir, "raw_frames"), targetWidth, targetHeight);

				audioPath = preProcessResult.Item2;
				frameRate = preProcessResult.Item1.VideoStreams.First().Framerate;
				
				if (saveAscii)
				{
					var audioBytes = await File.ReadAllBytesAsync(audioPath);
					new SavedFrames
					{
						Frames = frames,
						Framerate = frameRate,
						Audio = audioBytes
					}.Save(processedArgs.AsciiSavePath);
					Console.WriteLine($"Saved the converted video to {processedArgs.AsciiSavePath}.");
					Directory.Delete(_tempDir, true);
					return;
				}

				Console.Write("Ready to play video! Press enter to begin playback.");
				Console.ReadLine();
			}
			else
			{
				var savedFrames = FrameIO.ReadFrames(processedArgs.VideoPath);
				frames = savedFrames.Frames;
				frameRate = savedFrames.Framerate;
				audioPath = Path.Join(_tempDir, "audio.wav");
				Directory.CreateDirectory(_tempDir);
				await File.WriteAllBytesAsync(audioPath, savedFrames.Audio);
			}
			
			Console.Clear();
			
			// disable warning as i don't want to await this - i want the execution to just continue!
#pragma warning disable 4014
			new NetCoreAudio.Player().Play(audioPath);
#pragma warning restore 4014

			PlayAllFrames(frames, frameRate);

			Directory.Delete(_tempDir, true);
		}

		private static Args ProcessArgs(IEnumerable<string> rawArgs)
		{
			var processedArgs = new Args();
			Parser.Default.ParseArguments<Args>(rawArgs).WithParsed(o => { processedArgs = o; });
			return processedArgs;
		}


		private static async Task<(IMediaInfo, string)> PreProcess(string path)
		{
			var processor = new PreProcessor {VideoPath = path, TempFolder = _tempDir};
			Console.WriteLine("Reading metadata");
			await processor.PopulateMetadata();
			Console.WriteLine("Preparing to pre-process");
			PreProcessor.CleanupTempDir(_tempDir);
			Directory.CreateDirectory(_tempDir);
			Console.Write("Extracting Audio... ");
			var audioPath = await processor.ExtractAudio();
			Console.WriteLine("Done");
			Console.Write("Splitting into images, this may use a lot of disk space... ");
			await processor.SplitVideoIntoImages();
			Console.WriteLine("Done");
			Console.WriteLine("pre-processing complete");

			return (processor.Metadata, audioPath);
		}

		private static string[] ConvertAllImagesToAscii(
			string imageDirectory, int targetWidth, int targetHeight)
		{
			Console.Write("Converting all images to ASCII art, this may take a while... ");

			var working = new List<string>();
			var files = new DirectoryInfo(imageDirectory) // the directory
				.EnumerateFiles() // get all files
				.OrderBy(f => Convert.ToInt32(f.Name[new Range(6, f.Name.Length - 4)])); // put them in order!!!
			foreach (var file in files)
			{
				var converter = new Converter {ImagePath = file.FullName};
				var ascii     = converter.ProcessImage(targetWidth, targetHeight);
				working.Add(ascii);
			}

			Console.WriteLine("Done");

			return working.ToArray();
		}

		private static void PlayAllFrames(IEnumerable<string> frames, double frameRate)
		{
			var frameTime   = 1000 / frameRate;

			var timeDebt = 0d;
			
			Console.CursorVisible = false;

			foreach (var frame in frames)
			{
				// setup for measuring later
				var startTime = DateTime.Now;
				
				Console.CursorLeft = 0;
				Console.CursorTop  = 0;
				Console.Write(frame);
				
				// measure the time rendering took
				var renderTime = (DateTime.Now - startTime).TotalMilliseconds;
				// the amount of time we need to compensate for
				var makeupTarget = renderTime + timeDebt;
				// the maximum possible correction to apply this frame
				var correction = Math.Min(makeupTarget, frameTime);
				// if we can't fully make up time try to do it later
				if (makeupTarget > frameTime)
					timeDebt += frameTime - makeupTarget;
				// work out the new time to wait
				var correctedFrameTime = Convert.ToInt32(Math.Round(frameTime - correction));
			
				// wait for it!
				Thread.Sleep(new TimeSpan(0, 0, 0, 0, correctedFrameTime));
			}

			Console.CursorVisible = true;
		}
	}
}