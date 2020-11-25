using MailCheck.Mx.Contracts.Tester;

namespace MailCheck.Mx.TlsTester.Domain
{
    public class MxHostTestDetails
    {
        public MxHostTestDetails(TlsTestPending test)
        {
            Test = test;
        }

        public TlsTestPending Test { get; }
        public string NormalizedHostname { get; set;  }
        public TlsTestResults TestResults { get; set; }        
        public bool PublishedResultsSuccessfully { get; set; }
        public bool SkipTesting { get; set; }
    }
}
