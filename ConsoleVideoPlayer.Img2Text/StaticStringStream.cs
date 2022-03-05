using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleVideoPlayer.Img2Text;

public class StaticStringStream : IStringStream
{
	
	private readonly Queue<string> _buffer;

	public StaticStringStream(IEnumerable<string> buffer) => _buffer = new Queue<string>(buffer);

	public Task<string>   Read()           => Task.FromResult(_buffer.Dequeue());
	public Task<string[]> ReadUntilEmpty() => Task.FromResult(_buffer.ToArray());

	public bool Empty => _buffer.Count == 0;
}