using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.Entity
{
    public class MxEntityCreated : Message
    {
        public MxEntityCreated(string id) 
            : base(id)
        {
        }

        public MxState State => MxState.Created;
    }
}
