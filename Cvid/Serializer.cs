using System.Collections.Generic;
using System.IO;

namespace Cvid
{
	public static class Serializer
	{
		public static byte[] Serialize(ParsedCvid cvid)
		{
			using var serialized = new MemoryStream();
			Serialize(cvid, serialized, out var byteCount);
			var span = new byte[byteCount];
			serialized.Read(span);

			return span;
		}

		public static void Serialize(ParsedCvid cvid, Stream stream)
			=> Serialize(cvid, stream, out _);

		public static void Serialize(ParsedCvid cvid, Stream stream, out int byteCount)
		{
			// header to identify cvid version
			stream.WriteByte((byte) 'c');
			stream.WriteByte((byte) 'v');
			stream.WriteByte((byte) cvid.Version);
			
			// metadata
			SerializeMetadata(cvid, ref stream);

			// audio
			// length takes up 4 bytes
			stream.Write(cvid.Audio.Length);
			stream.Write(cvid.Audio);

			// frames
			// frame count takes up 4 bytes
			stream.Write(cvid.Frames.Count);

			// the actual frames
			while (cvid.Frames.Count > 0)
				stream.Write(cvid.Frames.Dequeue());

			byteCount = 4 + 4 + 8 + 4 + cvid.Audio.Length + 4 + cvid.Frames.Count;
		}

		private static void SerializeMetadata(ParsedCvid frames, ref Stream steam)
		{
			var firstFrame = frames.Frames.Peek().Split("\n");
			var width      = firstFrame[0].Length;
			var height     = firstFrame.Length;
			// 4 bytes
			steam.Write(width);
			// 4 bytes
			steam.Write(height);
			// 8 bytes
			steam.Write(frames.Framerate);
		}

		public static ParsedCvid Deserialize(byte[] bytes) => Deserialize(new MemoryStream(bytes));

		public static ParsedCvid Deserialize(Stream stream)
		{
			// read metadata
			var width     = stream.ReadInt32();
			var height    = stream.ReadInt32();
			var framerate = stream.ReadDouble();

			// read audio
			var audioLength = stream.ReadInt32();
			var audio       = stream.Read(audioLength);

			// read frames
			var framesCount = stream.ReadInt32();
			var frames      = new List<string>();
			
			for (var i = 0; i < framesCount; i++) frames.Add(stream.ReadString(width * height));

			return new ParsedCvid
			{
				Frames    = new Queue<string>(frames),
				Framerate = framerate,
				Audio     = audio
			};
		}
	}
	
	public static class SerializerExt
	{
		public static byte[] ToBytes(this ParsedCvid cvid)                => Serializer.Serialize(cvid);
		public static void   ToBytes(this ParsedCvid cvid, Stream stream) => Serializer.Serialize(cvid, stream);

		public static void ToBytes(this ParsedCvid cvid, Stream stream, out int byteCount)
			=> Serializer.Serialize(cvid, stream, out byteCount);
	}
}