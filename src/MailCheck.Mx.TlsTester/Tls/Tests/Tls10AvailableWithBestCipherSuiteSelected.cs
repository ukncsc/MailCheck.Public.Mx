using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.Tls.Tests
{
    public class Tls10AvailableWithBestCipherSuiteSelected : Tls11AvailableWithBestCipherSuiteSelected
    {
        public override int Id => (int)TlsTestType.Tls10AvailableWithBestCipherSuiteSelected;

        public override string Name => nameof(Tls10AvailableWithBestCipherSuiteSelected);

        public override TlsVersion Version => TlsVersion.TlsV1;
    }
}