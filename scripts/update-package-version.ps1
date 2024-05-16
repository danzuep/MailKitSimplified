$latestVersion="2.10.0"
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
# docker run -d -p 3000:80 -p 25:25 -p 143:143 rnwood/smtp4dev