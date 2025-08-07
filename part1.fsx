open System
open System.IO

let argv = Array.tail (fsi.CommandLineArgs)

if argv.Length <> 1 then
    printfn "Usage: dotnet fsi script.fsx <file_path>"
    Environment.Exit(1)

let filePath = argv.[0]
try
    if not (File.Exists filePath) then
        printfn "Error: The file '%s' does not exist." filePath
        Environment.Exit(1)

    let lines = File.ReadAllLines filePath
    if lines.Length = 0 then
        printfn "Error: The file '%s' is empty or contains no data." filePath
        Environment.Exit(1)

    // Create the jagged array
    let jagged : string[][] =
        lines
        |> Array.map (fun line ->
            if line = "" then
                [||]
            else
                line.Split('\t')
        )

    // For demonstration or testing, print the structure
    printfn "Created jagged array with %d rows:" jagged.Length
    jagged
    |> Array.iteri (fun i row ->
        printfn "Row %d: %d columns - %A" i row.Length row
    )
with
| ex ->
    printfn "Error: An unexpected error occurred - %s" ex.Message
    Environment.Exit(1)