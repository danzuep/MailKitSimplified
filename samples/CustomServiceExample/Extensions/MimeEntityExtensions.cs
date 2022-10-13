using MimeKit;

namespace MailKit.Simple.Core.Extensions
{
    public static class MimeEntityExtensions
    {
        public static MimeEntity BuildMultipart(this MimeEntity mimeBody, IEnumerable<MimeEntity> mimeEntities)
        {
            if (!mimeEntities?.Any() ?? true)
                return mimeBody;
            var multipart = new Multipart("mixed");
            if (mimeBody != null)
                multipart.Add(mimeBody);
            foreach (var mimeEntity in mimeEntities)
                if (mimeEntity != null)
                    multipart.Add(mimeEntity);
            return multipart;
        }
    }
}
