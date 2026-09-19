# Format C#: dotnet tool restore; dotnet csharpier format
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "dist"

dotnet publish (Join-Path $root "src\ReplayAssistant\ReplayAssistant.csproj") `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishAot=false `
  -p:EnableCompressionInSingleFile=true `
  -o $out

dotnet publish (Join-Path $root "src\ReplayAssistant.Update\ReplayAssistant.Update.csproj") `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishAot=false `
  -o $out

Copy-Item (Join-Path $root "appsettings.example.json") (Join-Path $out "appsettings.example.json") -Force

Write-Host "Published to $out"
