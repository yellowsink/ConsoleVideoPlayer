using System.Text;
using ImageMagick;

namespace ConsoleVideoPlayer.Img2Text
{
    public class Converter
    {
        public string ImagePath { get; init; }

        public string ProcessImage(int? targetWidth = null, int? targetHeight = null)
        {
            var img = new MagickImage(ImagePath);
            targetWidth ??= img.BaseWidth;
            targetHeight ??= img.BaseHeight;
            var processor = new ImageProcessor {Image = new MagickImage(ImagePath)};
            var resizedImage = ResizeImage(targetWidth.Value, targetHeight.Value, processor);

            var working = new StringBuilder();
            for (var y = 0; y < targetHeight; y += 2)
            {
                for (var x = 0; x < targetWidth; x++)
                {
                    var topCol = resizedImage.ColourFromPixelCoordinate(x, y);
                    var btmCol = resizedImage.ColourFromPixelCoordinate(x, y + 1);
                
                    var topR = topCol.R.ToString();
                    var topG = topCol.G.ToString();
                    var topB = topCol.B.ToString();
                    var btmR = btmCol.R.ToString();
                    var btmG = btmCol.G.ToString();
                    var btmB = btmCol.B.ToString();
                
                    working.Append($"\u001b[38;2;{topR};{topG};{topB};48;2;{btmR};{btmG};{btmB}m"); // Add ANSI escape sequence for colour :)
                    working.Append('▀');
                }
                working.Append('\n');
            }

            return working.ToString();
        }

        private static ImageProcessor ResizeImage(int targetWidth, int targetHeight, ImageProcessor processor)
            => new() {Image = processor.ResizeImage(targetWidth, targetHeight)};
    }
}