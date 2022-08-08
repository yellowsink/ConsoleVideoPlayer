namespace ConsoleVideoPlayer.MediaProcessor;

/// <summary>
/// A frame stream that just buffers values in memory
/// </summary>
public class MemoryFrameStream : IFrameStream
{
	public MemoryFrameStream() { }
	public MemoryFrameStream(IReadOnlyCollection<string> items) => Add(items);

	public FrameStreamStatus Status     => FrameStreamStatus.STOPPED;
	public int               Count      => _frames.Count;
	public int               ReadyCount => Count;

	private readonly LinkedList<string> _frames = new();

	public void Add(IReadOnlyCollection<string> inItems)
	{
		foreach (var i in inItems)
			_frames.AddLast(i);
	}

	public void AddAndRun(IReadOnlyCollection<string> inItems) => Add(inItems);


	private int _dequeueCounter = 0;
	public bool TryGet(out string? frame)
	{
		frame = _frames.First?.Value;
		if (frame == null) return false;


		if (_dequeueCounter++ == 500)
		{
			GC.Collect();
			_dequeueCounter = 0;
		}
		
		_frames.RemoveFirst();
		return true;
	}

	public async Task<string> GetAsync()
	{
		string? tmp;
		while (!TryGet(out tmp))
			await Task.Delay(1);

		return tmp!;
	}

	public Task<string[]> GetAllRemaining()
	{
		var arr = _frames.ToArray();
		_frames.Clear();
		return Task.FromResult(arr);
	}

	public void SafelyProcessMore(int items) {}
	public void SafelyProcessAll() {}
	public void Run() {}
}