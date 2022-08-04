using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Simplified
{
    public class SimplifiedTlsTestPending : Message
    {
        public SimplifiedTlsTestPending(string id) : base(id)
        {
        }

        public string ReceiptHandle { get; set; }
    }
}
