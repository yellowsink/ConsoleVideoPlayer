using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace ConsoleVideoPlayer.Player
{
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
	[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
	public class Args
	{
		[Option('v', "video", Required = true)]
		public string VideoPath { get; set; } = "";

		[Option('t', "tempfolder", Required = false)]
		public string? TempFolderPath { get; set; }

		[Option('h', "height", Required = false)]
		public int Height { get; set; } = 72;

		[Option('w', "width", Required = false)]
		public int Width { get; set; } = 128;

		[Option('s', "cvidSavePath", Required = false)]
		public string CvidSavePath { get; set; } = "";

		[Option('a', "useSavedFrames", Required = false)]
		public bool UseSavedFrames { get; set; }

		[Option('i', "viu", Required = false)]
		public bool UseViu { get; set; }

		[Option('d', "debug", Required = false)]
		public bool Debug { get; set; }
	}
}