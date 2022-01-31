namespace LDPrint.Core

open FParsec

[<RequireQualifiedAccess>]
module LDraw =

    // right-handed coordinate system; -Y is up
    let standardEncoding = System.Text.Encoding.UTF8

    let fileExtensions = [| "ldr"; "dat"; "mpd" |]

    let maxFileNameSize = 255 // Including file extension

    let lineEnding = "\r\n"

    [<Literal>]
    let Command0Marker = '0'

    [<Literal>]
    let Command1Marker = '1'

    [<Literal>]
    let Command2Marker = '2'

    [<Literal>]
    let Command3Marker = '3'

    [<Literal>]
    let Command4Marker = '4'

    [<Literal>]
    let Command5Marker = '5'

    [<Literal>]
    let NewLine = '\n'

[<AutoOpen>]
module LDrawParserTypes =
//    open System.Numerics

    type Title = Title of string
    
    type Color =
        | HexColor of string // Hex string
        | CurrentColor // Color 16 - inherit color from parent
        | ComplementColor // Color 24 - complementary color for line display
        | SpecificColor of int // Some other specific color
        member self.StringValue =
            match self with
            | HexColor str -> str
            | CurrentColor -> "16"
            | ComplementColor -> "24"
            | SpecificColor n -> $"{n}"
//        member self.ToNumeric =
//            match self with
//            | CurrentColor -> 16
//            | ComplementColor -> 24
//            | HexColor hexString -> int hexString 
//            | SpecificColor n -> n
        static member FromNumeric (n : CharParsers.NumberLiteral) =
            if n.IsHexadecimal then
                HexColor n.String
            else
                match int n.String with
                | 16 -> CurrentColor
                | 24 -> ComplementColor
                | n -> SpecificColor n
    
    type ParsedValue<'a> =
        { StringValue : string
          Value : 'a }
    
    type ParsedVector3 = ParsedVector3 of ParsedValue<float32> * ParsedValue<float32> * ParsedValue<float32>

    type ParsedMatrix4x4 = ParsedMatrix4x4 of ParsedValue<float32>[]
    
    type LDrawCommand =
        | Name of name : string
        | Author of author : string
        | Comment of comment : string
        | MetaCommandInvocation of metaCommand : string * metaCommandParameters : string list
        | PartReferenceCommand of color : Color * transformation : ParsedMatrix4x4 * filePath : string
        | LineCommand of color : Color * p1 : ParsedVector3 * p2 : ParsedVector3
        | TriangleCommand of color : Color * p1 : ParsedVector3 * p2 : ParsedVector3 * p3 : ParsedVector3
        | QuadrilateralCommand of color : Color * p1 : ParsedVector3 * p2 : ParsedVector3 * p3 : ParsedVector3 * p4 : ParsedVector3
        | OptionalLineCommand of color : Color * p1 : ParsedVector3 * p2 : ParsedVector3 * p3 : ParsedVector3 * p4 : ParsedVector3
        | BlankLine
        | UnparsedLine of string

    type LDrawFile =
        { Title : Title option
          Commands : LDrawCommand list }

[<RequireQualifiedAccess>]
module ParsedVector3 =

//    let toString (v : ParsedVector3) = $"{v.X} {v.Y} {v.Z}" 
    let toString (ParsedVector3 (x, y ,z)) = $"{x.StringValue} {y.StringValue} {z.StringValue}"

[<RequireQualifiedAccess>]
module ParsedMatrix4x4 =
//    open System.Numerics

//    let toString (m : ParsedMatrix4x4) = $"{m.M41} {m.M42} {m.M43} {m.M11} {m.M21} {m.M31} {m.M12} {m.M22} {m.M32} {m.M13} {m.M23} {m.M33}" 
    let toString (ParsedMatrix4x4 m) =
        m
        |> Seq.map (fun pf -> pf.StringValue)
        |> String.concat " "

[<RequireQualifiedAccess>]
module LDrawFile =
    let toLines (lDrawFile : LDrawFile) = 
        let pointsToString points = points |> Seq.map ParsedVector3.toString |> String.concat " "
        seq {
            match lDrawFile.Title with
            | Some (Title str) -> yield $"0 {str}"
            | _ -> () 

            yield!
                lDrawFile.Commands
                |> List.map (function
                    | Comment comment ->
                        $"0{comment}"
                    | Name name ->
                        $"0 Name:{name}"
                    | Author author ->
                        $"0 Author:{author}"
                    | MetaCommandInvocation (metaCommand, metaCommandParameters) ->
                        let parameters = metaCommandParameters |> String.concat " "
                        $"0 !{metaCommand} {parameters}"
                    | PartReferenceCommand (color, matrix4X4, filePath) ->
                        $"1 {color.StringValue} {ParsedMatrix4x4.toString matrix4X4} {filePath}"
                    | LineCommand (color, p1, p2) ->
                        $"2 {color.StringValue} {pointsToString [ p1; p2 ]}"
                    | TriangleCommand (color, p1, p2, p3) ->
                        $"3 {color.StringValue} {pointsToString [ p1; p2; p3 ]}"
                    | QuadrilateralCommand (color, p1, p2, p3, p4) ->
                        $"4 {color.StringValue} {pointsToString [ p1; p2; p3; p4 ]}"
                    | OptionalLineCommand (color, p1, p2, p3, p4) ->
                        $"5 {color.StringValue} {pointsToString [ p1; p2; p3; p4 ]}"
                    | BlankLine ->
                        ""
                    | UnparsedLine unparsed ->
                        failwith $"Unparsed line: %A{unparsed}")
        }

[<RequireQualifiedAccess>]
module LDrawParser =
    open BasicParsers
 
    let pColor =
        numberLiteral (NumberLiteralOptions.DefaultInteger ||| NumberLiteralOptions.AllowHexadecimal) "color"
        .>> lws
        |>> Color.FromNumeric
        <!> "Color"

    let pParsedFloat32 : Parser<_> =
        numberLiteral (NumberLiteralOptions.DefaultFloat ||| NumberLiteralOptions.AllowFractionWOIntegerPart) "float32"
        |>> fun num ->
            { StringValue = num.String
              Value = float32 num.String }

    let pDimensionalValue =
        pParsedFloat32
        .>> lws
        <!> "Dimensional Value"
    
    let pVector3 =
        tuple3 pDimensionalValue pDimensionalValue pDimensionalValue
        |>> ParsedVector3
        <!> "Vector3"
    
    let pMatrix4x4 =
        parray 12 pDimensionalValue
        |>> function
            | vs when vs.Length = 12 -> ParsedMatrix4x4 vs
            | junk -> failwith $"Matrix4x4 definition of {junk.Length} elements is invalid."
//        |>> function
//            | [| x; y; z; a; b; c; d; e; f; g; h; i |] as parsedFloat32s ->
//                // matrix elements are ordered as x y z / a b c d e f g h i, where x y z is the position vector and a-i are the 3x3 rotation matrix:
//                Matrix4x4(
//                    a, d, g, 0f,    // | a d g 0 |
//                    b, e, h, 0f,    // | b e h 0 |
//                    c, f, i, 0f,    // | c f i 0 |
//                    x, y, z, 1f)    // | x y z 1 |
//            | junk -> failwith $"Matrix4x4 definition of {junk.Length} elements is invalid."
         <!> "Matrix4x4"

    let skipCommandMarker commandMarker =
        skipChar commandMarker
        >>. lws1
        <!> $"Command {commandMarker} Marker"
    
    let pTitleLine =
        skipCommandMarker LDraw.Command0Marker
        >>. restOfLine false
        |>> Title
        <!> "Title"

    let pCommand0 =
        let pMetaCommandInvocation =
            let pMetaCommand = skipChar '!' >>. many1Satisfy ((<>) ' ') .>> lws1
            let pMetaCommandParameters = sepEndBy (many1Satisfy (isNoneOf " \t\n")) lws1 .>> skipRestOfLine false 
            
            skipCommandMarker LDraw.Command0Marker
            >>. (pMetaCommand <!> "meta command") .>>. (pMetaCommandParameters <!> "meta command parameters")
            |>> MetaCommandInvocation
            <!> "MetaCommandInvocation"
            
        let pName =
            skipCommandMarker LDraw.Command0Marker
            >>. skipString "Name:"
            >>. restOfLine false <!> "name content"
            |>> Name
            <!> "Name"
            
        let pAuthor =
            skipCommandMarker LDraw.Command0Marker
            >>. skipString "Author:"
            >>. restOfLine false <!> "author content"
            |>> Author
            <!> "Author"

        let pComment =
            skipChar LDraw.Command0Marker
            >>. opt (restOfLine false) <!> "comment content"
            |>> (function Some c -> Comment c | None -> failwith "empty comment")
            <!> "Comment"

        choice [
            attempt pMetaCommandInvocation
            attempt pName
            attempt pAuthor
            pComment
        ]
        <!> "Command 0"

    let pPartReferenceCommand =
        let pFilePath = many1Satisfy (fun c -> c |> isLetter || c |> isDigit || c |> isAnyOf "._-/\\") .>> lws <!> "FilePath"
        skipCommandMarker LDraw.Command1Marker
        >>. tuple3 pColor pMatrix4x4 pFilePath
        .>> skipRestOfLine false
        |>> PartReferenceCommand
        <!> "PartReferenceCommand"

    let pLineCommand =
        skipCommandMarker LDraw.Command2Marker
        >>. tuple3 pColor pVector3 pVector3
        .>> skipRestOfLine false
        |>> LineCommand
        <!> "LineCommand"

    let pTriangleCommand =
        skipCommandMarker LDraw.Command3Marker
        >>. tuple4 pColor pVector3 pVector3 pVector3
        .>> skipRestOfLine false
        |>> TriangleCommand
        <!> "TriangleCommand"

    let pQuadrilateralCommand =
        skipCommandMarker LDraw.Command4Marker
        >>. tuple5 pColor pVector3 pVector3 pVector3 pVector3
        .>> skipRestOfLine false
        |>> QuadrilateralCommand
        <!> "QuadrilateralCommand"

    let pOptionalLineCommand =
        skipCommandMarker LDraw.Command5Marker
        >>. tuple5 pColor pVector3 pVector3 pVector3 pVector3
        .>> skipRestOfLine false
        |>> OptionalLineCommand
        <!> "OptionalLineCommand"

    let pBlankLineOrUnparsedLine =
        manySatisfy ((<>) '\n')
        |>> (function "" -> BlankLine | str -> failwith str)// UnparsedLine str)
        <!> "Blank line"

    let pLDrawCommand : Parser<LDrawCommand> =
        fun (stream : CharStream<_>) ->
            match stream.Peek() with
            | LDraw.Command0Marker -> pCommand0 stream
            | LDraw.Command1Marker -> pPartReferenceCommand stream
            | LDraw.Command2Marker -> pLineCommand stream
            | LDraw.Command3Marker -> pTriangleCommand stream
            | LDraw.Command4Marker -> pQuadrilateralCommand stream
            | LDraw.Command5Marker -> pOptionalLineCommand stream
            | _ -> pBlankLineOrUnparsedLine stream

    let private pLDrawTitle = 
        ws
        >>. opt pTitleLine
        .>> newline
        <!> "Title line"
    
    let private pLDrawCommands =
        sepEndBy (lws >>. pLDrawCommand) skipNewline
        <!> "Command lines"

    let private pLDrawFile : Parser<LDrawFile> =
        pLDrawTitle .>>. pLDrawCommands
        |>> (fun (title, commands) ->
            { Title = title
              Commands = commands })
        <!> "LDraw file"

    let parseFile (filePath : string) =
        let fileText, fileEncoding = File.readAllTextAndEncoding filePath
        match runParserOnString pLDrawFile () filePath fileText with
        | Success (lDrawFile, _, _) -> Result.Ok lDrawFile
        | Failure (message, _, _) -> Result.Error $"Failure parsing {filePath} - {message}"
