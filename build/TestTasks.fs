module TestTasks

open System.IO
open BlackFox.Fake
open Fake.Core
open Fake.DotNet

open ProjectInfo
open BasicTasks

let skipTestsFlag = "--skipTests"

let failOnFocusFlag = "--fail-on-focused-tests"

[<Literal>]
let jsIOResultFolder = "./tests/TestingUtils/TestResults/js"

[<Literal>]
let pyIOResultFolder = "./tests/TestingUtils/TestResults/py"

let private runTool command args =
    if System.OperatingSystem.IsWindows() then
        CreateProcess.fromRawCommand "cmd.exe" (["/c"; command] @ args)
        |> Proc.run
    else
        CreateProcess.fromRawCommand command args
        |> Proc.run

let private runPyxpectoDotNet project =
    let args =
        [
            "--project"
            $"\"{project}\""
            "--configuration"
            configuration
            "--no-build"
        ]
        |> String.concat " "

    let result = DotNet.exec id "run" args

    if not result.OK then
        failwithf "Pyxpecto tests failed for %s" project

let runTestsDotnet =
    BuildTask.createEmpty "RunTestsDotnet"
        [BaseModelTasks.testBaseModelDotNet; PolyglotSQLiteTasks.testPolyglotSQLiteDotNet]

let runTestsJs =
    BuildTask.createEmpty "RunTestsJS"
        [BaseModelTasks.testBaseModelJS; PolyglotSQLiteTasks.testPolyglotSQLiteJS]

let runTestsPy =
    BuildTask.createEmpty "RunTestsPy"
        [BaseModelTasks.testBaseModelPy; PolyglotSQLiteTasks.testPolyglotSQLitePy]

let runTests =
    BuildTask.createEmpty "RunTests"
        [clean; buildSolution; BaseModelTasks.testBaseModel; PolyglotSQLiteTasks.testPolyglotSQLite]
