using MailCheck.Mx.Contracts.Tester;

namespace MailCheck.Mx.TlsTester.Domain
{
    public class MxHostTestDetails
    {
        public MxHostTestDetails(TlsTestResults testResults, TlsTestPending test)
        {
            TestResults = testResults;
            Test = test;
        }

        public TlsTestResults TestResults { get; }
        public TlsTestPending Test { get; }
        public bool PublishedResultsSuccessfully { get; set; }
    }
}
