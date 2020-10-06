using MailCheck.Common.Contracts.Messaging;

namespace MailCheck.Mx.Contracts.External
{
    public class TlsScheduledReminder : ScheduledReminder
    {
        public TlsScheduledReminder(string id, string resourceId)
            : base(id, resourceId)
        {
        }
    }
}
