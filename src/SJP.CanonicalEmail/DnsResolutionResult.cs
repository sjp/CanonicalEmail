using DnsClient.Protocol;

namespace SJP.CanonicalEmail;

internal sealed record DnsResolutionResult
{
    public DnsResolutionResult(bool success, IReadOnlyCollection<MxRecord> records)
    {
        Success = success;
        Records = records;
    }

    public bool Success { get; }

    public IReadOnlyCollection<MxRecord> Records { get; }
}