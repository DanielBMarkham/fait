module Applib

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