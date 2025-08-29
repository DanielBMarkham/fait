// app2lib.fsx
// Purely functional library for app2 and app2-test
// No side effects, no I/O, no logging

module App2Lib

open System
open System.Text.RegularExpressions

/// Reverses the columns in a single line based on the delimiter regex pattern.
/// Splits the line using the regex, reverses the columns while preserving the original delimiters.
let private reverseColumnsLine (line: string) (delimPattern: string) : string =
    if String.IsNullOrEmpty line then
        ""
    else
        let regex = Regex($"^(.*?)((?:{delimPattern}(.*))*)$", RegexOptions.Singleline)
        let m = regex.Match(line)
        if not m.Success then
            line  // If no match, return original
        else
            let columns = m.Groups.[1].Value :: (if m.Groups.[3].Captures.Count > 0 then [ for c in m.Groups.[3].Captures -> c.Value ] else [])
            let delimsMatch = Regex(delimPattern)
            let delims = delimsMatch.Matches(m.Groups.[2].Value) |> Seq.cast<Match> |> Seq.map (fun dm -> dm.Value) |> List.ofSeq
            let revColumns = List.rev columns
            let revDelims = if delims.Length > 0 then List.rev delims else []
            let sb = System.Text.StringBuilder()
            for i in 0..revColumns.Length - 1 do
                sb.Append(revColumns.[i]) |> ignore
                if i < revColumns.Length - 1 then
                    sb.Append(revDelims.[i]) |> ignore
            sb.ToString()

/// Pure function to process a single line for app2 (initially reverses columns).
let processAppDataLine (line: string) (delim: string) : string =
    reverseColumnsLine line delim

/// Pure function to process a single line for app2-test (initially reverses columns).
let processAppTestDataLine (line: string) (delim: string) : string =
    reverseColumnsLine line delim