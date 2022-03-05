using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using ConsoleVideoPlayer.MediaProcessor;

namespace ConsoleVideoPlayer.Player;

internal static class Program
{
	private static readonly Stopwatch Stopwatch = new();

	private static string _tempDir = null!;

	private static async Task Main(string[] args)
	{
#if DEBUG
		// ticks per second is equivalent to hertz
		var freq            = Stopwatch.Frequency;
		var isHighPrecision = Stopwatch.IsHighResolution;

		Console.WriteLine($"Timer frequency: {freq / 1_000_000_000}GHz ({freq / 1_000_000}MHz), High precision: {(isHighPrecision ? "Yes" : "No")}");
			
		Console.ReadKey(); // let me get a damn debugger on this
#endif

		var processedArgs = ProcessArgs(args);
		if (string.IsNullOrWhiteSpace(processedArgs.VideoPath))
			return;

		var saveAscii = !string.IsNullOrWhiteSpace(processedArgs.CvidSavePath);

		_tempDir = processedArgs.TempFolderPath
				?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
								@"ConsoleVideoPlayer-Temp");

		LinkedList<string> frames;
		string             audioPath;
		double             frameRate;

		if (processedArgs.UseSavedFrames)
			(frames, frameRate, audioPath) = await ReadSaved(processedArgs);
		else
		{
			var (meta, tempAPath)
				= await PreProcessor.preProcess(processedArgs.VideoPath,
												processedArgs.Width,
												processedArgs.Height,
												_tempDir);

			audioPath = tempAPath;
			frameRate = meta.VideoStreams.First().Framerate;

			if (processedArgs.UseViu)
			{
				Console.Write("\nReady to play video! Press enter to begin playback.");
				Console.ReadLine();
				ViuPlay(audioPath, frameRate, processedArgs.Height);
				return;
			}

			frames
				= new LinkedList<string >(await Converter.convertAllImages(Path.Combine(_tempDir, "raw_frames")));

			// free disk space
			PreProcessor.cleanupTempDir(Path.Combine(_tempDir, "raw_frames"));
			
			if (saveAscii)
			{
				await AsciiSave(audioPath, frames, frameRate, processedArgs);
				return;
			}
		}

		Console.Write("\nReady to play video! Press enter to begin playback.");
		Console.ReadLine();
		AsciiPlay(audioPath, frames, frameRate, processedArgs.Debug);
	}

	private static async Task<(LinkedList<string>, double, string)> ReadSaved(Args processedArgs)
	{
		Console.Write("Loading CVID file... ");
		Stopwatch.Restart();

		var savedFrames = Cvid.Read(processedArgs.VideoPath);
		var frames      = savedFrames.Frames;
		var frameRate   = savedFrames.Framerate;
		var audioPath   = Path.Join(_tempDir, "audio.wav");
		Directory.CreateDirectory(_tempDir);
		await File.WriteAllBytesAsync(audioPath, savedFrames.Audio);

		Stopwatch.Stop();
		Console.WriteLine($"Done in {Math.Round(Stopwatch.Elapsed.TotalSeconds, 2)}s");

		return (frames, frameRate, audioPath);
	}

	private static async Task AsciiSave(string audioPath, LinkedList<string> frames, double frameRate,
										Args   processedArgs)
	{
		Console.Write("Saving to CVID file...       ");
		Stopwatch.Restart();

		var audioBytes = await File.ReadAllBytesAsync(audioPath);
		new Cvid
		{
			Frames    = frames,
			Framerate = frameRate,
			Audio     = audioBytes
		}.Write(processedArgs.CvidSavePath);
		Console.CursorVisible = true;
		Directory.Delete(_tempDir, true);

		Stopwatch.Stop();
		Console.WriteLine($"Done in {Math.Round(Stopwatch.Elapsed.TotalSeconds, 2)}s");

		Console.WriteLine($"\nSaved the converted video to {processedArgs.CvidSavePath}.");
	}

	private static void AsciiPlay(string audioPath, LinkedList<string> frames, double frameRate, bool debug)
	{
		Console.Clear();

		// disable warning as i don't want to await this - i want the execution to just continue!
#pragma warning disable 4014
		new NetCoreAudio.Player().Play(audioPath);
#pragma warning restore 4014

		Player.PlayAsciiFrames(frames, frameRate, debug);

		Directory.Delete(_tempDir, true);
	}

	private static void ViuPlay(string audioPath, double frameRate, int targetHeight)
	{
		Console.Clear();

		// disable warning as i don't want to await this - i want the execution to just continue!
#pragma warning disable 4014
		new NetCoreAudio.Player().Play(audioPath);
#pragma warning restore 4014

		var files = new LinkedList<string>(new DirectoryInfo(Path.Combine(_tempDir, "raw_frames")).EnumerateFiles()
											  .OrderBy(f => Convert.ToInt32(f.Name[new Range(6,
																				f.Name.Length
																			  - 4)]))
											  .Select(f => f.FullName)
											  .ToArray());

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

		if (processedArgs.UseViu && !string.IsNullOrWhiteSpace(processedArgs.CvidSavePath))
		{
			Console.WriteLine("Cannot use viu and save frames together");
			Environment.Exit(2);
		}

		// width and height must be multiples of 2 or stuff breaks
		processedArgs.Width  += processedArgs.Width  % 2;
		processedArgs.Height += processedArgs.Height % 2;

		return processedArgs;
	}
}