open System
open System.IO

let delimiter = '\t'

let processLines (lines: string[]) : string[][] =
    lines
    |> Array.map (fun line ->
        if line = "" then
            [||]
        else
            line.Split(delimiter)
    )

let processFile (filePath: string) : Result<string[][], string> =
    try
        if not (File.Exists filePath) then
            Error (sprintf "The file '%s' does not exist." filePath)
        else
            let lines = File.ReadAllLines filePath
            if lines.Length = 0 then
                Error (sprintf "The file '%s' is empty or contains no data." filePath)
            else
                Ok (processLines lines)
    with
    | ex ->
        Error (sprintf "An unexpected error occurred: %s" ex.Message)
//Environment.GetCommandLineArgs
//let argv = Array.tail (fsi.CommandLineArgs)
let argv = Array.tail (Environment.GetCommandLineArgs())
type MainReturnType = | InputLines of string array | NothingThere of unit
let blines:(MainReturnType) =
    if argv.Length = 1 then
        let filePath = argv.[0]
        match processFile filePath with
        | Ok jagged -> 
            // For demonstration or testing, print the structure
            printfn "Created jagged array with %d rows:" jagged.Length
            jagged
            |> Array.iteri (fun i row ->
                printfn "Row %d: %d columns - %A" i row.Length row
            )
            NothingThere (Environment.Exit(0))
        | Error msg ->
            printfn "Error: %s" msg
            NothingThere(Environment.Exit(1))
    else
        // Read from stdin if no file path provided (supports piping)
        let rec readLines acc =
            let line = Console.ReadLine()
            if line <> null then
                readLines (line :: acc)
            else
                List.rev acc |> List.toArray
        InputLines(readLines [])
let lines = 
    match blines
        with
            | InputLines a->a
            |_ -> Array.Empty()
if lines.Length = 0 then
    printfn "Error: No input data provided via file or stdin."
    Environment.Exit(1)

let jagged = processLines lines

// For demonstration or testing, print the structure
printfn "Created jagged array with %d rows:" jagged.Length
jagged
|> Array.iteri (fun i row ->
    printfn "Row %d: %d columns - %A" i row.Length row
)

// Pure reverse function: takes jagged array and prints to stdout
let printJagged (jagged: string[][]) (delim: char) : unit =
    for row in jagged do
        let line = String.concat (string delim) row
        printfn "%s" line