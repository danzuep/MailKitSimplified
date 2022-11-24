namespace MailKitSimplified.Core.Abstractions
{
    public interface IEmailWriter
    {
        IEmailWriter From(string emailAddress, string name = "");

        IEmailWriter To(string emailAddress, string name = "");

        IEmailWriter Cc(string emailAddress, string name = "");

        IEmailWriter Bcc(string emailAddress, string name = "");

        IEmailWriter Subject(string subject);

        IEmailWriter Body(string body, bool isHtml = true);

        IBasicEmail Result { get; }
    }
}
