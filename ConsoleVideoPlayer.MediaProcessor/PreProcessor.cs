using System.Diagnostics;
using Xabe.FFmpeg;

namespace ConsoleVideoPlayer.MediaProcessor;

public static class PreProcessor
{
	public static Task<IMediaInfo> GetMetadata(string videoPath) => FFmpeg.GetMediaInfo(videoPath);

	public static async Task<string> ExtractAudio(string videoPath, string tempDir, bool overwrite = false)
	{
		var path = Path.Combine(tempDir, "ExtractedAudio");

		var pathMkv = path + ".mkv";
		var pathWav = path + ".wav";

		if (overwrite) File.Delete(pathWav);

		await (await FFmpeg.Conversions.FromSnippet.ExtractAudio(videoPath, pathMkv)).Start();
		await (await FFmpeg.Conversions.FromSnippet.Convert(pathMkv, pathWav)).Start();

		File.Delete(pathMkv);

		return pathWav;
	}

	public static async Task SplitIntoFrames(IMediaInfo metadata, int width, int height, string tempDir,
											 bool       overwrite = false)
	{
		var dest = Path.Combine(tempDir, "RawFrames");

		if (overwrite) CleanupTempDir(dest);

		Directory.CreateDirectory(dest);

		string NameBuilder(string i) => $"\"{Path.Combine(dest, $"image{i}.png")}\"";

		var stream = metadata.VideoStreams.FirstOrDefault()?.SetCodec(VideoCodec.png);

		if (stream == null)
			throw new InvalidDataException("input media must have a video stream");

		await FFmpeg.Conversions.New()
					.AddStream(stream)
					.AddParameter($"-s {width}x{height}")
					.ExtractEveryNthFrame(1, NameBuilder)
					.UseMultiThread(true)
					.Start();
	}
	
	public static void CleanupTempDir(string path)
	{
		try { Directory.Delete(path, true); }
		catch
		{ // ignored
		}
	}

	public static async Task<(IMediaInfo, string)> PreProcess(string videoPath, string tempDir, int width, int height)
	{
		var sw = Stopwatch.StartNew();
		Console.Write("Preparing to pre-process     ");
		var metadata = await GetMetadata(videoPath);
		CleanupTempDir(tempDir);
		Directory.CreateDirectory(tempDir);
		Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms");
		
		sw.Restart();
		Console.Write("Extracting Audio             ");
		var audioPath = await ExtractAudio(videoPath, tempDir);
		Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F2}s");
		
		sw.Restart();
		Console.Write("Splitting into images        ");
		await SplitIntoFrames(metadata, width, height, tempDir);
		Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F2}s");

		return (metadata, audioPath);
	}
}