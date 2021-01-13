using System;
using System.Threading.Tasks;

namespace ConsoleVideoPlayer
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			MainAsync().GetAwaiter()
			           .GetResult(); // Do it like this instead of .Wait() to stop exceptions from being wrapped into an AggregateException
		}

		private static async Task MainAsync() => Console.WriteLine("Hello World!");
	}
}