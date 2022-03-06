module ConsoleVideoPlayer.MediaProcessor.Converter

open System
open System.Diagnostics
open System.IO
open System.Text
open SkiaSharp

[<Literal>]
let THREAD_COUNT = 8

let buildAnsiEscape (top: SKColor) (btm: SKColor) prevTop prevBtm (target: StringBuilder) =
    let tChanged = top <> prevTop
    let bChanged = btm <> prevBtm

    if tChanged || bChanged then
        target.Append "\u001b[" |> ignore

        // if we need to, set FG col
        if tChanged then
            target.Append $"38;2;%i{top.Red};%i{top.Green};%i{top.Blue}"
            |> ignore

        // if both, add a separator
        if tChanged && bChanged then
            target.Append ';' |> ignore

        // if we need to, set BG col
        if bChanged then
            target.Append $"48;2;%i{btm.Red};%i{btm.Green};%i{btm.Blue}"
            |> ignore

        target.Append 'm' |> ignore

    target.Append '▀'

let processImage (path: string) =
    let lookupData =
        PixelLookup.makeLookupData (SKBitmap.Decode path)

    let imgWidth = lookupData.Image.Width
    let imgHeight = lookupData.Image.Height
    let colAtCoord = PixelLookup.colourAtCoord lookupData

    let working = StringBuilder()
    // ew
    let mutable prevTop = SKColor.Empty
    let mutable prevBtm = SKColor.Empty

    [|0 .. ((imgHeight / 2) - 1)|]
    |> Array.map ((*) 2)
    |> Array.iter
        (fun y ->
            [|0 .. (imgWidth - 1)|]
            |> Array.iter
                (fun x ->
                    let top = colAtCoord x y
                    let btm = colAtCoord x (y + 1)

                    buildAnsiEscape top btm prevTop prevBtm working
                    |> ignore

                    prevTop <- top
                    prevBtm <- btm)

            working.AppendLine() |> ignore)

    string working

let convertAllImages imageDir =
    printf "Creating ASCII art           "

    let files =
        DirectoryInfo(imageDir).EnumerateFiles()
        |> Seq.sortBy (fun f -> int f.Name.[6..(f.Name.Length - 5)])

    let threadFileLists =
        files
        |> Seq.mapi (fun i f -> (i, f.FullName))
        |> Seq.splitInto THREAD_COUNT

    let sw = Stopwatch.StartNew()

    let computations =
        threadFileLists
        |> Seq.map
            (fun fileList ->
                async {
                    do! Async.SwitchToThreadPool()

                    return
                        fileList
                        |> Array.map (fun (i, path) -> (i, processImage path))
                })

    task {
        let! threadResults = computations |> Async.Parallel

        sw.Stop()

        printfn
            $"Done in %i{sw.Elapsed.Minutes}m %.2f{float sw.Elapsed.Seconds
                                                   + (float sw.Elapsed.Milliseconds / 1000.)}s"

        return
            threadResults
            |> Seq.concat
            |> Seq.sortBy fst
            |> Seq.map snd
    }