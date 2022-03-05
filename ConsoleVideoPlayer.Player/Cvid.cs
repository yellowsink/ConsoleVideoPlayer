using System.Collections.Generic;
using System.IO;
using MessagePack;
using ZstdNet;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static class Cvid
	{
		public static void Write(this ParsedCvid cvid, string savePath)
		{
			using var stream = new CompressionStream(File.Create(savePath));
			MessagePackSerializer.Serialize(stream, cvid);
		}

		public static ParsedCvid Read(string savePath)
		{
			using var file = File.OpenRead(savePath);
			try { return MessagePackSerializer.Deserialize<ParsedCvid>(new DecompressionStream(file)); }
			catch (MessagePackSerializationException)
			{
				file.Seek(0, SeekOrigin.Begin);
				return MessagePackSerializer.Deserialize<ParsedCvid>(file);
			}
		}
	}

	[MessagePackObject]
	public class ParsedCvid
	{
		[Key(2)]       public byte[]        Audio;
		[Key(1)]       public double        Framerate;
		[IgnoreMember] public Queue<string> Frames = new();

		[Key(0)]
		public string[] FrameArray
		{
			set => Frames = new Queue<string>(value);
			get => Frames.ToArray();
		}
	}
}