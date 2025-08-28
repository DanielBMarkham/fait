// app2.fsx
// Main script for app2. Loads the shared library and handles CLI I/O, options, logging, and tests.
// Designed to be as functional as possible while handling imperative I/O.

#load "app2lib.fsx"

open System
open System.IO
open App2Lib

let appName = "app2"

/// Log levels for verbosity control.
type LogLevel = 
    | Info 
    | Warn 
    | Error

    static member ToInt = function Info -> 0 | Warn -> 1 | Error -> 2
    static member ToString = function Info -> "INFO" | Warn -> "WARN" | Error -> "ERROR"

/// Logs a message if the message level meets or exceeds the user-specified verbosity level.
let logMsg (userLevel: LogLevel) (useDt: bool) (level: LogLevel) (msg: string) : unit =
    if LogLevel.ToInt level >= LogLevel.ToInt userLevel then
        let prefix = if useDt then DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + " " else ""
        let levelStr = LogLevel.ToString level
        Console.Error.WriteLine($"{prefix}{levelStr}: {msg}")

/// Parses command-line arguments into options.
/// Logs warnings to stderr for invalid options (using Console.Error since verbosity may not be parsed yet).
let parseArgs (args: string[]) : bool * LogLevel * string option * string option * string * bool * bool =
    let mutable i = 0
    let mutable help = false
    let mutable verbosity = Warn  // Default
    let mutable inputFile = None
    let mutable outputFile = None
    let mutable delim = @"\t"  // Default tab regex
    let mutable runTests = false
    let mutable useDatetime = false

    while i < args.Length do
        let arg = args.[i]
        match arg with
        | "--h" | "--help" -> 
            help <- true
        | "--v" | "--verbosity" -> 
            i <- i + 1
            if i < args.Length then
                match args.[i].ToUpperInvariant() with
                | "INFO" -> verbosity <- Info
                | "WARN" -> verbosity <- Warn
                | "ERROR" -> verbosity <- Error
                | _ -> Console.Error.WriteLine($"Unknown verbosity level: {args.[i]}")
        | "--i" | "--input" -> 
            i <- i + 1
            if i < args.Length then inputFile <- Some args.[i]
            else Console.Error.WriteLine("Warning: --input requires a filename argument")
        | "--o" | "--output" -> 
            i <- i + 1
            if i < args.Length then outputFile <- Some args.[i]
            else Console.Error.WriteLine("Warning: --output requires a filename argument")
        | "--d" | "--delim" -> 
            i <- i + 1
            if i < args.Length then delim <- args.[i]
            else Console.Error.WriteLine("Warning: --delim requires a regex pattern argument")
        | "--t" | "--tests" -> 
            runTests <- true
        | "--dt" | "--datetime" -> 
            useDatetime <- true
        | _ -> 
            Console.Error.WriteLine($"Unknown argument: {arg}")

        i <- i + 1

    help, verbosity, inputFile, outputFile, delim, runTests, useDatetime

/// Prints help message, including usage scenarios for BASH and DOS.
let printHelp (appName: string) : unit =
    Console.WriteLine($"{appName} - A console application for processing tab-delimited lines (initially reverses column order).")
    Console.WriteLine("Usage: appName [options]")
    Console.WriteLine("Options:")
    Console.WriteLine("  --h, --help         Show this help message.")
    Console.WriteLine("  --v, --verbosity <LEVEL>  Set verbosity: INFO (all logs), WARN (default, warnings and errors), ERROR (only errors).")
    Console.WriteLine("  --i, --input <FILE>  Read input from FILE instead of stdin.")
    Console.WriteLine("  --o, --output <FILE> Write output to FILE instead of stdout.")
    Console.WriteLine("  --d, --delim <REGEX> Use REGEX as column delimiter (default: \\t).")
    Console.WriteLine("  --t, --tests        Run smoke tests instead of normal operation.")
    Console.WriteLine("  --dt, --datetime    Prefix log messages with high-precision datetime.")
    Console.WriteLine("")
    Console.WriteLine("Usage scenarios:")
    Console.WriteLine("Piping input/output (streams lines, reverses columns):")
    Console.WriteLine("  BASH: cat appsample.txt | app2 | app2 > reversed_twice.txt  (should match original)")
    Console.WriteLine("  DOS:  type appsample.txt | app2 | app2 > reversed_twice.txt  (should match original)")
    Console.WriteLine("File input/output:")
    Console.WriteLine("  BASH/DOS: app2 --input appsample.txt --output out.txt --d ','")
    Console.WriteLine("Interactive input (type lines, end with Ctrl+D (BASH) or Ctrl+Z (DOS)):")
    Console.WriteLine("  BASH/DOS: app2 --d ' '")
    Console.WriteLine("Multiple pipes (e.g., app2 | app2-test | app2-test | app2 works indefinitely, reversing columns each time).")

/// Runs internal smoke tests using the pure processing function from the library.
/// Tests various data cases: empty lines, varying column counts, empty fields.
let runInternalTests (processLine: string -> string -> string) (delim: string) (userLevel: LogLevel) (useDt: bool) : unit =
    logMsg userLevel useDt Info "Running internal smoke tests using pure functions."
    let emptyLineTest = ("Empty line", "", "")
    let oneColTest = ("One column", "field1", "field1")
    let twoColTest = ("Two columns", "field1\tfield2", "field2\tfield1")
    let skipColTest = ("Skipping columns (empty field)", "field1\t\tfield3", "field3\t\tfield1")
    let sixFields = [|"1"; "2"; "3"; "4"; "5"; "6"|]
    let sixInput = String.Join("\t", sixFields)
    let sixExpected = String.Join("\t", Array.rev sixFields)
    let sixTest = ("Six columns", sixInput, sixExpected)
    let fifteenFields = Array.init 15 (fun i -> (i+1).ToString())
    let fifteenInput = String.Join("\t", fifteenFields)
    let fifteenExpected = String.Join("\t", Array.rev fifteenFields)
    let fifteenTest = ("Fifteen columns", fifteenInput, fifteenExpected)
    let testCases : (string * string * string) list = [
        emptyLineTest
        oneColTest
        twoColTest
        skipColTest
        sixTest
        fifteenTest
    ]
    for (name, inputLine, expected) in testCases do
        let actual = processLine inputLine delim
        let pass = actual = expected
        let result = sprintf "%s: %s - Expected: '%s', Actual: '%s'" name (if pass then "PASS" else "FAIL") expected actual
        Console.WriteLine result
        if not pass then
            logMsg userLevel useDt Warn $"Internal test failed: {name}"

/// Prints descriptions of external OS smoke tests for manual execution.
let printExternalTests (appName: string) (userLevel: LogLevel) (useDt: bool) : unit =
    logMsg userLevel useDt Info "External OS smoke tests (run manually to verify behavior)."
    Console.WriteLine("1. Empty piped input (no output expected):")
    Console.WriteLine("   BASH: echo | app2 > out.txt ; if [ -s out.txt ]; then echo 'FAIL: produced output on empty input'; else echo 'PASS'; fi")
    Console.WriteLine("   DOS:  echo. | app2 > out.txt & if exist out.txt (for /f %%i in ('find /c /v \"\" out.txt') do if %%i gtr 0 (echo FAIL) else (echo PASS)) else (echo PASS)")
    Console.WriteLine("2. Interactive input (type 'a\\tb', end session, expect 'b\\ta'):")
    Console.WriteLine("   BASH/DOS: app2  (type line, Ctrl+D (BASH) or Ctrl+Z (DOS) to end)")
    Console.WriteLine("3. File input with empty file (no output expected):")
    Console.WriteLine("   BASH/DOS: touch empty.txt ; app2 --input empty.txt > out.txt ; (check out.txt empty)")
    Console.WriteLine("4. Varying columns in file (use appsample.txt, verify reversed columns):")
    Console.WriteLine("   BASH/DOS: app2 --input appsample.txt --output out.txt  (manually verify reversal)")
    Console.WriteLine("5. Piping multiple times (should reverse back to original after even count):")
    Console.WriteLine("   BASH: cat appsample.txt | app2 | app2 > out.txt ; diff appsample.txt out.txt  (should match)")
    Console.WriteLine("   DOS:  type appsample.txt | app2 | app2 > out.txt  (diff appsample.txt out.txt should match)")
    Console.WriteLine("6. Custom delimiter:")
    Console.WriteLine("   BASH/DOS: echo 'a,b' | app2 --d ','  (expect 'b,a')")
    Console.WriteLine("7. Help and options:")
    Console.WriteLine("   BASH/DOS: app2 --h  (should print help without processing)")

/// Main processing logic: reads input (file or stdin), processes line-by-line, writes output (file or stdout).
/// Streams data without loading entire input at once.
let processInput (inputFile: string option) (outputFile: string option) (delim: string) (processLine: string -> string -> string) (userLevel: LogLevel) (useDt: bool) : unit =
    if userLevel = Info then logMsg userLevel useDt Info "Starting line-by-line processing."
    let writer =
        match outputFile with
        | Some file ->
            new StreamWriter(file)
        | None ->
            Console.Out :> TextWriter
    use outputWriter = writer
    if inputFile.IsSome then
        try
            use reader = File.OpenText(inputFile.Value)
            let mutable line = reader.ReadLine()
            while not isNull line do
                let processedLine = processLine line delim
                outputWriter.WriteLine(processedLine)
                line <- reader.ReadLine()
        with
        | :? FileNotFoundException as ex ->
            logMsg userLevel useDt Error $"Input file not found: {inputFile.Value}. Error: {ex.Message}"
        | :? IOException as ex ->
            logMsg userLevel useDt Error $"I/O error: {ex.Message}"
        | ex ->
            logMsg userLevel useDt Error $"Unexpected error during processing: {ex.Message}"
    else
        try
            let mutable line = Console.ReadLine()
            while not isNull line do
                let processedLine = processLine line delim
                outputWriter.WriteLine(processedLine)
                line <- Console.ReadLine()
        with
        | ex ->
            logMsg userLevel useDt Error $"Unexpected error during processing: {ex.Message}"
    if userLevel = Info then logMsg userLevel useDt Info "Finished line-by-line processing."

/// Entry point.
[<EntryPoint>]
let main argv =
    try
        let args = argv
        let help, userLevel, inputFile, outputFile, delim, runTests, useDt = parseArgs args
        if help then
            printHelp appName
            0
        elif runTests then
            runInternalTests processAppDataLine delim userLevel useDt
            printExternalTests appName userLevel useDt
            0
        else
            processInput inputFile outputFile delim processAppDataLine userLevel useDt
            0
    with
    | ex ->
        // Catch any top-level exception, log, and exit safely (no throw to OS).
        Console.Error.WriteLine($"Top-level error: {ex.Message}")
        1