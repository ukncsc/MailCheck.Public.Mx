using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.TlsEntity.Entity.Notifications
{
    public class TlsCertAdvisoryRemoved : Message
    {
        public TlsCertAdvisoryRemoved(string id, string host, List<AdvisoryMessage> messages) : base(id)
        {
            Host = host;
            Messages = messages;
        }

        public string Host { get; }

        public List<AdvisoryMessage> Messages { get; }
    }
}
