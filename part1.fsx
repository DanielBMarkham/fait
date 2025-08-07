open System
open System.IO

let processFile (filePath: string) : Result<string[][], string> =
    try
        if not (File.Exists filePath) then
            Error (sprintf "The file '%s' does not exist." filePath)
        else
            let lines = File.ReadAllLines filePath
            if lines.Length = 0 then
                Error (sprintf "The file '%s' is empty or contains no data." filePath)
            else
                // Create the jagged array
                let jagged : string[][] =
                    lines
                    |> Array.map (fun line ->
                        if line = "" then
                            [||]
                        else
                            line.Split('\t')
                    )
                Ok jagged
    with
    | ex ->
        Error (sprintf "An unexpected error occurred: %s" ex.Message)

let argv = Array.tail (fsi.CommandLineArgs)

if argv.Length <> 1 then
    printfn "Usage: dotnet fsi script.fsx <file_path>"
    Environment.Exit(1)

let filePath = argv.[0]

match processFile filePath with
| Ok jagged ->
    // For demonstration or testing, print the structure
    printfn "Created jagged array with %d rows:" jagged.Length
    jagged
    |> Array.iteri (fun i row ->
        printfn "Row %d: %d columns - %A" i row.Length row
    )
| Error msg ->
    printfn "Error: %s" msg
    Environment.Exit(1)