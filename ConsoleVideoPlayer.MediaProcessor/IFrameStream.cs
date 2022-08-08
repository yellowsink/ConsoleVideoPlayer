using System.Diagnostics.CodeAnalysis;

namespace ConsoleVideoPlayer.MediaProcessor;

/// <summary>
/// Current statuses of an IFrameStream
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum FrameStreamStatus
{
	/// <summary>
	/// not running or finished
	/// </summary>
	STOPPED,

	/// <summary>
	/// currently processing frames
	/// </summary>
	WORKING,

	/// <summary>
	/// waiting for a call to the SafelyProcess* methods
	/// </summary>
	IDLE
}

/// <summary>
/// A stream that provides frames on demand to a consumer
/// </summary>
public interface IFrameStream
{
	/// <summary>
	/// The status of the current framestream - see FrameStreamStatus enum
	/// </summary>
	public FrameStreamStatus Status { get; }

	/// <summary>
	/// How many items are in the framestream in total (not all may be dequeueable)
	/// </summary>
	public int Count { get; }

	/// <summary>
	/// How many items are ready to synchronously dequeue right now
	/// </summary>
	public int ReadyCount { get; }

	/// <summary>
	/// Adds some items to the frame stream to be processed
	/// </summary>
	/// <param name="inItems"></param>
	public void Add(IReadOnlyCollection<string> inItems);

	/// <summary>
	/// Adds some items to the frame stream to be processed, and starts processing if not already
	/// </summary>
	/// <param name="inItems"></param>
	public void AddAndRun(IReadOnlyCollection<string> inItems);

	/// <summary>
	/// Gets an item synchronously if possible, returns if successful or not
	/// </summary>
	/// <param name="frame"></param>
	/// <returns></returns>
	public bool TryGet(out string? frame);

	/// <summary>
	/// Gets an item asynchronously, waiting for one to be ready if none are ready
	/// </summary>
	/// <returns></returns>
	public Task<string> GetAsync();

	/// <summary>
	/// Waits for status to be stopped then dequeues all results to an array
	/// </summary>
	/// <returns></returns>
	public Task<string[]> GetAllRemaining();

	/// <summary>
	/// Swaps into capped mode and allows more items to be processed until going idle again
	/// </summary>
	/// <param name="items"></param>
	public void SafelyProcessMore(int items);

	/// <summary>
	/// Swaps into uncapped mode, the frame stream will no longer idle
	/// </summary>
	public void SafelyProcessAll();

	/// <summary>
	/// Does nothing if not stopped. Begins processing items on a bg thread. Goes idle when waiting in capped mode. Stops when runs out of items.
	/// </summary>
	public void Run();
}