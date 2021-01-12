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

		public async Task PopulateMetadata() => Metadata = await FFmpeg.GetMediaInfo(VideoPath);

		public async Task ExtractAudio()
		{
			var tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			                              "Temp\\Cain Atkinson\\ConsoleVideoPlayer");
			var audioPath    = Path.Combine(tempFolder, "ExtractedAudio");
			var audioPathMkv = $"{audioPath}.mkv";
			await FFmpeg.Conversions.FromSnippet.ExtractAudio(VideoPath, audioPathMkv);
			await FFmpeg.Conversions.FromSnippet.Convert(audioPathMkv, $"{audioPath}.wav");
		}
	}
}