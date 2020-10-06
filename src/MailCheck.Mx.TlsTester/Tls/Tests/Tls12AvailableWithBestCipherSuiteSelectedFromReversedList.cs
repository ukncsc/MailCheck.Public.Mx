using System.Collections.Generic;
using System.Linq;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.Tls.Tests
{
    public class Tls12AvailableWithBestCipherSuiteSelectedFromReversedList :
        Tls12AvailableWithBestCipherSuiteSelected
    {
        public override int Id => (int)TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList;

        public override string Name => nameof(Tls12AvailableWithBestCipherSuiteSelectedFromReversedList);

        public override List<CipherSuite> CipherSuites => Enumerable.Reverse(base.CipherSuites).ToList();
    }
}