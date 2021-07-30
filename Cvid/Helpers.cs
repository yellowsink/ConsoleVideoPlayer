using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cvid
{
	public static class Helpers
	{
		public static byte[] Read(this Stream stream, int byteCount)
		{
			var buffer = new byte[byteCount];
			stream.Read(buffer);
			return buffer;
		}

		public static int ReadInt32(this Stream stream) => BitConverter.ToInt32(stream.Read(4));

		public static double ReadDouble(this Stream stream) => BitConverter.ToDouble(stream.Read(8));
		
		public static char ReadChar(this Stream stream) => BitConverter.ToChar(stream.Read(2));

		public static string ReadString(this Stream stream, int length)
		{
			var sb = new StringBuilder();

			for (var i = 0; i < length; i++) sb.Append(stream.ReadChar());

			return sb.ToString();
		}

		public static void Write(this Stream stream, string str)
		{
			foreach (var c in str) stream.Write(c);
		}

		public static void Write(this Stream stream, dynamic item) => stream.Write(BitConverter.GetBytes(item));

		public static string BytesToString(this IEnumerable<byte> bytes)
		{
			var sb = new StringBuilder();
			
			foreach (var c in bytes.Separate(2)) sb.Append(BitConverter.ToChar(c.ToArray()));

			return sb.ToString();
		}

		public static IEnumerable<byte> ToBytes(this string str) => str.SelectMany(BitConverter.GetBytes);

		public static byte[] ToByteArray(this string str) => str.ToBytes().ToArray();

		public static byte[][] Separate(this IEnumerable<byte> bytes, int interval)
			=> bytes.Select((b, i) => (i, b))
					.OrderBy(p => p.i)
					.GroupBy(p => p.i / interval)
					.Select(p => p.Select(item => item.b).ToArray())
					.ToArray();
	}
}