using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ConsoleVideoPlayer.Player
{
    public static class Player
    {
        public static void PlayAsciiFrames(IEnumerable<string> frames, double frameRate)
        {
            Console.CursorVisible = false;

            GenericPlay(
                Console.Write,
                frames,
                frameRate);
            
            Console.CursorVisible = true;
        }

        public static void PlayViuFrames(IEnumerable<string> filePaths, double frameRate, int targetHeight)
        {
            // scale values to represent viu better
            targetHeight /= 2;

            GenericPlay(
                path => { Process.Start("viu", $"{path} -h {targetHeight}")?.WaitForExit(); },
                filePaths,
                frameRate);
        }

        private static void GenericPlay<T>(Action<T> renderFunc, IEnumerable<T> iterator, double frameRate)
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