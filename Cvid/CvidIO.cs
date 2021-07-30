using System;
using System.IO;
using System.Linq;
using MessagePack;

namespace Cvid
{
	// ReSharper disable once InconsistentNaming
	public static class CvidIO
	{
		public static void Write(this ParsedCvid cvid, string savePath, CvidVersion ver = CvidVersion.V2)
		{
			using var file = File.Create(savePath);
			switch (ver)
			{
				case CvidVersion.V1:
					MessagePackSerializer.Serialize(file, (CvidV1Object) cvid);
					break;
				case CvidVersion.V2:
					cvid.ToBytes(file);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(ver), ver, null);
			}
		}

		public static ParsedCvid Read(string savePath)
		{
			using var file = File.OpenRead(savePath);
			// jank but ok
			return GetCvidVersion(file) == CvidVersion.V2
					   ? Serializer.Deserialize(file)
					   : MessagePackSerializer.Deserialize<CvidV1Object>(file);
		}

		private static CvidVersion GetCvidVersion(Stream bytes)
		{
			var first2Bytes                   = bytes.Read(2);
			var thirdByte                     = bytes.ReadByte();
			if (bytes.CanSeek) bytes.Position -= 3;

			// i know this is dumb but oh well - cvid >=2 should always start cv then ver, cvid 1 is just msgpack so it doesnt
			return first2Bytes.SequenceEqual(new[] { (byte) 'c', (byte) 'v' })
					   ? (CvidVersion) thirdByte
					   : CvidVersion.V1;
		}
	}
}