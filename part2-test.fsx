#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Diagnostics

let verifyOutput (actual: string) (expected: string) : bool =
    actual.Trim() = expected.Trim()

let verifyErrorContains (err: string) (substring: string) : bool =
    err.Contains(substring)

type TestCase = {
    Name: string
    Input: string option
    ExpectedOut: string
    ExpectedErrSubstring: string option
    Args: string
}

let parseTestLine (line: string) : TestCase =
    let parts = line.Split('\t')
    if parts.Length < 3 then failwith $"Invalid test line: {line}"
    {
        Name = parts.[0]
        Input = if parts.[1] = "" then None else Some (parts.[1].Replace("\\n", "\n"))
        ExpectedOut = parts.[2].Replace("\\n", "\n")
        ExpectedErrSubstring = if parts.Length > 3 && parts.[3] <> "" then Some parts.[3] else None
        Args = if parts.Length > 4 then parts.[4] else ""
    }

let getHardCodedTests () : TestCase list =
    [
        { Name = "Simple stdin/stdout"; Input = Some "a\tb\tc\n1\t2\t3\n"; ExpectedOut = "c\tb\ta\n3\t2\t1"; ExpectedErrSubstring = None; Args = "" }
        { Name = "Empty input"; Input = Some ""; ExpectedOut = ""; ExpectedErrSubstring = None; Args = "" }
        { Name = "Single column"; Input = Some "x\ny\n"; ExpectedOut = "x\ny"; ExpectedErrSubstring = None; Args = "" }
        { Name = "Uneven columns"; Input = Some "a\tb\nc\nd\te\tf\n"; ExpectedOut = "b\ta\nc\nf\te\td"; ExpectedErrSubstring = None; Args = "" }
        { Name = "Bad input file"; Input = None; ExpectedOut = ""; ExpectedErrSubstring = Some "Error opening input file"; Args = "-i nonexistent.txt" }
        { Name = "Output file"; Input = Some "a\tb\tc\n"; ExpectedOut = "c\tb\ta"; ExpectedErrSubstring = None; Args = $"-o {Path.GetTempFileName()}" }  // Note: For file tests, would need to read output file, but simplified here
    ]

type Options = {
    TestFile: string option
    Help: bool
}

let parseOptions (args: string[]) : Options =
    let rec parse acc remaining =
        match remaining with
        | [] -> acc
        | "-t" :: f :: tail -> parse { acc with TestFile = Some f } tail
        | ("-h" | "--help") :: _ -> { acc with Help = true }
        | _ :: tail -> parse acc tail
    parse { TestFile = None; Help = false } (Array.toList args)

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
    Console.WriteLine "part2-test.fsx: A test script for part2.fsx, which processes tab-delimited text."
    Console.WriteLine ""
    Console.WriteLine "Usage: dotnet fsi part2-test.fsx [options]"
    Console.WriteLine ""
    Console.WriteLine "Options:"
    Console.WriteLine "  -t <file>    Test file in tab-delimited format (default: run hard-coded tests)"
    Console.WriteLine "               Format per line: name\tinput\t.expected\t[err_substring]\t[args]"
    Console.WriteLine "               input and expected support \\n for newlines."
    Console.WriteLine "  -h, --help   Show this help message"
    Console.WriteLine ""
    Console.WriteLine "Examples:"
    Console.WriteLine "  dotnet fsi part2-test.fsx                    # Run hard-coded tests"
    Console.WriteLine "  dotnet fsi part2-test.fsx -t tests.txt       # Run tests from file"

let main () =
    try
        let opts = parseOptions (getScriptArgs ())

        if opts.Help then
            showHelp ()
        else
            let scriptPath = "part2.fsx"  // Assumes part2.fsx is in the same directory

            let runProcessor (args: string) (inputText: string option) : string * string * int =
                let psi = ProcessStartInfo("dotnet", $"fsi {scriptPath} {args}")
                psi.RedirectStandardInput <- inputText.IsSome
                psi.RedirectStandardOutput <- true
                psi.RedirectStandardError <- true
                psi.UseShellExecute <- false
                use p = new Process(StartInfo = psi)
                p.Start() |> ignore
                if inputText.IsSome then
                    p.StandardInput.Write(inputText.Value)
                    p.StandardInput.Close()
                p.WaitForExit()
                let out = p.StandardOutput.ReadToEnd()
                let err = p.StandardError.ReadToEnd()
                out, err, p.ExitCode

            let tests =
                match opts.TestFile with
                | None -> getHardCodedTests ()
                | Some f ->
                    try
                        File.ReadAllLines f
                        |> Array.filter (fun line -> line.Trim() <> "" && not (line.StartsWith("#")))
                        |> Array.map parseTestLine
                        |> Array.toList
                    with e ->
                        Console.Error.WriteLine $"Error reading test file '{f}': {e.Message}"
                        getHardCodedTests ()

            for test in tests do
                try
                    let out, err, code = runProcessor test.Args test.Input
                    let outPass = verifyOutput out test.ExpectedOut
                    let errPass = match test.ExpectedErrSubstring with
                                  | None -> err.Trim() = ""
                                  | Some s -> verifyErrorContains err s
                    let overall = if outPass && errPass then "PASS" else "FAIL"
                    Console.WriteLine $"Test '{test.Name}': {overall} (Exit code: {code}, Out match: {outPass}, Err match: {errPass}, Error: '{err.Trim()}')"
                    // For output file tests, might need to read file if args has -o, but simplified; assume stdout for most
                with e ->
                    Console.Error.WriteLine $"Error running test '{test.Name}': {e.Message}"
    with e ->
        Console.Error.WriteLine $"Unexpected error: {e.Message}"

main ()