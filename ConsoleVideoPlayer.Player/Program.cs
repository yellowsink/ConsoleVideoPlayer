using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleVideoPlayer.MediaProcessor;

namespace ConsoleVideoPlayer.Player;

internal static class Program
{
	private static readonly Stopwatch Stopwatch = new();

	private static string _tempDir = null!;

	private static async Task Main(string[] args)
	{
#if DEBUG
		var freq            = Stopwatch.Frequency;
		var hwTimer = Stopwatch.IsHighResolution;

		Console.WriteLine($"Timer frequency: {freq / 1_000_000_000}GHz ({freq / 1_000_000}MHz), Backend: {(hwTimer ? "OS timer" : "System.DateTime")}");

		Console.ReadKey(); // let me get a debugger on this
#endif

		var processedArgs = Args.ProcessArgs(args);
		if (string.IsNullOrWhiteSpace(processedArgs.VideoPath))
			return;

		var saveAscii = !string.IsNullOrWhiteSpace(processedArgs.SavePath);

		_tempDir = processedArgs.TempFolderPath
				?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
								@"ConsoleVideoPlayer-Temp");

		IFrameStream fstream;
		string           audioPath;
		double           frameRate;

		if (processedArgs.UseSavedFrames)
			(fstream, frameRate, audioPath) = await ReadSaved(processedArgs);
		else
		{
			var (meta, tempAPath) = await PreProcessor.PreProcess(processedArgs.VideoPath,
																  _tempDir,
																  processedArgs.UseViu ? null : processedArgs.Width,
																  processedArgs.UseViu ? null : processedArgs.Height);

			audioPath = tempAPath;
			frameRate = meta.VideoStreams.First().Framerate;

			if (processedArgs.UseViu)
			{
				Console.Write("\nReady to play video! Press enter to begin playback.");
				Console.ReadLine();
				await ViuPlay(audioPath, frameRate, processedArgs.FrameSkip);
				return;
			}

			var dir   = new DirectoryInfo(Path.Combine(_tempDir, "RawFrames"));
			var files = dir.EnumerateFiles().OrderBy(f => int.Parse(f.Name[6..^4])).Select(f => f.FullName);

			fstream = new ConvFrameStream();
			fstream.SafelyProcessMore(Player.FrameBatchSize);
			fstream.AddAndRun(files.ToArray());


			if (saveAscii)
			{
				await AsciiSave(audioPath, fstream, frameRate, processedArgs);
				return;
			}
		}

		Console.Write("\nReady to play video! Press enter to begin playback.");
		Console.ReadLine();
		await AsciiPlay(audioPath, fstream, frameRate, processedArgs.Debug, processedArgs.FrameSkip);
	}

	private static async Task<(IFrameStream, double, string)> ReadSaved(Args processedArgs)
	{
		Console.Write("Loading CVID file... ");
		Stopwatch.Restart();

		var cvid = Cvid.Read(processedArgs.VideoPath);
		var frames      = new MemoryFrameStream(cvid.Frames);
		var frameRate   = cvid.Framerate;
		var audioPath   = Path.Join(_tempDir, "audio.wav");
		Directory.CreateDirectory(_tempDir);
		await File.WriteAllBytesAsync(audioPath, cvid.Audio);

		Stopwatch.Stop();
		Console.WriteLine($"Done in {Math.Round(Stopwatch.Elapsed.TotalSeconds, 2)}s");

		return (frames, frameRate, audioPath);
	}

	private static async Task AsciiSave(string audioPath, IFrameStream frames, double frameRate,
										Args   processedArgs)
	{
		Console.Write("Saving to CVID file...       ");
		Stopwatch.Restart();

		frames.SafelyProcessAll();

		var audioBytes = await File.ReadAllBytesAsync(audioPath);
		new Cvid
		{
			Frames    = await frames.GetAllRemaining(),
			Framerate = frameRate,
			Audio     = audioBytes
		}.Write(processedArgs.SavePath);
		Console.CursorVisible = true;
		Directory.Delete(_tempDir, true);

		Stopwatch.Stop();
		Console.WriteLine($"Done in {Math.Round(Stopwatch.Elapsed.TotalSeconds, 2)}s");

		Console.WriteLine($"\nSaved the converted video to {processedArgs.SavePath}.");
	}

	private static async Task AsciiPlay(string audioPath, IFrameStream frames, double frameRate, bool debug,
										int    skip)
	{
		Console.Clear();

		// disable warning as i don't want to await this - i want the execution to just continue!
		await new NetCoreAudio.Player().Play(audioPath);

		await Player.PlayAsciiFrames(frames, frameRate, debug, skip);

		Directory.Delete(_tempDir, true);
	}

	private static async Task ViuPlay(string audioPath, double frameRate, int skip)
	{
		Console.Clear();

		// disable warning as i don't want to await this - i want the execution to just continue!
		await new NetCoreAudio.Player().Play(audioPath);

		var files = new LinkedList<string>(new DirectoryInfo(Path.Combine(_tempDir, "RawFrames")).EnumerateFiles()
											  .OrderBy(f => Convert.ToInt32(f.Name[new Range(6, f.Name.Length - 4)]))
											  .Select(f => f.FullName));

		Player.PlayViuFrames(files, frameRate, skip);

		Directory.Delete(_tempDir, true);
	}
}