using System;
using System.IO;
using System.Threading.Tasks;
using ConsoleVideoPlayer.VideoProcessor;
using NUnit.Framework;

namespace ConsoleVideoPlayer.Tests
{
	public class PreProcessorTests
	{
		private string _testVideoPath;

		[SetUp]
		public void Setup()
		{
			_testVideoPath = Path.Combine(Environment.CurrentDirectory, "test_vid.mp4");
		}

		[Test]
		public async Task ExtractAudioTest()
		{
			var preProcessor = new PreProcessor
			{
				VideoPath = _testVideoPath
			};

			try
			{
				var path = await preProcessor.ExtractAudio();
				Assert.True(File.Exists(path));
			}
			catch (Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}
	}
}