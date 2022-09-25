# MailKitSimplified

## Usage

### Setup

Either dependance injection (IEmailSender smtpSender) or:

using var smtpSender = EmailSender.Create("smtp.example.com");

### Sending Mail

var email = smtpSender.Email
    .From("me@example.com")
    .To("you@example.com")
    .Subject("Hi")
    .Body("~")
    .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf");

await email.SendAsync();
