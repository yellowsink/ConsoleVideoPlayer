using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace Cvid
{
	public class ParsedCvid
	{
		public CvidVersion Version { get; init; }

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
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "NotAccessedField.Global")]
	// ReSharper disable once ClassNeverInstantiated.Global
	public class CvidV1Object
	{
		[IgnoreMember] public CvidVersion Version = CvidVersion.V1;

		[Key(2)]       public byte[]        Audio;
		[Key(1)]       public double        Framerate;
		[IgnoreMember] public Queue<string> Frames = new();

		[Key(0)]
		public string[] FrameArray
		{
			set => Frames = new Queue<string>(value);
			get => Frames.ToArray();
		}
		
		// casts and conversions
		public static implicit operator CvidV1Object(ParsedCvid c)
			=> new()
			{
				Frames    = c.Frames,
				Audio     = c.Audio,
				Framerate = c.Framerate
			};
		
		public static implicit operator ParsedCvid(CvidV1Object c)
			=> new()
			{
				Frames    = c.Frames,
				Audio     = c.Audio,
				Framerate = c.Framerate
			};
	}

	public enum CvidVersion
	{
		V1 = 1,
		V2 = 2
	}
}