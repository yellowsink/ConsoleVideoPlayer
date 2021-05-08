# ConsoleVideoPlayer
The video player for your terminal.

This project requires a 24bit colour terminal such as kitty.

This is a tool to play a video on your terminal!
It can theoretically render at any resolution,
and at any framerate.

In practice however, it is limited by the speed the terminal
is capable of rendering at.
For example kitty on my laptop fails to render
at 60fps at the default resolution.
I suggest around 30fps for best results.


## How to use
Run the ConsoleVideoPlayer.Player executable and pass it the path to an mp4 file with `-v`. You can optionally specify a custom temp directory to use with `-t`, and specify the width and height to play the video at with `-w` and `-h`.

To save the converted video as a file for later, use `-s` and give a path. To later use this file pass it to `-v` and add `-a`.

### Examples
Convert and play test.mp4
`./ConsoleVideoPlayer.Player -v test.mp4`

Convert test.mp4 and save it to test.convid
`./ConsoleVideoPlayer.Player -v test.mp4 -s test.convid`

Play the converted test.convid without needing to convert
`./ConsoleVideoPlayer.Player -v test.convid -a`

Convert and play test.mp4 at 84x72
`./ConsoleVideoPlayer.Player -v test.mp4 -w 84 -h 72`
