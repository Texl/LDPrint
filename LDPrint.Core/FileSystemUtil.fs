namespace LDPrint.Core

[<RequireQualifiedAccess>]
module File =
    open System.IO
    open System.Text

    let utf8Encoding =
        Encoding.GetEncoding("UTF-8", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)

    let windows1252Encoding =
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)

    let private encodings =
        [ utf8Encoding
          windows1252Encoding
          Encoding.Default ]
    
    let readAllTextAndEncoding (filePath : string) =
        encodings
        |> Seq.choose (fun encoding ->
            try
                use stream = new StreamReader(filePath, encoding)
                let fileContents = stream.ReadToEnd()
                Some (fileContents, stream.CurrentEncoding)
            with exn -> None)
        |> Seq.tryHead
        |> Option.defaultWith (fun () -> failwith $"Unable to read {filePath} with proper encoding.")

    let writeAllLinesAsync (dryRun : bool) (filePath : string) (lines : #seq<string>) (encoding : Encoding) =
        task {
            if dryRun then
                printfn $"{filePath}"
            else
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore
                do! File.WriteAllLinesAsync(filePath, lines, encoding)
        }

    let writeAllTextAsync (dryRun : bool) (filePath : string) (text : string) (encoding : Encoding) =
        task {
            if dryRun then
                printfn $"{filePath}"
            else
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore
                do! File.WriteAllTextAsync(filePath, text, encoding)
        }
