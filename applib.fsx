open System

type LogLevel =
    | Info = 1
    | Warn = 2
    | Error = 3

let mutable verbosity = LogLevel.Error
let mutable addDatetime = false

let log (level: LogLevel) (msg: string) =
    let prefix = if addDatetime then DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + " " else ""
    if (int level) >= (int verbosity) then
        Console.Error.WriteLine (prefix + msg)

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
        elif arg = "--dt" then
            addDatetime <- true
        else
            log LogLevel.Error (sprintf "Unknown option: %s" arg)
        i <- i + 1
    (inputFile, outputFile, showHelp)

let reverseAppWords (cluster: string) : string =
    if String.IsNullOrEmpty cluster then ""
    else
        cluster.Split([|' '|], StringSplitOptions.None)
        |> Array.rev
        |> String.concat " "

let processAppRow (fields: string[]) : string[] =
    if verbosity = LogLevel.Info then log LogLevel.Info "Entering processAppRow"
    let result = 
        fields 
        |> Array.rev 
        |> Array.map reverseAppWords
    if verbosity = LogLevel.Info then log LogLevel.Info "Exiting processAppRow"
    result

let processAppData (data: string[][]) : string[][] =
    if verbosity = LogLevel.Info then log LogLevel.Info "Entering processAppData"
    let result = data |> Array.map processAppRow
    if verbosity = LogLevel.Info then log LogLevel.Info "Exiting processAppData"
    result

let reverseAppTestWords (cluster: string) : string =
    if String.IsNullOrEmpty cluster then ""
    else
        cluster.Split([|' '|], StringSplitOptions.None)
        |> Array.rev
        |> String.concat " "

let processAppTestRow (fields: string[]) : string[] =
    if verbosity = LogLevel.Info then log LogLevel.Info "Entering processAppTestRow"
    let result = 
        fields 
        |> Array.rev 
        |> Array.map reverseAppTestWords
    if verbosity = LogLevel.Info then log LogLevel.Info "Exiting processAppTestRow"
    result

let processAppTestData (data: string[][]) : string[][] =
    if verbosity = LogLevel.Info then log LogLevel.Info "Entering processAppTestData"
    let result = data |> Array.map processAppTestRow
    if verbosity = LogLevel.Info then log LogLevel.Info "Exiting processAppTestData"
    result

let appTestArraysEqual (a1: string[]) (a2: string[]) =
    if a1.Length <> a2.Length then false
    else
        Seq.zip a1 a2 |> Seq.forall (fun (x, y) -> x = y)

let appTestJaggedEqual (d1: string[][]) (d2: string[][]) =
    if d1.Length <> d2.Length then false
    else
        Seq.zip d1 d2 |> Seq.forall (fun (r1, r2) -> appTestArraysEqual r1 r2)