namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class SelectedCipherSuite
    {
        public SelectedCipherSuite(string testName, string cipherSuite)
        {
            TestName = testName;
            CipherSuite = cipherSuite;
        }

        public string TestName { get; }

        public string CipherSuite { get; }
    }
}
