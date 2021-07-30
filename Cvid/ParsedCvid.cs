using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace Cvid
{
	public class ParsedCvid
	{
		public readonly CvidVersion Version;

		public byte[]        Audio;
		public double        Framerate;
		public Queue<string> Frames = new();

		public string[] FrameArray
		{
			set => Frames = new Queue<string>(value);
			get => Frames.ToArray();
		}
	}

	[MessagePackObject]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once ClassNeverInstantiated.Global
	public class CvidV1Object : ParsedCvid
	{
		[IgnoreMember]
		public new readonly CvidVersion Version = CvidVersion.V1;

		[Key(2)] public new byte[] Audio;
		[Key(1)] public new double Framerate;

		[Key(0)]
		public new string[] FrameArray
		{
			set => Frames = new Queue<string>(value);
			get => Frames.ToArray();
		}
	}

	public enum CvidVersion
	{
		V1 = 1,
		V2 = 2
	}
}