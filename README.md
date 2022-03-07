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
slowly however, so you can enable the debug display with `-d` and
increase the frame skip value `-k` until the time debt stays more
consistently lower.

If you are rendering to viu on kitty (`--viu`), I recommend you set
frame skip to unlimited with `-k -1`, this is because slowdowns from
viu are expected, and the time debt system alone cannot compensate
sufficiently for the lag caused by this alone, whereas frame skipping
allows the player to drop frames constantly to keep up to realtime speed,
at the expense of framerate if your terminal is too slow.

Unrelated fun fact: I stole the time debt system in an earlier state for
the logic in a tetris game I never finished (mainly cause it's awesome!)

## Important notes
This program likes to use multiple gigabytes of RAM.
If you do not have much RAM be aware that your system will be
swapping memory constantly.

## How to use
Run the ConsoleVideoPlayer.Player executable and pass it the
path to an mp4.  See the help `-h` for other args.

To save the converted video as a file for later,
pass a second path.  To later use this file pass it with `-a`.

### Examples
| Functionality | Command |
|-|-|
| Play test.mp4 | `./ConsoleVideoPlayer.Player test.mp4` |
| Convert test.mp4 to test.cvid | `./ConsoleVideoPlayer.Player test.mp4 test.cvid` |
| Play test.cvid | `./ConsoleVideoPlayer.Player -a test.cvid` |
| Play test.mp4 at 84x72 | `./ConsoleVideoPlayer.Player test.mp4 -w 84 -h 72` |
