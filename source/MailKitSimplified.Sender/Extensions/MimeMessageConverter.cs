using MimeKit;

namespace MailKitSimplified.Sender.Extensions
{
    public static class MimeMessageConverter
    {
        public static MimeMessage CopyAsTemplate(this MimeMessage original)
        {
            var copy = new MimeMessage();
            if (original.From.Count > 0)
                copy.From.AddRange(original.From);
            if (original.To.Count > 0)
                copy.To.AddRange(original.To);
            if (original.ReplyTo.Count > 0)
                copy.ReplyTo.AddRange(original.ReplyTo);
            if (original.Cc.Count > 0)
                copy.Cc.AddRange(original.Cc);
            if (original.Bcc.Count > 0)
                copy.Bcc.AddRange(original.Bcc);
            if (original.Sender != null)
                copy.Sender = original.Sender;
            copy.Subject = original.Subject;
            return copy;
        }
    }
}
