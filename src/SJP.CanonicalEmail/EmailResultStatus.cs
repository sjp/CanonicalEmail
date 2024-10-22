namespace SJP.CanonicalEmail;

public enum EmailResultStatus
{
    None,
    Success,
    InvalidEmail,
    DnsFailure,
    UnknownProvider,
}
