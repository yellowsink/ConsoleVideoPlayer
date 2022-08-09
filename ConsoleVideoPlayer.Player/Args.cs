using System;
using System.Collections.Generic;
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

	[Option('y', "kitty", Required = false, HelpText = "Show images with the kitty image protocol")]
	public bool IsKitten { get; set; } // programs that use kitty apis are called kittens

	[Option('t', "tempFolder", Required = false, HelpText = "A custom tmp dir to make use of")]
	public string? TempFolderPath { get; set; }

	public static Args ProcessArgs(IEnumerable<string> rawArgs)
	{
		Args? processedArgs = null;
		Parser.Default.ParseArguments<Args>(rawArgs).WithParsed(o => { processedArgs = o; });

		if (processedArgs == null)
			// parser was unsuccessful
			Environment.Exit(1);
		
		if (processedArgs.IsKitten && processedArgs.UseSavedFrames)
		{
			Console.WriteLine("Cannot use viu and play saved frames together");
			Environment.Exit(1);
		}

		if (processedArgs.IsKitten && !string.IsNullOrWhiteSpace(processedArgs.SavePath))
		{
			Console.WriteLine("Cannot use kitty and save frames together");
			Environment.Exit(2);
		}

		// width and height must be multiples of 2 or stuff breaks
		processedArgs.Width  += processedArgs.Width  % 2;
		processedArgs.Height += processedArgs.Height % 2;

		return processedArgs;
	}
}