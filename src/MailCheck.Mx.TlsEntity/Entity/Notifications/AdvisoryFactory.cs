using System;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.Mx.TlsEntity.Entity.Notifications
{
    public static class AdvisoryFactory
    {
        public static AdvisoryMessage Create(MessageType messageType, string text, MessageDisplay messageDisplay = MessageDisplay.Standard)
        {
            return new AdvisoryMessage(Guid.Empty, messageType, text, null, messageDisplay);
        }
    }
}