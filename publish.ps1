param(
    [string] $Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop";

Write-Output "Build AOT";

dotnet publish BiliBiliMusic/BiliBiliMusic.csproj -o "published/$Version/AOT/" -r win-x64 -p:PublishAot=true -p:AssemblyVersion=$Version -p:Configuration=Release;

Remove-Item -Path ./published/$Version/AOT/av_libglesv2.dll
Remove-Item -Path ./published/$Version/AOT/BiliBiliMusic.pdb
Remove-Item -Path ./published/$Version/AOT/libHarfBuzzSharp.dll
Remove-Item -Path ./published/$Version/AOT/libSkiaSharp.dll

Rename-Item -Path ./published/$Version/AOT/BiliBiliMusic.exe -NewName 'BiliBiliMusicAOT.exe'

Write-Output "Build Compressed Single File";

dotnet publish BiliBiliMusic/BiliBiliMusic.csproj -o "published/$Version/CompressedSingleFile/" -r win-x64 -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishSingleFile=true -p:SelfContained=true -p:EnableCompressionInSingleFile=true -p:AssemblyVersion=$Version -p:Configuration=Release;

Write-Output "Build Finished";

[Console]::ReadKey()