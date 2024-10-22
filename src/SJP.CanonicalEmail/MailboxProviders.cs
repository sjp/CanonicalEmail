namespace SJP.CanonicalEmail;

internal static class MailboxProviders
{
    public static readonly MailboxProvider Apple = new MailboxProvider(
        "Apple",
        Rules.PlusAddressing,
        ["icloud.com."]);

    public static readonly MailboxProvider Fastmail = new MailboxProvider(
        "Fastmail",
        Rules.PlusAddressing | Rules.LocalPartAsHostname,
        ["messagingengine.com."]);

    public static readonly MailboxProvider Google = new MailboxProvider(
        "Google",
        Rules.PlusAddressing | Rules.StripPeriods,
        ["google.com.", "googlemail.com."]);

    public static readonly MailboxProvider Microsoft = new MailboxProvider(
        "Microsoft",
        Rules.PlusAddressing,
        ["outlook.com."]);

    public static readonly MailboxProvider ProtonMail = new MailboxProvider(
        "Proton Mail",
        Rules.PlusAddressing,
        ["protonmail.ch."]);

    public static readonly MailboxProvider Rackspace = new MailboxProvider(
        "Rackspace",
        Rules.PlusAddressing,
        ["emailsrvr.com."]);

    public static readonly MailboxProvider Yahoo = new MailboxProvider(
        "Yahoo",
        Rules.DashAddressing | Rules.StripPeriods,
        ["yahoodns.net."]);

    public static readonly MailboxProvider Yandex = new MailboxProvider(
        "Yandex",
        Rules.PlusAddressing,
        ["mx.yandex.net.", "yandex.ru."]);

    public static readonly MailboxProvider Zoho = new MailboxProvider(
        "Zoho",
        Rules.PlusAddressing,
        ["zoho.com."]);

    public static readonly IEnumerable<MailboxProvider> All = new List<MailboxProvider>
    {
        Apple,
        Fastmail,
        Google,
        Microsoft,
        ProtonMail,
        Rackspace,
        Yahoo,
        Yandex,
        Zoho,
    };
}
