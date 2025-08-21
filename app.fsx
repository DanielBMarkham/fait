#load "applib.fsx"

open System
open System.IO
open Microsoft.FSharp.Compiler.Interactive.Settings
open Applib

let helpText = """
Usage: app [options]

Options:
  --i <file>   Input file (default: stdin)
  --o <file>   Output file (default: stdout)
  --v <level>  Verbosity level: INFO, WARN, ERROR (default: ERROR)
  --dt         Enable high precision UTC datetime prefix on log messages
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
        #if INTERACTIVE
        fsi.CommandLineArgs |> Array.tail
        #else
        Environment.GetCommandLineArgs() |> Array.tail
        #endif
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
                let processed = processAppRow fields
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