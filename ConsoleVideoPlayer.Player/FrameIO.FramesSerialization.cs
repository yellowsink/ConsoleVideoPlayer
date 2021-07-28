using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static partial class FrameIO
	{
		public static class FramesSerialization
		{
			public static byte[] Serialize(SavedFrames frames)
			{
				var serialized = new MemoryStream(); 
				SerializeToStream(frames, serialized, out var byteCount);
				var span = new byte[byteCount];
				serialized.Read(span);

				return span;
			}

			public static void SerializeToStream(SavedFrames frames, Stream stream) => SerializeToStream(frames, stream, out _);
			public static void SerializeToStream(SavedFrames frames, Stream stream, out int byteCount)
			{
				SerializeMetadata(frames, ref stream);
				
				// audio
				// length takes up 4 bytes
				stream.Write(frames.Audio.Length);
				stream.Write(frames.Audio);
				
				// frames
				// frame count takes up 4 bytes
				stream.Write(frames.Frames.Count);

				// the actual frames
				while (frames.Frames.Count > 0)
					stream.Write(frames.Frames.Dequeue());

				byteCount = 4 + 4 + 8 + 4 + frames.Audio.Length + 4 + frames.Frames.Count;
			}

			private static void SerializeMetadata(SavedFrames frames, ref Stream steam)
			{
				var firstFrame    = frames.Frames.Peek().Split("\n");
				var width         = firstFrame[0].Length;
				var height        = firstFrame.Length;
				// 4 bytes
				steam.Write(width);
				// 4 bytes
				steam.Write(height);
				// 8 bytes
				steam.Write(frames.Framerate);
			}

			public static SavedFrames Deserialize(byte[] bytes)  => Deserialize(new MemoryStream(bytes));

			public static SavedFrames Deserialize(Stream stream)
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
				for (var i = 0; i < framesCount; i++)
					frames.Add(stream.ReadString(width * height));

				return new SavedFrames
				{
					Frames    = new Queue<string>(frames),
					Framerate = framerate,
					Audio     = audio
				};
			}
		}
	}

	public static class IoHelpers
	{
		public static byte[] Read(this Stream stream, int byteCount)
		{
			var buffer = new byte[byteCount];
			stream.Read(buffer);
			return buffer;
		}

		public static int ReadInt32(this Stream stream) => BitConverter.ToInt32(stream.Read(4));

		public static double ReadDouble(this Stream stream) => BitConverter.ToDouble(stream.Read(8));

		public static string ReadString(this Stream stream, int charCount)
		{
			var sb = new StringBuilder();
			
			var bytes = stream.Read(charCount * 2);
			foreach (var c in bytes.Separate(2)) sb.Append(BitConverter.ToChar(c.ToArray()));

			return sb.ToString();
		}

		public static void Write(this Stream stream, string str)
		{
			foreach (var c in str) stream.Write(c);
		}
		
		public static void Write(this Stream stream, dynamic item) => stream.Write(BitConverter.GetBytes(item));
		
		public static IEnumerable<IEnumerable<byte>> Separate(this IEnumerable<byte> bytes, int interval)
		{
			var queue   = new Queue<byte>(bytes);
			var working = new List<IEnumerable<byte>>();
			while (queue.Count > 0)
			{
				var section = new List<byte>();
				for (var i = 0; i < interval && queue.Count > 0; i++)
					section.Add(queue.Dequeue());
				working.Add(section);
			}

			return working;
		}
	}
}