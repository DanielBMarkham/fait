#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Diagnostics

type Verbosity = Error | Warn | Info

type Options = {
    TestFile: string option
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
        | "-t" :: f :: tail -> parse { acc with TestFile = Some f } tail
        | "-v" :: l :: tail -> parse { acc with Verbosity = parseVerbosity l } tail
        | ("-h" | "--help") :: _ -> { acc with Help = true }
        | _ :: tail -> parse acc tail
    parse { TestFile = None; Help = false; Verbosity = Error } (Array.toList args)

let log (opts: Options) (level: Verbosity) (msg: string) =
    let shouldLog =
        match opts.Verbosity, level with
        | Info, _ -> true
        | Warn, Warn | Warn, Error -> true
        | Error, Error -> true
        | _ -> false
    if shouldLog then Console.Error.WriteLine msg

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

let verifyOutput (opts: Options) (actual: string) (expected: string) : bool =
    log opts Info "Entering verifyOutput"
    let result = actual.Trim() = expected.Trim()
    log opts Info "Exiting verifyOutput"
    result

let verifyErrorContains (opts: Options) (err: string) (substring: string) : bool =
    log opts Info "Entering verifyErrorContains"
    let result = err.Contains(substring)
    log opts Info "Exiting verifyErrorContains"
    result

type TestCase = {
    Name: string
    Input: string option
    ExpectedOut: string
    ExpectedErrSubstring: string option
    Args: string
    InputContent: string option
}

let parseTestLine (opts: Options) (line: string) : TestCase option =
    log opts Info "Entering parseTestLine"
    let result = 
        try
            let parts = line.Split('\t')
            if parts.Length < 3 then None else
            Some {
                Name = parts.[0]
                Input = if parts.[1] = "" then None else Some (parts.[1].Replace("\\n", "\n"))
                ExpectedOut = parts.[2].Replace("\\n", "\n")
                ExpectedErrSubstring = if parts.Length > 3 && parts.[3] <> "" then Some parts.[3] else None
                Args = if parts.Length > 4 then parts.[4] else ""
                InputContent = if parts.Length > 5 && parts.[5] <> "" then Some (parts.[5].Replace("\\n", "\n")) else None
            }
        with _ -> None
    log opts Info "Exiting parseTestLine"
    result

let getHardCodedTests (opts: Options) : TestCase list =
    log opts Info "Entering getHardCodedTests"
    let result = [
        { Name = "Simple stdin/stdout"; Input = Some "a\tb\tc\n1\t2\t3\n"; ExpectedOut = "c\tb\ta\n3\t2\t1\n"; ExpectedErrSubstring = None; Args = ""; InputContent = None }
        { Name = "Empty input"; Input = Some ""; ExpectedOut = ""; ExpectedErrSubstring = None; Args = ""; InputContent = None }
        { Name = "Single column"; Input = Some "x\ny\n"; ExpectedOut = "x\ny\n"; ExpectedErrSubstring = None; Args = ""; InputContent = None }
        { Name = "Uneven columns"; Input = Some "a\tb\nc\nd\te\tf\n"; ExpectedOut = "b\ta\nc\nf\te\td\n"; ExpectedErrSubstring = None; Args = ""; InputContent = None }
        { Name = "Bad input file"; Input = None; ExpectedOut = ""; ExpectedErrSubstring = Some "Error opening input file"; Args = "-i nonexistent.txt"; InputContent = None }
        { Name = "Output file"; Input = Some "a\tb\tc\n"; ExpectedOut = "c\tb\ta\n"; ExpectedErrSubstring = None; Args = "-o TEMP_OUT"; InputContent = None }
        { Name = "Input output files"; Input = None; ExpectedOut = "c\tb\ta\n3\t2\t1\n"; ExpectedErrSubstring = None; Args = "-i TEMP_IN -o TEMP_OUT"; InputContent = Some "a\tb\tc\n1\t2\t3\n" }
    ]
    log opts Info "Exiting getHardCodedTests"
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
    Console.WriteLine "part2-test.fsx: A test script for part2.fsx, which processes tab-delimited text."
    Console.WriteLine ""
    Console.WriteLine "Usage: dotnet fsi part2-test.fsx [options]"
    Console.WriteLine ""
    Console.WriteLine "Options:"
    Console.WriteLine "  -t <file>    Test file in tab-delimited format (default: run hard-coded tests)"
    Console.WriteLine "               Format per line: name\tinput\texpected\t[err_substring]\t[args]\t[input_content]"
    Console.WriteLine "               input, expected, and input_content support \\n for newlines."
    Console.WriteLine "  -v <level>   Verbosity level: ERROR, WARN, INFO (default: ERROR)"
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
            log opts Info "Entering main"

            let scriptPath = "part2.fsx"  // Assumes part2.fsx is in the same directory

            let runProcessor (args: string) (inputText: string option) : string * string * int =
                log opts Info "Entering runProcessor"
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
                log opts Info "Exiting runProcessor"
                out, err, p.ExitCode

            let tryParseTests (lines: seq<string>) : TestCase list option =
                log opts Info "Entering tryParseTests"
                let parsed =
                    lines
                    |> Seq.filter (fun line -> line.Trim() <> "" && not (line.StartsWith("#")))
                    |> Seq.choose (parseTestLine opts)
                    |> Seq.toList
                let result = if parsed.IsEmpty then None else Some parsed
                log opts Info "Exiting tryParseTests"
                result

            let tests =
                match opts.TestFile with
                | Some f ->
                    try
                        let lines = File.ReadLines f
                        match tryParseTests lines with
                        | Some ts -> ts
                        | None -> 
                            log opts Warn "Warning: Invalid test format in file; running hard-coded tests."
                            getHardCodedTests opts
                    with e ->
                        log opts Error $"Error reading test file '{f}': {e.Message}"
                        getHardCodedTests opts
                | None ->
                    if Console.IsInputRedirected then
                        let lines = readLines opts Console.In
                        match tryParseTests lines with
                        | Some ts -> ts
                        | None -> 
                            log opts Warn "Warning: Invalid test format in piped input; running hard-coded tests."
                            getHardCodedTests opts
                    else
                        getHardCodedTests opts

            for test in tests do
                try
                    let tempInOpt =
                        if test.Args.Contains("TEMP_IN") && test.InputContent.IsSome then
                            let temp = Path.GetTempFileName()
                            File.WriteAllText(temp, test.InputContent.Value)
                            Some temp
                        else None

                    let tempOutOpt =
                        if test.Args.Contains("TEMP_OUT") then
                            Some (Path.GetTempFileName())
                        else None

                    let args =
                        test.Args
                        |> fun a -> match tempInOpt with | Some t -> a.Replace("TEMP_IN", t) | None -> a
                        |> fun a -> match tempOutOpt with | Some t -> a.Replace("TEMP_OUT", t) | None -> a

                    let out, err, code = runProcessor args test.Input

                    let actualOut =
                        match tempOutOpt with
                        | Some f ->
                            let content = File.ReadAllText f
                            try File.Delete f with _ -> ()
                            content
                        | None -> out

                    let outPass = verifyOutput opts actualOut test.ExpectedOut
                    let errPass = match test.ExpectedErrSubstring with
                                  | None -> err.Trim() = ""
                                  | Some s -> verifyErrorContains opts err s
                    let overall = if outPass && errPass then "PASS" else "FAIL"
                    Console.WriteLine $"{test.Name}\t{overall}\tExit code: {code}\tOut match: {outPass}\tErr match: {errPass}\tError: '{err.Trim()}'"

                    match tempInOpt with
                    | Some f -> try File.Delete f with _ -> ()
                    | None -> ()
                with e ->
                    log opts Error $"Error running test '{test.Name}': {e.Message}"

            log opts Info "Exiting main"
    with e ->
        let opts = parseOptions (getScriptArgs ())  // Fallback opts for logging
        log opts Error $"Unexpected error: {e.Message}"

main ()