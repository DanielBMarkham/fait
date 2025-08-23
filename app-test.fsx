#load "applib.fsx"

open System
open System.IO
open System.Text.RegularExpressions
open Microsoft.FSharp.Compiler.Interactive.Settings
open Applib

let runSmokeTests () =
    let test (name: string) (input: string[][]) (expected: string[][]) =
        let output = processAppTestData input
        let pass = appTestJaggedEqual output expected
        log LogLevel.Warn (sprintf "Smoke test %s: %s" name (if pass then "PASS" else "FAIL"))
    test "1 - Empty" [||] [||]
    test "2 - Single empty row" [| [||] |] [| [||] |]
    test "3 - Row with empty field" [| [|""|] |] [| [|""|] |]
    test "4 - Row with words" [| [|"hello world"|] |] [| [|"world hello"|] |]
    test "5 - Multiple fields" [| [|"a b"; "c d e"|] |] [| [|"e d c"; "b a"|] |]
    test "6 - With hole (intentional fail)" [| [|"x"; ""; "y"|] |] [| [|"y"; ""; "z"|] |]  // Expected 'z' instead of 'x' to force fail

let parseArgs (args: string[]) =
    let mutable inputFile = None
    let mutable outputFile = None
    let mutable showHelp = false
    let mutable delim = @"\t"
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
        elif arg = "--dt" then
            addDatetime <- true
        elif arg = "--delim" then
            i <- i + 1
            if i < args.Length then delim <- args.[i]
        else
            log LogLevel.Error (sprintf "Unknown option: %s" arg)
        i <- i + 1
    (inputFile, outputFile, showHelp, delim)

let helpText = """
Usage: app-test [options]

Options:
  --i <file>   Input file (default: stdin)
  --o <file>   Output file (default: stdout)
  --v <level>  Verbosity level: INFO, WARN, ERROR (default: ERROR)
  --dt         Enable high precision UTC datetime prefix on log messages
  --delim <regex> Input delimiter as regex pattern (default: \t)
  --h          Show this help

This program reads delimited text, processes it by reversing the order of clusters in each line and reversing the words in each cluster, and outputs the result with tab as delimiter.
It operates in streaming mode, processing line by line.
It also runs internal smoke tests and logs results at WARN level.

Examples:
In DOS:
  app-test.cmd --i appsample.txt --o output.txt
  type appsample.txt | app-test.cmd > output.txt
  app-test.cmd --delim "," --i input.csv --o output.txt

In Bash:
  ./app-test --i appsample.txt --o output.txt
  cat appsample.txt | ./app-test > output.txt
  ./app-test --delim "," --i input.csv --o output.txt

If no input is provided, it reads from stdin, which may appear as hanging if waiting for keyboard input.
"""

let main () =
    let args = 
        #if INTERACTIVE
        fsi.CommandLineArgs |> Array.tail
        #else
        Environment.GetCommandLineArgs() |> Array.tail
        #endif
    let inputFile, outputFile, showHelp, delim = parseArgs args
    if showHelp then
        Console.Out.WriteLine helpText
    runSmokeTests ()
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
                let fields = Regex.Split(line, delim)
                let processed = processAppTestRow fields
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