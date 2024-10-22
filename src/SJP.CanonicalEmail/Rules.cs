namespace SJP.CanonicalEmail;

[Flags]
internal enum Rules
{
    /// <summary>
    /// Invalid state.
    /// </summary>
    None = 0,

    /// <summary>
    /// The <c>-</c> character can be used for tagging incoming email to a given address.
    /// e.g. <c>abc-example@yahoo.com</c> tags emails sent to the <c>abc@yahoo.com</c> address with <c>example</c>.
    /// </summary>
    DashAddressing = 1,

    /// <summary>
    /// The <c>+</c> character can be used for tagging incoming email to a given address,
    /// e.g. <c>abc+example@gmail.com</c> tags emails sent to the <c>abc@gmail.com</c> address with <c>example</c>.
    /// </summary>
    PlusAddressing = 2,

    /// <summary>
    /// The <c>.</c> character can be used to separate the "user" part of an email address from its
    /// domain when multiple <c>.</c> characters are present for the "user" part of the email address.
    /// </summary>
    LocalPartAsHostname = 4,

    /// <summary>
    /// The <c>.</c> character is supported for cosmetic purposes,
    /// e.g. <c>a.b.c@gmail.com</c> is equivalent to <c>abc@gmail.com</c>.
    /// </summary>
    StripPeriods = 8
}
