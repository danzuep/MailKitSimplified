Set-Location -Path $PSScriptRoot/../samples

# Check if dotnet-outdated-tool is installed
$toolName = "dotnet-outdated-tool"
$toolInstalled = dotnet tool list --global | Select-String -Pattern $toolName
if (-not $toolInstalled) {
    Write-Host "$toolName is not installed. Installing..."
    dotnet tool install --global $toolName
}

# Update all packages to their latest versions
$folders = Get-ChildItem -Path . -Directory
foreach ($folder in $folders) {
    Write-Host $folder.FullName
    Set-Location -Path $folder.FullName
    dotnet outdated -u
}

Set-Location -Path $PSScriptRoot/..
