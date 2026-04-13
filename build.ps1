[CmdletBinding()]
param (
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$BuildArguments
)

dotnet run --project build/_build.csproj -- @BuildArguments
