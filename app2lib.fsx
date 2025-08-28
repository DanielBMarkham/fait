// app2lib.fsx
// Purely functional library for app2 and app2-test
// No side effects, no I/O, no logging

module App2Lib

open System
open System.Text.RegularExpressions

/// Reverses the columns in a single line based on the delimiter regex pattern.
/// Splits the line using the regex, reverses the array of parts, and joins back using the delimiter string.
let private reverseColumnsLine (line: string) (delim: string) : string =
    let regex = Regex(delim)
    let parts = regex.Split(line)
    String.Join(delim, Array.rev parts)

/// Pure function to process a single line for app2 (initially reverses columns).
let processAppDataLine (line: string) (delim: string) : string =
    reverseColumnsLine line delim

/// Pure function to process a single line for app2-test (initially reverses columns).
let processAppTestDataLine (line: string) (delim: string) : string =
    reverseColumnsLine line delim