using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.Tls.Tests
{
    public class Tls10AvailableWithWeakCipherSuiteNotSelected : Tls11AvailableWithWeakCipherSuiteNotSelected
    {
        public override int Id => (int)TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected;

        public override string Name => nameof(Tls10AvailableWithWeakCipherSuiteNotSelected);

        public override TlsVersion Version => TlsVersion.TlsV1;
    }
}