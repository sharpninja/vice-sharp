#!/usr/bin/env bash
set -eo pipefail
dotnet run --project build/_build.csproj -- "$@"
