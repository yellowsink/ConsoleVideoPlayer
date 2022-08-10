<h1 align="center">ConsoleVideoPlayer</h1>
<p align="center">The video player for your terminal.</p>

[Proof of concept video (bad apple of course!)](https://youtu.be/cc2f94KSjIQ)

Best experienced with a 24bit colour terminal such as kitty.
Does work without though (try it on the Linux TTY if you want!).

This is a tool to play a video on your terminal!
It can theoretically render at any resolution,
and at any framerate.

In practice however, it is limited by the speed the terminal
is capable of rendering at.

## Tuning away lag

This tool has a powerful lag compensation system known as the
time debt system. On it's own it is usually good enough to deal
with lag from slow frames on terminals that can ordinarily keep
the rendering speed high enough.

Some terminals at some resolutions may consistently render very
slowly however, so frame skipping is allowed by default.
You may still enable the debug display with `-d` and
play with the frame skip value `-k` until its tweaked just right.

If you are rendering to kitty (`--kitty`), I recommend you leave frame
skip on unlimited, this is because slowdowns are expected,
and the time debt system alone might not compensate
sufficiently for the lag caused by this alone, whereas frame skipping
allows the player to drop frames constantly to keep up to realtime speed,
at the expense of framerate if your terminal is too slow.

Update 2022-08-10: The system has been rewritten from the ground up.
It now does not drift over time, and users on Windows especially will
find that they experience much more accurate timing.

Unrelated fun fact: I stole a very early version of time debt for
the logic in a tetris game I never finished (mainly cause it's awesome!)

## How to use
Run the ConsoleVideoPlayer.Player executable and pass it the
path to an mp4. Run with no args to see help.