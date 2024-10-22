using System.Net.Mail;

namespace SJP.CanonicalEmail
{
    public interface IEmailCanonicalizer
    {
        EmailResult Canonicalize(MailAddress mailAddress);

        EmailResult Canonicalize(string emailAddress);

        Task<EmailResult> CanonicalizeAsync(MailAddress mailAddress, CancellationToken cancellationToken = default);

        Task<EmailResult> CanonicalizeAsync(string emailAddress, CancellationToken cancellationToken = default);
    }
}