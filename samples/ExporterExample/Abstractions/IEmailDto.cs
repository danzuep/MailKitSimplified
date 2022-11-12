namespace ExporterExample.Abstractions
{
    /// <summary>
    /// Get DTO from standardised email header format.
    /// <seealso href="https://datatracker.ietf.org/doc/html/rfc8621#section-4.1.2.3">RFC 8621</seealso>
    /// </summary>
    public interface IEmailDto
    {
        DateTimeOffset? Date { get; set; }
        IEnumerable<string> From { get; set; }
        IEnumerable<string> To { get; set; }
        IEnumerable<string> Cc { get; set; }
        IEnumerable<string> Bcc { get; set; }
        string MessageId { get; set; }
        string Subject { get; set; }
    }
}
