using System;
using System.IO;
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
		public async Task<string> ExtractAudio()
		{
			var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			                              "Temp\\Cain Atkinson\\ConsoleVideoPlayer");
			Directory.CreateDirectory(tempFolder);

			var audioPath    = Path.Combine(tempFolder, "ExtractedAudio");
			var audioPathWav = $"{audioPath}.wav";

			await ExtractAudio(audioPath);

			return audioPathWav;
		}

		/// <summary>
		///     Extracts the audio as WAV from a video into the specified folder
		/// </summary>
		public async Task ExtractAudio(string destination)
		{
			var audioPathMkv = $"{destination}.mkv";
			var audioPathWav = $"{destination}.wav";
			await (await FFmpeg.Conversions.FromSnippet.ExtractAudio(VideoPath, audioPathMkv)).Start();
			await (await FFmpeg.Conversions.FromSnippet.Convert(audioPathMkv, audioPathWav)).Start();
			File.Delete(audioPathMkv);
		}
	}
}