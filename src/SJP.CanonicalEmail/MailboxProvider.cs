namespace SJP.CanonicalEmail;

internal sealed record MailboxProvider
{
    public MailboxProvider(string name, Rules rules, IEnumerable<string> mxDomains)
    {
        Name = name;
        Flags = rules;
        MxDomains = mxDomains;
    }

    public string Name { get; }

    public Rules Flags { get; }

    public IEnumerable<string> MxDomains { get; }
}
