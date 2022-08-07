namespace ConsoleVideoPlayer.MediaProcessor;

// The ascii frame conversion is actually fast enough now since the overhaul that multithreading is unnecessary

public class ConversionStream
{
	public bool IsRunning;

	public int Count;

	private readonly Queue<string> _inbox  = new();
	private readonly Queue<string> _outbox = new();

	public void Add(IReadOnlyCollection<string> paths)
	{
		Count += paths.Count;
		foreach (var path in paths)
			_inbox.Enqueue(path);
	}

	public void AddAndRun(IReadOnlyCollection<string> paths)
	{
		Add(paths);
		Run();
	}

	public bool TryGet(out string? frame)
	{
		var res = _outbox.TryDequeue(out frame);
		if (res) Count--;
		return res;
	}

	public async Task<string> GetAsync()
	{
		if (TryGet(out var tmp))
			return tmp!;

		while (_outbox.Count == 0)
			await Task.Delay(1);

		Count--;
		return _outbox.Dequeue();
	}

	public void Run()
	{
		if (IsRunning) return;

		IsRunning = true;

		Task.Run(() =>
		{
			while (_inbox.Count != 0)
				_outbox.Enqueue(Converter.ProcessImage(_inbox.Dequeue()));

			IsRunning = false;
		});
	}
}