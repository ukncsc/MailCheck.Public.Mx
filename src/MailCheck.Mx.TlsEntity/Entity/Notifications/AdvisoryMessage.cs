using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEntity.Entity.Notifications
{
    public class AdvisoryMessage
    {
        public AdvisoryMessage(MessageType messageType, string text,
            MessageDisplay messageDisplay = MessageDisplay.Standard)
        {
            MessageType = messageType;
            Text = text;
            MessageDisplay = messageDisplay;
        }

        public MessageType MessageType { get; }
        public string Text { get; }
        public MessageDisplay MessageDisplay { get; }
    }
}