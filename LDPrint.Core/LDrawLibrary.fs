namespace LDPrint.Core

[<AutoOpen>]
module PartLibraryTypes =
    
    type PartLibraryEntry =
        { FilePath : string
          Scaling : decimal
          LDrawFile : LDrawFile }
        
    type PartLibrary =
        { SourcePath : string
          Entries : Result<PartLibraryEntry, string>[] }

[<RequireQualifiedAccess>]
module PartLibrary =
    open System.IO

    let read (scaling : decimal) (inputPath : string) = 
        
        let inputLibraryRoot = Path.GetFullPath inputPath

        let lDrawFilePaths =
            LDraw.fileExtensions
            |> Seq.collect (fun extension ->
                Directory.EnumerateFiles(inputLibraryRoot, $"*.{extension}", EnumerationOptions(RecurseSubdirectories=true))
                |> Seq.map (fun fullFilePath ->
                    Path.GetRelativePath(inputLibraryRoot, fullFilePath)))
            |> Array.ofSeq
            
        let lDrawFiles =
            lDrawFilePaths
            |> Array.map (fun relativeFilePath ->
                let fullPath = Path.Combine(inputLibraryRoot, relativeFilePath)
                let lDrawFileResult = LDrawParser.parseFile fullPath

                lDrawFileResult
                |> Result.map (fun lDrawFile ->
                    { FilePath = relativeFilePath
                      Scaling = scaling
                      LDrawFile = lDrawFile }))
                
        printfn $"{lDrawFiles.Length} files processed."
            
        { SourcePath = inputLibraryRoot
          Entries = lDrawFiles }

    let asyncWrite (dryRun : bool) (outputPath : string) (partLibrary : PartLibrary) =
        let outputLibraryRoot = Path.GetFullPath outputPath

        let partListingFilePath = Path.Combine(outputLibraryRoot, "PartListing.txt")

        let partListingLines =
            partLibrary.Entries
            |> Array.choose (function
                | Ok e ->
                    let title =
                        match e.LDrawFile.Title with
                        | Some (Title title) -> title
                        | None -> "<No title>" 
                    Some $"{e.FilePath}\t{title} "
                | Error _ -> None)

        async {
            do! File.writeAllLinesAsync dryRun partListingFilePath partListingLines LDraw.standardEncoding |> Async.AwaitTask
            
            for result in partLibrary.Entries do
                match result with
                | Ok entry ->
                    let outputFilePath = Path.Combine(outputLibraryRoot, entry.FilePath)
                    let outputText = entry.LDrawFile |> LDrawFile.toLines |> String.concat "\r\n" 
                    do! File.writeAllTextAsync dryRun outputFilePath outputText LDraw.standardEncoding |> Async.AwaitTask
                | Error message ->
                    printfn $"error: {message}"
        }
