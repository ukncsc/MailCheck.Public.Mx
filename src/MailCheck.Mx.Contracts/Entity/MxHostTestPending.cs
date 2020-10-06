using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxHostTestPending : Message
    {
        public MxHostTestPending(string id) : base(id)
        {
        }
    }
}
