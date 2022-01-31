namespace LDPrint.Core.Tests

module FileSystemUtil =
    open System.IO
    open Expecto
    open LDPrint.Core

    module TestData =
        let partsLibraryPath = "./ldraw"

    [<Tests>]
    let tests =
        testList "LDrawParser" [
            test "LDrawParser.parseFile" {
                let files =
                    Directory.EnumerateFiles(TestData.partsLibraryPath, "*.dat", EnumerationOptions(RecurseSubdirectories=true))
                    |> Seq.filter (fun filePath -> filePath.EndsWith("2902s01.dat"))
                    |> Seq.map (fun filePath ->
                        let result = LDrawParser.parseFile filePath
                        filePath, result)
                    |> Array.ofSeq
                    
                files
                |> Array.iter (snd >> function
                    | Result.Ok _ -> ()
                    | Error error ->
                        printfn $"{error}"
                        failwith error)
            }
        ]
