#!/usr/bin/env bash
set -euo pipefail

cd RoslynAnalysis && dotnet run -- coverage-analysis ../RoslynRefactoring.Tests/RoslynRefactoring.Tests.csproj
