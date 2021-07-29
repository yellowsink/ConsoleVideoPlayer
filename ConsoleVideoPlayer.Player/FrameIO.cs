using System.Collections.Generic;
using System.IO;
using MessagePack;
using MessagePack.Resolvers;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static class FrameIO
	{
		public static void Save(this ParsedCvidFile frames, string savePath)
			=> MessagePackSerializer.Serialize(File.Create(savePath), frames, ContractlessStandardResolver.Options);

		public static ParsedCvidFile ReadFrames(string savePath)
			=> MessagePackSerializer.Deserialize<ParsedCvidFile>(File.OpenRead(savePath));
	}

	[MessagePackObject]
	public class ParsedCvidFile
	{
		[Key("audio")] public byte[]        Audio;
		[Key("rate")]  public double        Framerate;
		[IgnoreMember] public Queue<string> Frames = new();

		[Key("frames")]
		// ReSharper disable once UnusedMember.Global
		public string[] FrameArray
		{
			set => Frames = new Queue<string>(value);
			get => Frames.ToArray();
		}
	}
}