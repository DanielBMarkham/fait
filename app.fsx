open System
open System.IO

type LogLevel =
    | Info = 1
    | Warn = 2
    | Error = 3

let mutable verbosity = LogLevel.Error

let log (level: LogLevel) (msg: string) =
    if (int level) >= (int verbosity) then
        Console.Error.WriteLine msg

let reverseWords (cluster: string) : string =
    if String.IsNullOrEmpty cluster then ""
    else
        cluster.Split([|' '|], StringSplitOptions.None)
        |> Array.rev
        |> String.concat " "

let processRow (fields: string[]) : string[] =
    if verbosity = LogLevel.Info then log LogLevel.Info "Entering processRow"
    let result = 
        fields 
        |> Array.rev 
        |> Array.map reverseWords
    if verbosity = LogLevel.Info then log LogLevel.Info "Exiting processRow"
    result

let processData (data: string[][]) : string[][] =
    if verbosity = LogLevel.Info then log LogLevel.Info "Entering processData"
    let result = data |> Array.map processRow
    if verbosity = LogLevel.Info then log LogLevel.Info "Exiting processData"
    result

let parseArgs (args: string[]) =
    let mutable inputFile = None
    let mutable outputFile = None
    let mutable showHelp = false
    let mutable i = 0
    while i < args.Length do
        let arg = args.[i]
        if arg = "--i" then
            i <- i + 1
            if i < args.Length then inputFile <- Some args.[i]
        elif arg = "--o" then
            i <- i + 1
            if i < args.Length then outputFile <- Some args.[i]
        elif arg = "--v" then
            i <- i + 1
            if i < args.Length then
                match args.[i].ToUpper() with
                | "INFO" -> verbosity <- LogLevel.Info
                | "WARN" -> verbosity <- LogLevel.Warn
                | "ERROR" -> verbosity <- LogLevel.Error
                | _ -> log LogLevel.Error (sprintf "Invalid verbosity level: %s" args.[i])
        elif arg = "--h" then
            showHelp <- true
        else
            log LogLevel.Error (sprintf "Unknown option: %s" arg)
        i <- i + 1
    (inputFile, outputFile, showHelp)

let helpText = """
Usage: app [options]

Options:
  --i <file>   Input file (default: stdin)
  --o <file>   Output file (default: stdout)
  --v <level>  Verbosity level: INFO, WARN, ERROR (default: ERROR)
  --h          Show this help

This program reads tab-delimited text, processes it by reversing the order of clusters in each line and reversing the words in each cluster, and outputs the result.
It operates in streaming mode, processing line by line.

Examples:
In DOS:
  app.cmd --i sample.txt --o output.txt
  type sample.txt | app.cmd > output.txt

In Bash:
  ./app --i sample.txt --o output.txt
  cat sample.txt | ./app > output.txt

If no input is provided, it reads from stdin, which may appear as hanging if waiting for keyboard input.
"""

let main () =
    let args = 
        try
            fsi.CommandLineArgs |> Array.tail
        with
        | _ -> [||]
    let inputFile, outputFile, showHelp = parseArgs args
    if showHelp then
        Console.Out.WriteLine helpText
    let reader : TextReader =
        match inputFile with
        | Some file ->
            try
                new StreamReader(file)
            with
            | ex ->
                log LogLevel.Error (sprintf "Error opening input file %s: %s. Falling back to stdin." file ex.Message)
                Console.In
        | None ->
            Console.In
    let writer : TextWriter =
        match outputFile with
        | Some file ->
            try
                new StreamWriter(file)
            with
            | ex ->
                log LogLevel.Error (sprintf "Error opening output file %s: %s. Falling back to stdout." file ex.Message)
                Console.Out
        | None ->
            Console.Out
    try
        try
            let mutable line = reader.ReadLine()
            while line <> null do
                let fields = line.Split '\t'
                let processed = processRow fields
                let outLine = String.Join("\t", processed)
                writer.WriteLine outLine
                line <- reader.ReadLine()
        with
        | ex ->
            log LogLevel.Error (sprintf "Error during processing: %s" ex.Message)
    finally
        if reader <> Console.In then reader.Close()
        if writer <> Console.Out then writer.Close()

main ()