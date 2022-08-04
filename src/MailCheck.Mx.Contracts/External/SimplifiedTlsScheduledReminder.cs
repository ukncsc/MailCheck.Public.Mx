using MailCheck.Common.Contracts.Messaging;

namespace MailCheck.Mx.Contracts.External
{
    public class SimplifiedTlsScheduledReminder : ScheduledReminder
    {
        public SimplifiedTlsScheduledReminder(string id, string resourceId)
            : base(id, resourceId)
        {
        }
    }
}
