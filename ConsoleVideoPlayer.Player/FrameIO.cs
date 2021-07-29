using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using MessagePack;
using MessagePack.Resolvers;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static class FrameIO
	{
		public static void Save(this ParsedCvidFile frames, string savePath) => MessagePackSerializer.Serialize(File.Create(savePath), frames, ContractlessStandardResolver.Options);

		public static ParsedCvidFile ReadFrames(string savePath) => MessagePackSerializer.Deserialize<ParsedCvidFile>(File.OpenRead(savePath));
	}

	[MessagePackObject]
	public class ParsedCvidFile
	{
		[Key("frames")]
		public string[] FrameArray
		{
			set => Frames = new(value);
			get => Frames.ToArray();
		}
		[IgnoreMember] public Queue<string> Frames = new();
		[Key("rate")]  public double        Framerate;
		[Key("audio")] public byte[]        Audio;
	}
}