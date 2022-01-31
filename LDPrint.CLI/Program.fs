open LDPrint.Core
open LDPrint.Cli.CliArguments

let results = parser.ParseCommandLine()

let dryRun = results.Contains(Dry_Run)
let inputPath = results.GetResult(Input_Path)
let outputPath = results.GetResult(Output_Path, "output")
let scale = results.GetResult(Scale, 1m)

try
    // Read part library
    let partLibrary = PartLibrary.read scale inputPath

    // Write part library
    PartLibrary.asyncWrite dryRun outputPath partLibrary |> Async.RunSynchronously

    printfn "Success"
with exn ->
    printfn $"Error:\n%A{exn}"
