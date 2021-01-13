using CommandLine;

namespace ConsoleVideoPlayer.Player
{
	public class Args
	{
		[Option('r', "read", Required = false)]
		public bool Help { get; set; }

		[Option('v', "video", Required = true)]
		public string VideoPath { get; set; }

		[Option('t', "tempfolder", Required = false)]
		public string TempFolderPath { get; set; }
	}
}