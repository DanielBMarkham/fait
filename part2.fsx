#!/usr/bin/env -S dotnet fsi

open System
open System.IO

type Verbosity = Error | Warn | Info

type Options = {
    Input: string option
    Output: string option
    Help: bool
    Verbosity: Verbosity
}

let parseVerbosity (s: string) =
    match s.ToLowerInvariant() with
    | "error" -> Error
    | "warn" -> Warn
    | "info" -> Info
    | _ -> Error  // Default on invalid

let parseOptions (args: string[]) : Options =
    let rec parse acc remaining =
        match remaining with
        | [] -> acc
        | "-i" :: f :: tail -> parse { acc with Input = Some f } tail
        | "-o" :: f :: tail -> parse { acc with Output = Some f } tail
        | "-v" :: l :: tail -> parse { acc with Verbosity = parseVerbosity l } tail
        | ("-h" | "--help") :: _ -> { acc with Help = true }
        | _ :: tail -> parse acc tail
    parse { Input = None; Output = None; Help = false; Verbosity = Error } (Array.toList args)

let log (opts: Options) (level: Verbosity) (msg: string) =
    let shouldLog =
        match opts.Verbosity, level with
        | Info, _ -> true
        | Warn, Warn | Warn, Error -> true
        | Error, Error -> true
        | _ -> false
    if shouldLog then Console.Error.WriteLine msg

let processLine (opts: Options) (line: string) : string =
    log opts Info "Entering processLine"
    let result = line.Split('\t') |> Array.rev |> String.concat "\t"
    log opts Info "Exiting processLine"
    result

let processLines (opts: Options) (lines: seq<string>) : seq<string> =
    log opts Info "Entering processLines"
    let result = Seq.map (processLine opts) lines
    log opts Info "Exiting processLines"
    result

let readLines (opts: Options) (reader: TextReader) : seq<string> =
    log opts Info "Entering readLines"
    let result = seq {
        let mutable line = reader.ReadLine()
        while not (Object.ReferenceEquals(line, null)) do
            yield line
            line <- reader.ReadLine()
    }
    log opts Info "Exiting readLines"
    result

let getScriptArgs () =
    let allArgs = Environment.GetCommandLineArgs()
    if allArgs.Length > 1 then
        if allArgs.[1] = "fsi" then
            allArgs |> Array.skip 3
        elif allArgs.[0].EndsWith("fsi.exe", StringComparison.OrdinalIgnoreCase) then
            allArgs |> Array.skip 2
        else
            allArgs |> Array.skip 1
    else
        [||]

let showHelp () =
    Console.WriteLine "part2.fsx: A script to process tab-delimited text by reversing the order of columns in each line."
    Console.WriteLine ""
    Console.WriteLine "Usage: dotnet fsi part2.fsx [options]"
    Console.WriteLine ""
    Console.WriteLine "Options:"
    Console.WriteLine "  -i <file>    Input file (default: stdin)"
    Console.WriteLine "  -o <file>    Output file (default: stdout)"
    Console.WriteLine "  -v <level>   Verbosity level: ERROR, WARN, INFO (default: ERROR)"
    Console.WriteLine "  -h, --help   Show this help message"
    Console.WriteLine ""
    Console.WriteLine "Examples:"
    Console.WriteLine "  echo \"a\tb\tc\" | dotnet fsi part2.fsx                # Outputs: c\tb\ta"
    Console.WriteLine "  dotnet fsi part2.fsx -i input.txt -o output.txt      # Processes input.txt to output.txt"
    Console.WriteLine "  type input.txt | dotnet fsi part2.fsx > output.txt   # Windows pipe example"
    Console.WriteLine ""
    Console.WriteLine "Note: To avoid option interception by dotnet or fsi, use -- before script options if needed, e.g., dotnet fsi part2.fsx -- -h"

let main () =
    try
        let opts = parseOptions (getScriptArgs ())

        if opts.Help then
            showHelp ()

        log opts Info "Entering main"

        let input, inputDispose =
            match opts.Input with
            | None -> Console.In, (fun () -> ())
            | Some f ->
                try
                    let r = File.OpenText f
                    r, r.Dispose
                with e ->
                    log opts Error $"Error opening input file '{f}': {e.Message}"
                    Console.In, (fun () -> ())

        use _inputDisposer = { new IDisposable with member _.Dispose() = inputDispose () }

        let output, outputDispose =
            match opts.Output with
            | None -> Console.Out, (fun () -> ())
            | Some f ->
                try
                    let w = File.CreateText f
                    w, w.Dispose
                with e ->
                    log opts Error $"Error opening output file '{f}': {e.Message}"
                    Console.Out, (fun () -> ())

        use _outputDisposer = { new IDisposable with member _.Dispose() = outputDispose () }

        let lines = readLines opts input
        let processed = processLines opts lines
        for line in processed do
            output.WriteLine line
        output.Flush()

        log opts Info "Exiting main"
    with e ->
        let opts = parseOptions (getScriptArgs ())  // Fallback opts for logging
        log opts Error $"Unexpected error: {e.Message}"

main ()