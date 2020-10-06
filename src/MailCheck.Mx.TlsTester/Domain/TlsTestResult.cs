using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Tls;

namespace MailCheck.Mx.TlsTester.Domain
{
    public class TlsTestResult
    {
        public TlsTestResult(ITlsTest test, BouncyCastleTlsTestResult result)
        {
            Test = test;
            Result = result;
        }

        public ITlsTest Test { get; }
        public BouncyCastleTlsTestResult Result { get; }
    }
}