using System.Threading.Tasks;

namespace ConsoleVideoPlayer.Img2Text;

public interface IStringStream
{
	public Task<string> Read();
	
	public bool         Empty { get; }

	public Task<string[]> ReadUntilEmpty();
}