# Run the smtp4dev container if it is not already running
$container = docker ps --filter "name=smtp4dev" --filter "status=running" --format "{{.Names}}"
if (-not $container) {
    # docker stop smtp4dev; docker rm smtp4dev;
    # docker run --name smtp4dev -p 3000:80 -p 25:25 -p 143:143 -d rnwood/smtp4dev
    docker-compose -f ./samples/docker-compose.yml up -d
    # Add a delay to let the container finish coming online
    Start-Sleep -Seconds 30
}

$smtpServer = "localhost"
$smtpPort = 25
$from = "sender@example.com"
$to = "recipient@example.com"
$subject = "Test Email"
$body = "This is a test email sent from PowerShell."

Send-MailMessage -From $from -To $to -Subject $subject -Body $body -SmtpServer $smtpServer -Port $smtpPort

Write-Host "Email sent successfully."
