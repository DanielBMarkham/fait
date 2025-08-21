#load "applib.fsx"

open System
open System.IO
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

let helpText = """
Usage: app-test [options]

Options:
  --i <file>   Input file (default: stdin)
  --o <file>   Output file (default: stdout)
  --v <level>  Verbosity level: INFO, WARN, ERROR (default: ERROR)
  --dt         Enable high precision UTC datetime prefix on log messages
  --h          Show this help

This program reads tab-delimited text, processes it by reversing the order of clusters in each line and reversing the words in each cluster, and outputs the result.
It operates in streaming mode, processing line by line.
It also runs internal smoke tests and logs results at WARN level.

Examples:
In DOS:
  app-test.cmd --i sample.txt --o output.txt
  type sample.txt | app-test.cmd > output.txt

In Bash:
  ./app-test --i sample.txt --o output.txt
  cat sample.txt | ./app-test > output.txt

If no input is provided, it reads from stdin, which may appear as hanging if waiting for keyboard input.
"""

let main () =
    let args = 
        #if INTERACTIVE
        fsi.CommandLineArgs |> Array.tail
        #else
        Environment.GetCommandLineArgs() |> Array.tail
        #endif
    let inputFile, outputFile, showHelp = parseArgs args
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
                let fields = line.Split '\t'
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