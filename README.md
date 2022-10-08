# MailKitSimplified.Sender ![Build and test workflow result badge](https://github.com/danzuep/MailKitSimplified.Sender/workflows/Pipeline/badge.svg)

When I first started using MailKit I was surprised at how many steps were involved in getting it all set up and working. The aim of this package is to make sending an email as simple as possible.

## Usage

### Setup

Either dependency injection (IEmailSender smtpSender) or:

using var smtpSender = EmailSender.Create("smtp.example.com");

### Sending Mail

var email = smtpSender.Email
    .From("me@example.com")
    .To("you@example.com")
    .Subject("Hi")
    .Body("~")
    .Attach("C:/Temp/attachment1.txt", "C:/Temp/attachment2.pdf");

await email.SendAsync();
