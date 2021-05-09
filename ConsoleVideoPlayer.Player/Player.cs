using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ConsoleVideoPlayer.Player
{
    public static class Player
    {
        /// <summary>
        /// Renders all frames
        /// </summary>
        public static void PlayAsciiFrames(IEnumerable<string> frames, double frameRate)
        {
            Console.CursorVisible = false;

            GenericPlay(
                frames,
                Console.Write,
                frameRate);
            
            Console.CursorVisible = true;
        }

        /// <summary>
        /// Renders all file paths as images using viu - very slow
        /// </summary>
        public static void PlayViuFrames(IEnumerable<string> filePaths, double frameRate, int targetHeight)
        {
            // scale values to represent viu better
            targetHeight /= 2;

            void RenderFunc(string path)
                => Process.Start("viu", $"{path} -h {targetHeight}");

            GenericPlay(
                filePaths,
                RenderFunc,
                frameRate);
        }

        /// <summary>
        /// Executes an arbitrary function for all items in the enumerable, and keeps in time with the framerate
        /// </summary>
        private static void GenericPlay<T>(IEnumerable<T> iterator, Action<T> renderFunc, double frameRate)
        {
            var frameTime = 1000 / frameRate;

            var timeDebt = 0d;

            Console.CursorVisible = false;

            foreach (var iterable in iterator)
            {
                // setup for measuring later
                var startTime = DateTime.Now;

                Console.CursorLeft = 0;
                Console.CursorTop = 0;
                renderFunc(iterable);

                // measure the time rendering took
                var renderTime = (DateTime.Now - startTime).TotalMilliseconds;
                // the amount of time we need to compensate for
                var makeupTarget = renderTime + timeDebt;
                // timeDebt has been accounted for, reset it!
                timeDebt = 0;
                // the maximum possible correction to apply this frame
                var correction = Math.Min(makeupTarget, frameTime);
                // if we can't fully make up time try to do it later
                if (makeupTarget > frameTime)
                    timeDebt += makeupTarget - frameTime;
                // compensate for rounding
                var toWait = frameTime - correction;
                timeDebt += toWait - Math.Floor(toWait);
                // work out the new time to wait
                var correctedFrameTime = Convert.ToInt32(Math.Floor(toWait));

                // wait for it!
                Thread.Sleep(new TimeSpan(0, 0, 0, 0, correctedFrameTime));
            }
        }
    }
}