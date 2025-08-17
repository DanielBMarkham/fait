#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Diagnostics

let verifyOutput (actual: string) (expected: string) : string =
    if actual.Trim() = expected.Trim() then "PASS" else "FAIL"

let verifyErrorContains (err: string) (substring: string) : string =
    if err.Contains(substring) then "PASS" else "FAIL"

let main () =
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

    // Test 1: Stdin to stdout (piped input/output)
    try
        let input1 = "a\tb\tc\n1\t2\t3\n"
        let expected1 = "c\tb\ta\n3\t2\t1"
        let out1, err1, code1 = runProcessor "" (Some input1)
        let result1 = verifyOutput out1 expected1
        Console.WriteLine $"Test 1 (stdin/stdout): {result1} (Exit code: {code1}, Error: '{err1.Trim()}')"
    with e ->
        Console.Error.WriteLine $"Error in Test 1: {e.Message}"

    // Test 2: Input file to output file
    try
        let inputFile = Path.GetTempFileName()
        let outputFile = Path.GetTempFileName()
        File.WriteAllText(inputFile, "a\tb\tc\n1\t2\t3\n")
        let out2, err2, code2 = runProcessor $"-i {inputFile} -o {outputFile}" None
        let resultText = File.ReadAllText(outputFile)
        let result2 = verifyOutput resultText "c\tb\ta\n3\t2\t1"
        Console.WriteLine $"Test 2 (file in/out): {result2} (Exit code: {code2}, Error: '{err2.Trim()}')"
        File.Delete inputFile
        File.Delete outputFile
    with e ->
        Console.Error.WriteLine $"Error in Test 2: {e.Message}"

    // Test 3: Bad input file, fallback with empty output
    try
        let badFile = "nonexistent_file_123.txt"
        let out3, err3, code3 = runProcessor $"-i {badFile}" None
        let result3 = verifyOutput out3 ""
        let errorCheck = verifyErrorContains err3 "Error opening input file"
        Console.WriteLine $"Test 3 (bad input file): Output {result3}, Error check {errorCheck} (Exit code: {code3})"
    with e ->
        Console.Error.WriteLine $"Error in Test 3: {e.Message}"

main ()
