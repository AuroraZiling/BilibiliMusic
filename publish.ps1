param(
    [string] $Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop";

dotnet publish BiliBiliMusic/BiliBiliMusic.csproj -o "published/$Version/" -r win-x64 -p:PublishAot=true -p:AssemblyVersion=$Version -p:Configuration=Release;

Write-Output "Build Finished";

[Console]::ReadKey()