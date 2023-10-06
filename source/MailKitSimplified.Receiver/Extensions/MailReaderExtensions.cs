using MailKit.Search;
using System.Collections.Generic;
using System;
using MailKitSimplified.Receiver.Abstractions;
using MailKit;

namespace MailKitSimplified.Receiver.Extensions
{
    public static class MailReaderExtensions
    {
        /// <summary>Core message summary items: UniqueId, Envelope, Headers, Size, and BodyStructure.</summary>
        /// <returns><see cref="IMailReader"/> query with a maximum of 250 results.</returns>
        public static IMailReader ItemsForMimeMessages(this IMailReader mailReader, MessageSummaryItems extras = MessageSummaryItems.UniqueId)
        {
            var coreMessageItems = extras |
                MessageSummaryItems.Envelope |
                MessageSummaryItems.Headers |
                MessageSummaryItems.Size |
                MessageSummaryItems.BodyStructure;
            return mailReader.Items(coreMessageItems);
        }

        /// <summary>Query just the arrival dates of messages on the server.</summary>
        /// <param name="deliveredAfter">Search for messages after this date.</param>
        /// <param name="deliveredBefore">Search for messages before this date.</param>
        /// <returns><see cref="IMailReader"/> query with a maximum of 250 results.</returns>
        public static IMailReader QueryBetweenDates(this IMailReader mailReader, DateTime deliveredAfter, DateTime? deliveredBefore = null)
        {
            DateTime before = deliveredBefore != null ? deliveredBefore.Value : DateTime.Now;
            var query = SearchQuery.DeliveredAfter(deliveredAfter).And(SearchQuery.DeliveredBefore(before));
            return mailReader.Query(query);
        }

        /// <summary>Query the server for messages with matching keywords in the subject or body text.</summary>
        /// <param name="keywords">Keywords to search for.</param>
        /// <returns><see cref="SearchQuery"/> query with a maximum of 250 results.</returns>
        public static IMailReader QueryKeywords(this IMailReader mailReader, IEnumerable<string> keywords)
        {
            var subjectMatch = keywords.MatchAny(SearchQuery.SubjectContains);
            var bodyMatch = keywords.MatchAny(SearchQuery.BodyContains);
            var query = subjectMatch.Or(bodyMatch);
            return mailReader.Query(query);
        }

        /// <summary>Query the server for message(s) with a matching message ID.</summary>
        /// <param name="messageId">Message-ID to search for.</param>
        /// <param name="addAngleBrackets">Angle brackets added by default.</param>
        /// <returns><see cref="IMailReader"/> query with a maximum of 250 results.</returns>
        public static IMailReader QueryMessageId(this IMailReader mailReader, string messageId, bool addAngleBrackets = true)
        {
            var searchText = addAngleBrackets ? $"<{messageId}>" : messageId;
            var query = SearchQuery.HeaderContains("Message-Id", searchText);
            return mailReader.Query(query);
        }
    }
}
