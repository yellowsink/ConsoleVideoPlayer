using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleVideoPlayer.Img2Text;

public class ConverterStream : IStringStream
{
	private const int ThreadCount = 8;
		
	public ConverterStream((int width, int height) dimensions)
	{
		_dimensions = dimensions;
		Task.Run(async () =>
		{
			while (!_stopThreadTrigger)
			{
				while (_inBuffer.Count > 0)
				{
					var tasks = new List<Task>();
					for (var i = 0; i < ThreadCount && _inBuffer.Count > 0; i++)
						tasks.Add(Task.Run(() => _outBuffer.Enqueue(Converter.ProcessImage(_inBuffer.Dequeue(),
																		_dimensions.width,
																		dimensions.height))));
					await Task.WhenAll(tasks);
				}

				await Task.Delay(10);
			}
		});
	}
	
	~ConverterStream() => _stopThreadTrigger = true;
	
	private readonly Queue<string> _outBuffer = new();
	private readonly Queue<string> _inBuffer  = new();

	private readonly (int width, int height) _dimensions;

	private bool _stopThreadTrigger;
	public async Task<string> Read()
	{
		while (_outBuffer.Count == 0) await Task.Delay(10);
		return _outBuffer.Dequeue();
	}

	public async Task<string[]> ReadUntilEmpty()
	{
		var working = new List<string>();
		while (!Empty) working.Add(await Read());
		return working.ToArray();
	}

	public void Write(string path) => _inBuffer.Enqueue(path);

	public bool Empty => _inBuffer.Count == 0 && _outBuffer.Count == 0;

	public static ConverterStream PrepareFromFiles(IEnumerable<string> files, int width, int height)
	{
		var stream = new ConverterStream((width, height));
		foreach (var file in files) stream.Write(file);
		return stream;
	}

	public static ConverterStream ConvertAllFromFolder(string imageDirectory, int targetWidth, int targetHeight)
	{
		var files = new DirectoryInfo(imageDirectory).EnumerateFiles()
													 .OrderBy(f => Convert.ToInt32(f.Name[new Range(6,
																  f.Name.Length - 4)]))
													 .Select(f => f.FullName)
													 .ToArray();

		return PrepareFromFiles(files, targetWidth, targetHeight);
	}
}