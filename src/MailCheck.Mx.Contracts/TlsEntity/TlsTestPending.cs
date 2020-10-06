using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.TlsEntity
{
    public class TlsTestPending : Message
    {
        public TlsTestPending(string id) : base(id)
        {
        }
    }
}
