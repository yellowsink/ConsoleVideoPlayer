module ConsoleVideoPlayer.MediaProcessor.PreProcessor

open System
open System.Diagnostics
open System.IO
open Xabe.FFmpeg

let getMetadata videoPath =
    FFmpeg.GetMediaInfo(videoPath) |> Async.AwaitTask

let extractAudio overwrite tempFolder videoPath =
    async {
        let path =
            Path.Combine(tempFolder, "ExtractedAudio")

        let pathMkv = $"%s{path}.mkv"
        let pathWav = $"%s{path}.wav"

        if overwrite then File.Delete pathWav

        let! extraction =
            FFmpeg.Conversions.FromSnippet.ExtractAudio(videoPath, pathMkv)
            |> Async.AwaitTask
            
        do!
            extraction.Start()
            |> Async.AwaitTask
            |> Async.Ignore

        let! conversion =
            FFmpeg.Conversions.FromSnippet.Convert(pathMkv, pathWav)
            |> Async.AwaitTask

        do!
            conversion.Start()
            |> Async.AwaitTask
            |> Async.Ignore

        File.Delete pathMkv

        return pathWav
    }

let splitIntoFrames width height overwrite tempFolder (metadata: IMediaInfo) =
    async {
        let destination = Path.Combine(tempFolder, "raw_frames")

        if overwrite then
            Directory.Delete(destination, true)

        Directory.CreateDirectory destination |> ignore

        let outputFilenameBuilder i =
            "\""
            + Path.Combine(destination, $"image%s{i}.png")
            + "\""

        let fnBuilderFunc =
            Func<string, string> outputFilenameBuilder

        let stream =
            (metadata.VideoStreams |> Seq.head)
                .SetCodec VideoCodec.png

        do!
            FFmpeg
                .Conversions
                .New()
                .AddStream(stream)
                .AddParameter($"-s %i{width}x%i{height}")
                .ExtractEveryNthFrame(1, fnBuilderFunc)
                .UseMultiThread(true)
                .Start()
            |> Async.AwaitTask
            |> Async.Ignore
    }

let cleanupTempDir folder =
    try
        Directory.Delete(folder, true)
    with
    | _ -> ()

let preProcess path width height tempDir =
    task {
        let sw = Stopwatch.StartNew()

        printf "Reading metadata             "
        let! metadata = getMetadata path
        printfn $"Done in %i{sw.ElapsedMilliseconds}ms"

        sw.Restart()
        printf "Preparing to pre-process     "
        cleanupTempDir tempDir
        Directory.CreateDirectory tempDir |> ignore
        printfn $"Done in %i{sw.ElapsedMilliseconds}ms"

        sw.Restart()
        printf "Extracting Audio             "
        let! audioPath = extractAudio false tempDir path
        printfn $"Done in %.2f{sw.Elapsed.TotalSeconds}s"

        sw.Restart()
        printf "Splitting into images        "
        do! splitIntoFrames width height false tempDir metadata
        printfn $"Done in %.2f{sw.Elapsed.TotalSeconds}s"

        return (metadata, audioPath)
    }