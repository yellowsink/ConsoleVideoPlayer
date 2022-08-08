namespace ConsoleVideoPlayer.MediaProcessor;

/// <summary>
///     An implementation of IFrameStream that converts images on disk to frames
/// </summary>
public class ConvFrameStream : IFrameStream
{
	// how many batches should be processed between GC collections
	public const int BatchCollectionInterval = 10;

	private readonly Queue<string> _inbox  = new();
	private readonly Queue<string> _outbox = new();

	// if -1, ignored, else counts down on every processed item. When 0, running thread will wait for it to not be.
	private int _safeProcessCountdown = -1;


	/// <inheritdoc />
	public FrameStreamStatus Status { get; private set; }

	/// <inheritdoc />
	public int Count { get; private set; }

	/// <inheritdoc />
	public int ReadyCount => _outbox.Count;

	/// <inheritdoc />
	public void Add(IReadOnlyCollection<string> paths)
	{
		Count += paths.Count;
		foreach (var path in paths)
			_inbox.Enqueue(path);
	}

	/// <inheritdoc />
	public void AddAndRun(IReadOnlyCollection<string> paths)
	{
		Add(paths);
		Run();
	}

	/// <inheritdoc />
	public bool TryGet(out string? frame)
	{
		var res = _outbox.TryDequeue(out frame);
		if (res) Count--;
		return res;
	}

	/// <inheritdoc />
	public async Task<string> GetAsync()
	{
		if (TryGet(out var tmp))
			return tmp!;

		while (_outbox.Count == 0)
			await Task.Delay(1);

		Count--;
		return _outbox.Dequeue();
	}

	/// <inheritdoc />
	public async Task<string[]> GetAllRemaining()
	{
		while (Status != FrameStreamStatus.STOPPED)
			await Task.Delay(1);

		var arr = _outbox.ToArray();
		_outbox.Clear();
		return arr;
	}

	/// <inheritdoc />
	public void SafelyProcessMore(int items)
	{
		if (_safeProcessCountdown == -1)
			_safeProcessCountdown  =  items;
		else _safeProcessCountdown += items;
	}

	/// <inheritdoc />
	public void SafelyProcessAll() => _safeProcessCountdown = -1;

	/// <inheritdoc />
	public void Run()
	{
		if (Status != FrameStreamStatus.STOPPED) return;

		Task.Run(async () =>
		{
			while (_inbox.Count != 0)
			{
				var batchCount = 0;
				var looping    = false;
				// a background thread sleeping then checking every 50ms shouldn't be a perf issue
				while (_safeProcessCountdown == 0)
				{
					if (!looping && batchCount == BatchCollectionInterval)
					{
						batchCount = 0;
						GC.Collect();
					}
					else { batchCount++; }

					Status = FrameStreamStatus.IDLE;

					looping = true;
					await Task.Delay(50);
				}

				Status = FrameStreamStatus.WORKING;

				_outbox.Enqueue(Converter.ProcessImage(_inbox.Dequeue()));

				if (_safeProcessCountdown != -1)
					_safeProcessCountdown--;
			}

			Status = FrameStreamStatus.STOPPED;
		});
	}
}