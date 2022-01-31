namespace LDPrint.Cli

module CliArguments =
    open System
    open Argu
    
    type CliArgument =
        | Dry_Run
        | [<AltCommandLine("-i")>] Input_Path of partPath : string
        | [<AltCommandLine("-o")>] Output_Path of outputPath : string
        | [<AltCommandLine("-s")>] Scale of decimal
        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Dry_Run _ -> "Executes without writing files."
                | Input_Path _ -> "LDraw part library path."
                | Output_Path _ -> "Output path. (default = './output')"
                | Scale _ -> "Output scale. (default = 1, roughly 1 LDU = 0.4mm.)"

    let parser : ArgumentParser<CliArgument> =
        let exitProcess =
            let colorizeErrors =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some ConsoleColor.Red
            ProcessExiter colorizeErrors

        ArgumentParser.Create(errorHandler=exitProcess)
