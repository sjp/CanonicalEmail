using System.Net.Mail;
using DnsClient;
using DnsClient.Protocol;
using Moq;

namespace SJP.CanonicalEmail.Tests
{
    public class EmailCanonicalizerTests
    {
        private Mock<ILookupClient> _dnsClient;

        private EmailCanonicalizer _canonicalizer;

        private List<MxRecord> _mxRecords;

        [SetUp]
        public void Setup()
        {
            _mxRecords = [];

            var queryResult = new Mock<IDnsQueryResponse>(MockBehavior.Strict);
            queryResult
                .Setup(qr => qr.Answers)
                .Returns(() => _mxRecords);

            _dnsClient = new Mock<ILookupClient>(MockBehavior.Strict);
            _dnsClient
                .Setup(dns => dns.Query(It.IsAny<string>(), It.IsAny<QueryType>(), QueryClass.IN))
                .Returns(() => queryResult.Object);
            _dnsClient
                .Setup(dns => dns.QueryAsync(It.IsAny<string>(), It.IsAny<QueryType>(), QueryClass.IN, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => queryResult.Object);

            _canonicalizer = new EmailCanonicalizer(_dnsClient.Object);
        }

        [Test]
        public void Canonicalize_WhenGivenNullMailAddress_ReturnsEmptyResult()
        {
            var result = _canonicalizer.Canonicalize((MailAddress)null!);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.Empty);
                Assert.That(result.CanonicalAddress, Is.Empty);
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [Test]
        public void Canonicalize_WhenGivenNullStringMailAddress_ReturnsEmptyResult()
        {
            var result = _canonicalizer.Canonicalize((string)null!);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.Empty);
                Assert.That(result.CanonicalAddress, Is.Empty);
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [TestCase("")]
        [TestCase("    ")]
        public void Canonicalize_WhenGivenEmptyStringMailAddress_ReturnsEmptyResult(string emailAddress)
        {
            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [TestCase("test@test@test.com")]
        [TestCase("test@test@test@test.com")]
        [TestCase("test.com")]
        public void Canonicalize_WhenGivenMailAddressWithoutSingleAt_ReturnsEmptyResult(string emailAddress)
        {
            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [TestCase("test@TEST@test.com")]
        [TestCase("test@TEST@test@test.com")]
        [TestCase("test.COM")]
        public void Canonicalize_WhenGivenMailAddressWithoutSingleAt_ReturnsLowerCasedEmail(string emailAddress)
        {
            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress.ToLowerInvariant()));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [Test]
        public void Canonicalize_WhenDnsQueryFails_ReturnsDnsFailureResult()
        {
            const string emailAddress = "test@example.com";
            _dnsClient
                .Setup(dns => dns.Query(It.IsAny<string>(), It.IsAny<QueryType>(), QueryClass.IN))
                .Throws(() => new DnsResponseException("dns broke"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.DnsFailure));
            });
        }

        [Test]
        public void Canonicalize_WhenDnsQueryFails_ReturnsLowerCasedEmail()
        {
            const string emailAddress = "teST@exaMPLE.com";
            _dnsClient
                .Setup(dns => dns.Query(It.IsAny<string>(), It.IsAny<QueryType>(), QueryClass.IN))
                .Throws(() => new DnsResponseException("dns broke"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress.ToLowerInvariant()));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.DnsFailure));
            });
        }

        [Test]
        public void Canonicalize_WhenUnknownEmailProviderReturned_ReturnsUnknownProviderResult()
        {
            const string emailAddress = "test@example.com";

            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.UnknownProvider));
            });
        }

        [Test]
        public void Canonicalize_WhenUnknownEmailProviderReturned_ReturnsLowerCasedEmail()
        {
            const string emailAddress = "teST@exaMPLE.com";

            var result = _canonicalizer.Canonicalize(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress.ToLowerInvariant()));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.UnknownProvider));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenAppleEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("icloud.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenAppleEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.icloud.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("test@example.example.com")]
        [TestCase("test@EXAMPLE.EXample.COM")]
        [TestCase("test@exaMPLe.example.com")]
        public void Canonicalize_WhenFastmailEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("messagingengine.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("test@example.example.com")]
        [TestCase("test@EXAMPLE.EXample.COM")]
        [TestCase("test@exaMPLe.example.com")]
        public void Canonicalize_WhenFastmailEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.messagingengine.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE+tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public void Canonicalize_WhenGoogleEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("googlemail.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE+tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public void Canonicalize_WhenGoogleEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.googlemail.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenMicrosoftEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("outlook.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenMicrosoftEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.outlook.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenProtonMailEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("protonmail.ch"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenProtonMailEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.protonmail.ch"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenRackspaceEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("emailsrvr.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenRackspaceEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.emailsrvr.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example-test@example.com")]
        [TestCase("example-test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE-test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE-tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public void Canonicalize_WhenYahooEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("yahoodns.net"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example-test@example.com")]
        [TestCase("example-test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE-test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE-tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public void Canonicalize_WhenYahooEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.yahoodns.net"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenYandexEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("yandex.ru"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenYandexEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.yandex.ru"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenYandexNetEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("mx.yandex.net"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenYandexNetEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.mx.yandex.net"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenZohoEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("zoho.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public void Canonicalize_WhenZohoEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.zoho.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [Test]
        public async Task CanonicalizeAsync_WhenGivenNullMailAddress_ReturnsEmptyResult()
        {
            var result = await _canonicalizer.CanonicalizeAsync((MailAddress)null!);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.Empty);
                Assert.That(result.CanonicalAddress, Is.Empty);
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [Test]
        public async Task CanonicalizeAsync_WhenGivenNullStringMailAddress_ReturnsEmptyResult()
        {
            var result = await _canonicalizer.CanonicalizeAsync((string)null!);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.Empty);
                Assert.That(result.CanonicalAddress, Is.Empty);
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [TestCase("")]
        [TestCase("    ")]
        public async Task CanonicalizeAsync_WhenGivenEmptyStringMailAddress_ReturnsEmptyResult(string emailAddress)
        {
            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [TestCase("test@test@test.com")]
        [TestCase("test@test@test@test.com")]
        [TestCase("test.com")]
        public async Task CanonicalizeAsync_WhenGivenMailAddressWithoutSingleAt_ReturnsEmptyResult(string emailAddress)
        {
            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }

        [TestCase("test@TEST@test.com")]
        [TestCase("test@TEST@test@test.com")]
        [TestCase("test.COM")]
        public async Task CanonicalizeAsync_WhenGivenMailAddressWithoutSingleAt_ReturnsLowerCasedEmail(string emailAddress)
        {
            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress.ToLowerInvariant()));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.InvalidEmail));
            });
        }



        [Test]
        public async Task CanonicalizeAsync_WhenDnsQueryFails_ReturnsDnsFailureResult()
        {
            const string emailAddress = "test@example.com";
            _dnsClient
                .Setup(dns => dns.QueryAsync(It.IsAny<string>(), It.IsAny<QueryType>(), QueryClass.IN, It.IsAny<CancellationToken>()))
                .Throws(() => new DnsResponseException("dns broke"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.DnsFailure));
            });
        }


        [Test]
        public async Task CanonicalizeAsync_WhenDnsQueryFails_ReturnsLowerCasedEmail()
        {
            const string emailAddress = "teST@exaMPLE.com";
            _dnsClient
                .Setup(dns => dns.QueryAsync(It.IsAny<string>(), It.IsAny<QueryType>(), QueryClass.IN, It.IsAny<CancellationToken>()))
                .Throws(() => new DnsResponseException("dns broke"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress.ToLowerInvariant()));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.DnsFailure));
            });
        }




        [Test]
        public async Task CanonicalizeAsync_WhenUnknownEmailProviderReturned_ReturnsUnknownProviderResult()
        {
            const string emailAddress = "test@example.com";

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.UnknownProvider));
            });
        }

        [Test]
        public async Task CanonicalizeAsync_WhenUnknownEmailProviderReturned_ReturnsLowerCasedEmail()
        {
            const string emailAddress = "teST@exaMPLE.com";

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(emailAddress.ToLowerInvariant()));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.UnknownProvider));
            });
        }












        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenAppleEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("icloud.com"));

            var result = _canonicalizer.Canonicalize(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenAppleEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.icloud.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("test@example.example.com")]
        [TestCase("test@EXAMPLE.EXample.COM")]
        [TestCase("test@exaMPLe.example.com")]
        public async Task CanonicalizeAsync_WhenFastmailEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("messagingengine.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("test@example.example.com")]
        [TestCase("test@EXAMPLE.EXample.COM")]
        [TestCase("test@exaMPLe.example.com")]
        public async Task CanonicalizeAsync_WhenFastmailEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.messagingengine.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE+tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public async Task CanonicalizeAsync_WhenGoogleEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("googlemail.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE+tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public async Task CanonicalizeAsync_WhenGoogleEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.googlemail.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenMicrosoftEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("outlook.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenMicrosoftEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.outlook.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenProtonMailEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("protonmail.ch"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenProtonMailEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.protonmail.ch"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenRackspaceEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("emailsrvr.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenRackspaceEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.emailsrvr.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example-test@example.com")]
        [TestCase("example-test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE-test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE-tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public async Task CanonicalizeAsync_WhenYahooEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("yahoodns.net"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example-test@example.com")]
        [TestCase("example-test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE-test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        [TestCase("ex.am.ple@example.com")]
        [TestCase("EXA.MP.LE-tes.t@EXample.COM")]
        [TestCase("EXAM...PLE@EXAMPLE.COM")]
        public async Task CanonicalizeAsync_WhenYahooEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.yahoodns.net"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenYandexEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("yandex.ru"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenYandexEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.yandex.ru"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenYandexNetEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("mx.yandex.net"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenYandexNetEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.mx.yandex.net"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenZohoEmailProvided_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("zoho.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        [TestCase("example@example.com")]
        [TestCase("example+test@example.com")]
        [TestCase("example+test_123@example.com")]
        [TestCase("EXAMPLE@EXAMPLE.COM")]
        [TestCase("EXAMPLE+test@EXample.COM")]
        [TestCase("exaMPLe@example.com")]
        public async Task CanonicalizeAsync_WhenZohoEmailSubdomainResolved_ReturnsExpectedEmails(string emailAddress)
        {
            _mxRecords.Add(CreateMxRecordForDomain("test-subdomain.zoho.com"));

            var result = await _canonicalizer.CanonicalizeAsync(emailAddress);

            const string expectedEmail = "example@example.com";

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Address, Is.EqualTo(emailAddress));
                Assert.That(result.CanonicalAddress, Is.EqualTo(expectedEmail));
                Assert.That(result.Status, Is.EqualTo(EmailResultStatus.Success));
            });
        }

        private static MxRecord CreateMxRecordForDomain(string domain)
        {
            return new MxRecord(
                new ResourceRecordInfo(domain, ResourceRecordType.MX, QueryClass.IN, 100, 100),
                100,
                DnsString.Parse(domain));
        }
    }
}