using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleVideoPlayer.VideoProcessor;
using NUnit.Framework;

namespace ConsoleVideoPlayer.Tests
{
	[TestFixture]
	public class PreProcessorTests
	{
		[SetUp] // Setup is called before each test
		public void Setup()
		{
			_testVideoPath = Path.Combine(Environment.CurrentDirectory, "test_vid.mp4");
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						 @"Temp\Cain Atkinson\ConsoleVideoPlayer");
		}

		private string _testVideoPath;

		[Test]
		public async Task ExtractAudioTest()
		{
			var preProcessor = new PreProcessor
			{
				VideoPath = _testVideoPath
			};

			try
			{
				var path = await preProcessor.ExtractAudio(true);
				Assert.True(File.Exists(path));
			}
			catch (Exception ex) { Assert.Fail(ex.Message); }
		}

		[Test]
		public async Task SplitVideoIntoImagesTest()
		{
			var preProcessor = new PreProcessor
			{
				VideoPath = _testVideoPath
			};

			try
			{
				var path = await preProcessor.SplitVideoIntoImages();
				Assert.True(Directory.EnumerateFiles(path).Any());
			}
			catch (Exception ex) { Assert.Fail(ex.Message); }
		}
	}
}