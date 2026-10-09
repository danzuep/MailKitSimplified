namespace MailKitSimplified.Generic.Abstractions
{
    /// <summary>Represents an immutable node in an email's content tree.</summary>
    public interface IEmailAstNode
    {
        /// <summary>Dispatches this node to its typed visitor overload.</summary>
        /// <param name="visitor">The visitor controlling traversal or transformation.</param>
        void Accept(IEmailVisitor visitor);
    }
}