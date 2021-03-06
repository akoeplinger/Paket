﻿module Paket.Resolver.ConflictSourcesSpecs

open Paket
open NUnit.Framework
open FsUnit
open TestHelpers
open Paket.Domain
open Paket.Requirements

let noGitHubConfigured _ = failwith "no GitHub configured"

let config1 = """
source "http://nuget.org/api/v2"

github fsharp/fsharp:master foo.fs
github fsprojects/FAKE:master test.fs
"""

[<Test>]
let ``should resolve source files with correct sha``() =
    let name = PackageName "name"
    let dep =
      { Requirements.PackageRequirement.Sources = []
        Requirements.PackageRequirement.Name = name
        Requirements.PackageRequirement.ResolverStrategy = Max
        Requirements.PackageRequirement.Parent = Requirements.PackageRequirementSource.DependenciesFile ""
        Requirements.PackageRequirement.FrameworkRestrictions = []
        Requirements.PackageRequirement.VersionRequirement = VersionRequirement.NoRestriction }
    let sha = "sha1"
    let cfg = DependenciesFile.FromCode(config1)
    let resolved = ModuleResolver.Resolve((fun _ -> [dep]), (fun _ _ _ _ -> sha), cfg.RemoteFiles)
    resolved
    |> shouldContain
      { Owner = "fsharp"
        Project = "fsharp"
        Name = "foo.fs"
        Commit = sha
        Dependencies = [name, VersionRequirement.NoRestriction] |> Set.ofList
        Origin = ModuleResolver.SingleSourceFileOrigin.GitHubLink }

let config2 = """
source "http://nuget.org/api/v2"

github fsharp/fsharp:master foo.fs
github fsharp/fsharp:fsharp4 bar.fs
github fsprojects/FAKE:master test.fs
github fsprojects/FAKE:vNext readme.md
"""

let expectedError = """Found conflicting source file requirements:
   - fsharp/fsharp
     Versions:
     - master
     - fsharp4
   - fsprojects/FAKE
     Versions:
     - master
     - vNext
   Currently multiple versions for same source directory are not supported.
   Please adjust the dependencies file.""" |> normalizeLineEndings

[<Test>]
let ``should fail resolving source files from same repository but different versions``() =
    try
        let cfg = DependenciesFile.FromCode(config2)
        ModuleResolver.Resolve(noGitHubConfigured, noGitHubConfigured, cfg.RemoteFiles) |> ignore
    with
    | ex -> ex.Message |> shouldEqual expectedError