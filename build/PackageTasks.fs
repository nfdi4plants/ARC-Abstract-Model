module PackageTasks

open MessagePrompts
open BasicTasks
open TranspileTasks
open TestTasks
open ProjectInfo
open Helpers

open BlackFox.Fake
open Fake.Core
open Fake.IO.Globbing.Operators

open System.Text.RegularExpressions

/// https://github.com/Freymaurer/Fake.Extensions.Release#release-notes-in-nuget
let private replaceCommitLink input = 
    let commitLinkPattern = @"\[\[#[a-z0-9]*\]\(.*\)\] "
    Regex.Replace(input,commitLinkPattern,"")

module BundleDotNet =
    let bundle (versionTag : string) (versionSuffix : string option) =
        System.IO.Directory.CreateDirectory(ProjectInfo.netPkgDir) |> ignore
        !! "src/*/*.fsproj"
        -- "src/bin/*"
        |> Seq.filter (fun (p : string) ->
            not (p.Contains("Python"))
        )
        |> Seq.iter (Fake.DotNet.DotNet.pack (fun p ->           
            let msBuildParams =
                {p.MSBuildParams with 
                    Properties = ([
                        "Version",versionTag
                        "PackageReleaseNotes",  (versionController.Notes |> List.map replaceCommitLink |> String.toLines )
                    ] @ p.MSBuildParams.Properties)
                    DisableInternalBinLog = true
                }
            {
                p with 
                    VersionSuffix = versionSuffix
                    MSBuildParams = msBuildParams
                    OutputPath = Some ProjectInfo.netPkgDir
            }
        ))

let packDotNet = BuildTask.create "PackDotNet" [clean; buildSolution; (*runTests*)] {
    if versionController.IsPrerelease then
        BundleDotNet.bundle versionController.NugetTag (Some versionController.NugetTag)
    else 
        BundleDotNet.bundle versionController.NugetTag None
}

let packJS = BuildTask.create "PackJS" [clean; buildSolution; transpileTS] {
    System.IO.Directory.CreateDirectory(npmPkgDir) |> ignore
    for package in ["build/out/base-model/js/node_modules/arc-base-model";
                    "build/out/polyglot-sqlite/js/node_modules/polyglot-sqlite"] do
        Fake.JavaScript.Npm.exec $"pack {package} --ignore-scripts --pack-destination {npmPkgDir}" id
}

let packPy = BuildTask.create "PackPy" [clean; buildSolution; transpilePy] {
    System.IO.Directory.CreateDirectory(pyPkgDir) |> ignore
    for root, package, distribution, requiresPython in
        ["build/out/base-model/python", "arc_base_model", "arc-base-model", ">=3.11";
         "build/out/polyglot-sqlite/python", "polyglot_sqlite", "polyglot-sqlite", ">=3.12"] do
        run uv $"run --no-sync --cache-dir build/out/uv-cache python build/native_wheel.py {root} {package} {distribution} {pyPkgDir} --requires-python \"{requiresPython}\"" "."
}

let pack = BuildTask.createEmpty "Pack" [packDotNet; packJS; packPy]
