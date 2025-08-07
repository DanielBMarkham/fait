open System

// Read all lines from console input
let rec readLines acc =
    let line = Console.ReadLine()
    if line <> null then
        readLines (line :: acc)
    else
        List.rev acc |> List.toArray

let lines = readLines []

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