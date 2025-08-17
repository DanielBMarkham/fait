#!/usr/bin/env -S dotnet fsi

open System
open System.IO

let processLine (line: string) : string =
    line.Split('\t')
    |> Array.rev
    |> String.concat "\t"

let processLines (lines: seq<string>) : seq<string> =
    Seq.map processLine lines

type Options = {
    Input: string option
    Output: string option
}

let parseOptions (args: string[]) : Options =
    let rec parse acc remaining =
        match remaining with
        | [] -> acc
        | "-i" :: f :: tail -> parse { acc with Input = Some f } tail
        | "-o" :: f :: tail -> parse { acc with Output = Some f } tail
        | _ :: tail -> parse acc tail
    parse { Input = None; Output = None } (Array.toList args)

let readLines (reader: TextReader) : seq<string> =
    seq {
        let mutable line = reader.ReadLine()
        while not (Object.ReferenceEquals(line, null)) do
            yield line
            line <- reader.ReadLine()
    }

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

let main () =
    try
        let opts = parseOptions (getScriptArgs ())

        let input, inputDispose =
            match opts.Input with
            | None -> Console.In, (fun () -> ())
            | Some f ->
                try
                    let r = File.OpenText f
                    r, r.Dispose
                with e ->
                    Console.Error.WriteLine $"Error opening input file '{f}': {e.Message}"
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
                    Console.Error.WriteLine $"Error opening output file '{f}': {e.Message}"
                    Console.Out, (fun () -> ())

        use _outputDisposer = { new IDisposable with member _.Dispose() = outputDispose () }

        let lines = readLines input
        let processed = processLines lines
        for line in processed do
            output.WriteLine line
    with e ->
        Console.Error.WriteLine $"Unexpected error: {e.Message}"

main ()

