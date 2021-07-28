using System.Collections.Generic;
using System.IO;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static partial class FrameIO
	{
		public static void Save(this SavedFrames frames, string savePath)
		{
			var file = File.Create(savePath);
			FramesSerialization.SerializeToStream(frames, file);
		}

		public static SavedFrames ReadFrames(string savePath)
		{
			var file         = File.OpenRead(savePath);
			return FramesSerialization.Deserialize(file);
		}
	}
	
	public class SavedFrames
	{
		public string[] FrameArray
		{
			set => Frames = new(value);
			get => Frames.ToArray();
		}
		public Queue<string> Frames = new();
		public double        Framerate;
		public byte[]        Audio;
	}
}