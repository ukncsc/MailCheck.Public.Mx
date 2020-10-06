using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.TlsEntity.Entity.Notifications
{
    public class TlsAdvisoryRemoved : Message
    {
        public TlsAdvisoryRemoved(string id, List<AdvisoryMessage> messages) : base(id)
        {
            Messages = messages;
        }

        public List<AdvisoryMessage> Messages { get; }
    }
}
