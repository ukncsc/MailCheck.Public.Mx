using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.TlsEntity.Entity.Notifications
{
    public class TlsAdvisoryAdded : Message
    {
        public TlsAdvisoryAdded(string id, List<AdvisoryMessage> messages) : base(id)
        {
            Messages = messages;
        }

        public List<AdvisoryMessage> Messages { get; }
    }
}