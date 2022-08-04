using MailCheck.Common.Contracts.Messaging;

namespace MailCheck.Mx.Contracts.External
{
    public class SimplifiedTlsExpired : ScheduledReminder
    {
        public SimplifiedTlsExpired(string id, string resourceId)
            : base(id, resourceId)
        {
        }
    }
}
