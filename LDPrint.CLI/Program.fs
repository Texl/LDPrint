open System
open Argu
open LDPrint.Core

let private convertPartLibrary inputPath outputPath outputScale =
    let partLibrary = inputPath |> PartLibrary.read
    partLibrary |> PartLibrary.write outputPath outputScale

type CliArguments =
    | [<AltCommandLine("-i")>] Input_Path of partPath : string
    | [<AltCommandLine("-o")>] Output_Path of outputPath : string
    | [<AltCommandLine("-s")>] Scale of float
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Input_Path _ -> "Path to part file library input."
            | Output_Path _ -> "Path to output. (default='./output/')"
            | Scale _ -> "Output scale. 1 corresponds to actual size, or 1 LDU ~= 0.4mm. (default = 1.)"

let errorHandler =
    function
    | ErrorCode.HelpText -> None
    | _ -> Some ConsoleColor.Red
    |> ProcessExiter

let parser = ArgumentParser.Create(errorHandler = errorHandler)
let parseResults = parser.ParseCommandLine()

let conversionResult =
    let inputPath = parseResults.GetResult(Input_Path)
    let outputPath = parseResults.GetResult(Output_Path, "output")
    let outputScale = parseResults.GetResult(Scale, 1.)
    convertPartLibrary inputPath outputPath outputScale
