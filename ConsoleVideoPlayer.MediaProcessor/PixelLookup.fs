module ConsoleVideoPlayer.MediaProcessor.PixelLookup

open SkiaSharp

type LookupData =
    {Image: SKBitmap
     // EW
     mutable PixelCache: SKColor array option}

let makeLookupData image = {Image = image; PixelCache = None}

let colourAtCoord lookup x y =
    let pixelCache =
        match lookup.PixelCache with
        | Some p -> p
        | None -> lookup.Image.Pixels

    // this is not nice but cache gotta cache
    lookup.PixelCache <- Some pixelCache

    pixelCache.[lookup.Image.Width * y + x]