using System;
using System.IO;
using MessagePack;
using ZstdNet;

namespace ConsoleVideoPlayer.Player;

[MessagePackObject]
public class Cvid
{
	[Key(2)] public byte[]   Audio = Array.Empty<byte>();
	[Key(1)] public double   Framerate;
	[Key(0)] public string[] Frames = Array.Empty<string>();

	public void Write(string savePath)
	{
		using var stream = new CompressionStream(File.Create(savePath));
		MessagePackSerializer.Serialize(stream, this);
	}

	public static Cvid Read(string savePath)
	{
		using var file = File.OpenRead(savePath);
		try { return MessagePackSerializer.Deserialize<Cvid>(new DecompressionStream(file)); }
		catch (MessagePackSerializationException)
		{
			file.Seek(0, SeekOrigin.Begin);
			return MessagePackSerializer.Deserialize<Cvid>(file);
		}
	}
}