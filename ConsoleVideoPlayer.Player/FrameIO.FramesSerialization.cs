using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleVideoPlayer.Player
{
	// ReSharper disable once InconsistentNaming
	public static partial class FrameIO
	{
		public static class FramesSerialization
		{
			private const int THREADS = 8;
			
			public static byte[] Serialize(SavedFrames frames)
			{
				var serialized = new MemoryStream();
				SerializeToStream(frames, serialized, out var byteCount);
				var span = new byte[byteCount];
				serialized.Read(span);

				return span;
			}

			public static void SerializeToStream(SavedFrames frames, Stream stream)
				=> SerializeToStream(frames, stream, out _);

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

			public static SavedFrames Deserialize(byte[] bytes) => Deserialize(new MemoryStream(bytes));

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
				var framesCount  = stream.ReadInt32();
				var framesBuffer = stream.Read(framesCount * 2     * width * height);
				var framesRaw    = framesBuffer.Separate(2 * width * height);

				var framesPerThread = new List<(int, byte[])>[THREADS];
				for (var i = 0; i < framesRaw.Length; i++)
				{
					framesPerThread[i % THREADS] ??= new List<(int, byte[])>();
					
					framesPerThread[i % THREADS].Add((i, framesRaw[i]));
				}

				var tasks = new List<Task<(int, string)[]>>();
				foreach (var threadFrames in framesPerThread)
					tasks.Add(Task.Run(() => threadFrames.Select(frame => (frame.Item1, frame.Item2.BytesToString())).ToArray()));

				Task.WaitAll(tasks.ToArray());

				var frames = tasks.SelectMany(t => t.Result)
								  .OrderBy(p => p.Item1)
								  .Select(p => p.Item2)
								  .ToArray();

				return new SavedFrames
				{
					Frames    = new Queue<string>(frames),
					Framerate = framerate,
					Audio     = audio
				};
			}
		}
	}
}