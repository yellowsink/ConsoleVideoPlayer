using System.Collections.Generic;
using System.IO;
using MessagePack;
using MessagePack.Resolvers;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static class FrameIO
	{
		public static void Save(this SavedFrames frames, string savePath)
		{
			var msgpack = MessagePackSerializer.Serialize(frames, ContractlessStandardResolver.Options);
			File.WriteAllBytes(savePath, msgpack);
		}

		public static SavedFrames ReadFrames(string savePath)
		{
			var msgpack = File.ReadAllBytes(savePath);
			return MessagePackSerializer.Deserialize<SavedFrames>(msgpack);
		}
	}

	[MessagePackObject]
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