namespace MailCheck.Mx.TlsTester.Domain
{
    public class TlsTestPending : Contracts.TlsEntity.TlsTestPending
    {
        public TlsTestPending(string id) : base(id)
        {

        }

        public string ReceiptHandle { get; set; }
    }
}
