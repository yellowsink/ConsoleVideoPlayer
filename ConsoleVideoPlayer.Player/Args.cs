using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace ConsoleVideoPlayer.Player;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class Args
{
	[Value(0, MetaName = "video", Required = true, HelpText = "The path of the video or cvid to play")]
	public string VideoPath { get; set; } = "";

	[Option('h', "height", Required = false, HelpText = "You should be able to figure this out", Default = 72)]
	public int Height { get; set; }

	[Option('w', "width", Required = false, HelpText = "And this too", Default = 128)]
	public int Width { get; set; }

	[Option('k',
			"frameSkip",
			Required = false,
			HelpText = "Drop at most set amount of consecutive frames if needed. -1 = uncapped, 0 = no skip.",
			Default = -1)]
	public int FrameSkip { get; set; }

	[Value(1, MetaName = "savePath", Required = false, HelpText = "Where to optionally save a cvid to")]
	public string SavePath { get; set; } = "";

	[Option('a', "useSavedFrames", Required = false, HelpText = "path is a cvid")]
	public bool UseSavedFrames { get; set; }

	[Option('d', "debug", Required = false, HelpText = "Show debug stats in the player")]
	public bool Debug { get; set; }

	[Option('i', "viu", Required = false, HelpText = "Show images with viu")]
	public bool UseViu { get; set; }

	[Option('t', "tempFolder", Required = false, HelpText = "A custom tmp dir to make use of")]
	public string? TempFolderPath { get; set; }
}