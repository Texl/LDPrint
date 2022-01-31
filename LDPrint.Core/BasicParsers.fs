namespace LDPrint.Core

[<AutoOpen>]
module internal BasicParsers =
    open FParsec

    type UserState = unit

    type Parser<'a> = Parser<'a, UserState>
    
    let lws : Parser<_> = skipManySatisfy (isAnyOf "\t ")

    let lws1 : Parser<_> = skipMany1Satisfy (isAnyOf "\t ")
    
    let ws = spaces

    let ws1 = spaces1

[<AutoOpen>]
module ParserDebugging =
    open System
    open System.Diagnostics
    open FParsec

    let BP (p : Parser<_,_>) stream =
        Debugger.Break()
        p stream

    let BPL (p : Parser<_,_>, label : string) stream =
        ignore label
        Debugger.Break()
        p stream // set a breakpoint here

    let private rightArrow = ""
    let private tabWidth = 4
    let mutable debugDepth = 0
    
    let escapeString (str : string) =
        let replacements =
            [ "\r", "\\r"
              "\n", "\\n"
              "\t", "\\t" ]
        (Text.NormalizeNewlines str, replacements)
        ||> List.fold (fun s -> s.Replace)

#if DEBUG_PARSER
    let (<!>) (p: Parser<_,_>) label : Parser<_,_> =
        fun stream ->
            let stopwatch = Stopwatch()
            stopwatch.Start()
            printfn $"[{stream.Position}]\t{(rightArrow.PadLeft(tabWidth * debugDepth + rightArrow.Length))}Begin '{label}': \"{stream.PeekString(256) |> escapeString}\""
            debugDepth <- debugDepth + 1
            let reply = p stream
            debugDepth <- debugDepth - 1
            let durationMilliseconds = stopwatch.Elapsed.TotalMilliseconds
            
            let maxLength = 256
            let ellipsis = "..."
            let truncateLength = maxLength - ellipsis.Length

            let parsedString =
                match reply.Status with
                | Ok ->
                    let resultString = $"%A{reply.Result}" |> escapeString
                    if resultString.Length > maxLength
                    then $" - {resultString.Substring(0, min resultString.Length truncateLength)}{ellipsis}"
                    else $" - {resultString}"
                | _ -> ""
            
            printfn $"[{stream.Position}]\t{rightArrow.PadLeft(tabWidth * debugDepth + rightArrow.Length)}End '{label}': (%A{reply.Status}{parsedString}) ({durationMilliseconds}ms)"
            reply
#else
    let (<!>) (p: Parser<_,_>) label : Parser<_,_> =
        ignore label
        p
#endif
