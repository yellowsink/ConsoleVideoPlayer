namespace ConsoleVideoPlayer.MediaProcessor;

public class ConversionStream
{
	private const int BatchSize = 8;

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

		Task.Run(async () =>
		{
			while (_inbox.Count != 0)
			{
				var batch = new List<string>();
				for (var i = 0; i < BatchSize; i++)
					if (_inbox.Count != 0)
						batch.Add(_inbox.Dequeue());

				var tasks = batch.Select((f, i) => Task.Run(() => (i, Converter.ProcessImage(f)))).ToArray();

				var results = await Task.WhenAll(tasks);

				var sorted = results.OrderBy(p => p.i).Select(p => p.Item2);
				foreach (var frame in sorted)
					_outbox.Enqueue(frame);
			}

			IsRunning = false;
		});
	}
}