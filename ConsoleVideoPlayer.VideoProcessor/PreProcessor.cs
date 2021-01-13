using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace ConsoleVideoPlayer.VideoProcessor
{
	public class PreProcessor
	{
		public string     VideoPath { get; init; }
		public IMediaInfo Metadata  { get; private set; }

		/// <summary>
		///     Populates the Metadata object
		/// </summary>
		public async Task PopulateMetadata() => Metadata = await FFmpeg.GetMediaInfo(VideoPath);

		/// <summary>
		///     Extracts the audio as WAV from a video into a temp folder
		/// </summary>
		/// <returns>The path to the audio file</returns>
		public async Task<string> ExtractAudio(bool overwrite = false)
		{
			var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			                              @"Temp\Cain Atkinson\ConsoleVideoPlayer");
			var audioPath    = Path.Combine(tempFolder, "ExtractedAudio");
			var audioPathWav = $"{audioPath}.wav";

			await ExtractAudio(audioPath, overwrite);

			return audioPathWav;
		}

		/// <summary>
		///     Extracts the audio as WAV from a video into the specified folder
		/// </summary>
		public async Task ExtractAudio(string destination, bool overwrite = false)
		{
			var audioPathMkv = $"{destination}.mkv";
			var audioPathWav = $"{destination}.wav";

			if (overwrite) File.Delete(audioPathWav);

			await (await FFmpeg.Conversions.FromSnippet.ExtractAudio(VideoPath, audioPathMkv)).Start();
			await (await FFmpeg.Conversions.FromSnippet.Convert(audioPathMkv, audioPathWav)).Start();
			File.Delete(audioPathMkv);
		}

		/// <summary>
		///     Extracts all the images as PNG in a video into a temp folder
		/// </summary>
		/// <returns>The path to the folder containing the images</returns>
		public async Task<string> SplitVideoIntoImages(bool overwrite = false)
		{
			var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			                              @"Temp\Cain Atkinson\ConsoleVideoPlayer\Split Images");
			await SplitVideoIntoImages(tempFolder, overwrite);
			return tempFolder;
		}

		/// <summary>
		///     Extracts all the images as PNG in a video into the specified folder
		/// </summary>
		public async Task SplitVideoIntoImages(string destination, bool overwrite = false)
		{
			if (overwrite) Directory.Delete(destination, true);
			Directory.CreateDirectory(destination);

			string OutputFileNameBuilder(string i) => $"\"{Path.Combine(destination, $"image{i}.png")}\"";

			var info        = await FFmpeg.GetMediaInfo(VideoPath).ConfigureAwait(false);
			var videoStream = info.VideoStreams.First()?.SetCodec(VideoCodec.png);

			await FFmpeg.Conversions.New()
			            .AddStream(videoStream)
			            .ExtractEveryNthFrame(1, OutputFileNameBuilder)
			            .Start();
		}
	}
}