using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxPollPending : Message
    {
        public MxPollPending(string id) : base(id)
        {
        }
    }
}
