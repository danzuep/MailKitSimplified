$latestVersion="2.5.2"
Set-Location -Path $PSScriptRoot/../samples
$folders = Get-ChildItem -Path . -Directory
foreach ($folder in $folders) {
    Write-Host $folder.FullName
    if ($folder.Name -ne "WorkerServiceExample") {
        Set-Location -Path $folder.Name
        dotnet add package MailKitSimplified.Sender --version $latestVersion
        dotnet add package MailKitSimplified.Receiver --version $latestVersion
        Set-Location -Path ..
    }
}