namespace SJP.CanonicalEmail;

public sealed record EmailResult
{
    public EmailResult(string address, string canonicalAddress, EmailResultStatus status)
    {
        Address = address;
        CanonicalAddress = canonicalAddress;
        Status = status;
    }

    public string Address { get; }

    public string CanonicalAddress { get; }

    public EmailResultStatus Status { get; }
}
