using System.Net.Mail;
using DnsClient;
using DnsClient.Protocol;

namespace SJP.CanonicalEmail;

public class EmailCanonicalizer : IEmailCanonicalizer
{
    private readonly ILookupClient _dnsClient;

    public EmailCanonicalizer(ILookupClient dnsClient = null)
    {
        _dnsClient = dnsClient ?? new LookupClient();
    }

    public EmailResult Canonicalize(MailAddress mailAddress)
    {
        if (mailAddress == null)
            return new EmailResult(string.Empty, string.Empty, EmailResultStatus.InvalidEmail);

        return Canonicalize(mailAddress.Address);
    }

    public EmailResult Canonicalize(string emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return new EmailResult(emailAddress ?? string.Empty, emailAddress ?? string.Empty, EmailResultStatus.InvalidEmail);

        var loweredEmail = emailAddress.ToLowerInvariant();
        var parts = loweredEmail.Split('@');
        if (parts.Length != 2)
            return new EmailResult(emailAddress, loweredEmail, EmailResultStatus.InvalidEmail);

        var address = new EmailAddress(parts[0], parts[1]);

        var dnsResult = QueryMxRecords(address.Domain);
        if (!dnsResult.Success)
            return new EmailResult(emailAddress, loweredEmail, EmailResultStatus.DnsFailure);

        var mxRecords = dnsResult.Records;
        var provider = GetMailboxProvider(mxRecords);
        if (provider == null)
            return new EmailResult(emailAddress, loweredEmail, EmailResultStatus.UnknownProvider);

        var canonicalAddress = CreateCanonicalAddress(provider, address);
        return new EmailResult(emailAddress, canonicalAddress, EmailResultStatus.Success);
    }

    public Task<EmailResult> CanonicalizeAsync(MailAddress mailAddress, CancellationToken cancellationToken = default)
    {
        if (mailAddress == null)
            return Task.FromResult(new EmailResult(string.Empty, string.Empty, EmailResultStatus.InvalidEmail));

        return CanonicalizeAsync(mailAddress.Address, cancellationToken);
    }

    public async Task<EmailResult> CanonicalizeAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return new EmailResult(emailAddress ?? string.Empty, emailAddress ?? string.Empty, EmailResultStatus.InvalidEmail);

        var loweredEmail = emailAddress.ToLowerInvariant();
        var parts = emailAddress.ToLowerInvariant().Split('@');
        if (parts.Length != 2)
            return new EmailResult(emailAddress, loweredEmail, EmailResultStatus.InvalidEmail);

        var address = new EmailAddress(parts[0], parts[1]);

        var dnsResult = await QueryMxRecordsAsync(address.Domain, cancellationToken);
        if (!dnsResult.Success)
            return new EmailResult(emailAddress, loweredEmail, EmailResultStatus.DnsFailure);

        var mxRecords = dnsResult.Records;
        var provider = GetMailboxProvider(mxRecords);
        if (provider == null)
            return new EmailResult(emailAddress, loweredEmail, EmailResultStatus.UnknownProvider);

        var canonicalAddress = CreateCanonicalAddress(provider, address);
        return new EmailResult(emailAddress, canonicalAddress, EmailResultStatus.Success);
    }

    private static string CreateCanonicalAddress(MailboxProvider mailboxProvider, EmailAddress address)
    {
        var localPart = address.Local;
        var domainPart = address.Domain;

        if (mailboxProvider.Flags.HasFlag(Rules.LocalPartAsHostname))
        {
            var updatedAddress = LocalPartAsHostname(address);
            localPart = updatedAddress.Local;
            domainPart = updatedAddress.Domain;
        }

        if (mailboxProvider.Flags.HasFlag(Rules.StripPeriods))
        {
            localPart = localPart.Replace(".", string.Empty);
        }

        if (mailboxProvider.Flags.HasFlag(Rules.PlusAddressing))
        {
            localPart = localPart.Split('+', 2)[0];
        }

        if (mailboxProvider.Flags.HasFlag(Rules.DashAddressing))
        {
            localPart = localPart.Split('-', 2)[0];
        }

        return $"{localPart}@{domainPart}";
    }

    private DnsResolutionResult QueryMxRecords(string domain)
    {
        try
        {
            var result = _dnsClient.Query(domain, QueryType.MX);
            var records = result.Answers
                .MxRecords()
                .OrderBy(r => r.Preference)
                .ToList();

            return new DnsResolutionResult(true, records);
        }
        catch (DnsResponseException)
        {
            // Handle DNS resolution failures
            return new DnsResolutionResult(false, []);
        }
    }

    private async Task<DnsResolutionResult> QueryMxRecordsAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dnsClient.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
            var records = result.Answers
                .MxRecords()
                .OrderBy(r => r.Preference)
                .ToList();

            return new DnsResolutionResult(true, records);
        }
        catch (DnsResponseException)
        {
            // Handle DNS resolution failures
            return new DnsResolutionResult(false, []);
        }
    }

    private static MailboxProvider? GetMailboxProvider(IEnumerable<MxRecord> mxRecords)
    {
        return mxRecords
            .Where(mx => mx.Exchange?.Value != null)
            .Select(mx => mx.Exchange.Value) // this will contain a '.' suffix, e.g. "gmail.com."
            .Select(mxd => MailboxProviders.All.FirstOrDefault(p => p.MxDomains.Any(d =>
                string.Equals(mxd, d, StringComparison.OrdinalIgnoreCase) // matches domain exactly
                || mxd.EndsWith("." + d, StringComparison.OrdinalIgnoreCase)))) // or is a subdomain
            .FirstOrDefault(p => p != null);
    }

    private static EmailAddress LocalPartAsHostname(EmailAddress address)
    {
        var domainSegments = address.Domain.Split('.');
        if (domainSegments.Length <= 2)
            return address;

        var localPart = domainSegments[0];
        var domainPart = string.Join(".", domainSegments.Skip(1));

        return new EmailAddress(localPart, domainPart);
    }

    private sealed record EmailAddress
    {
        public EmailAddress(string local, string domain)
        {
            Local = local;
            Domain = domain;
        }

        public string Local { get; }

        public string Domain { get; }
    }
}
