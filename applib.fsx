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