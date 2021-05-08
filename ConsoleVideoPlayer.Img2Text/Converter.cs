using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
    public class Converter
    {
        public string ImagePath { get; init; }

        public ((int, int), Color, Color)[] ProcessImage(int? targetWidth = null, int? targetHeight = null)
        {
            var img = new MagickImage(ImagePath);
            targetWidth ??= img.BaseWidth;
            targetHeight ??= img.BaseHeight;
            var processor = new ImageProcessor {Image = new MagickImage(ImagePath)};
            var resizedImage = ResizeImage(targetWidth.Value, targetHeight.Value, processor);

            var working = new List<((int, int), Color, Color)>();
            for (var y = 0; y < targetHeight; y+=2)
            for (var x = 0; x < targetWidth; x++)
                working.Add((
                    (x, y),
                    resizedImage.ColourFromPixelCoordinate(x, y),
                    resizedImage.ColourFromPixelCoordinate(x, y + 1)
                    ));

            return working.ToArray();
        }

        public void WriteResizedImage(int targetWidth, int targetHeight, string outDir,
            string fileName = null)
        {
            Directory.CreateDirectory(outDir);

            var imgProc = new ImageProcessor {Image = new MagickImage(ImagePath)};

            fileName ??= new FileInfo(imgProc.Image.FileName).Name;

            var resized = ResizeImage(targetWidth, targetHeight, imgProc);
            resized.Image.Write(Path.Combine(outDir, fileName));
        }

        private static ImageProcessor ResizeImage(int targetWidth, int targetHeight, ImageProcessor processor)
            => new() {Image = processor.ResizeImage(targetWidth, targetHeight)};
    }
}