using MailCheck.Common.Contracts.Messaging;

namespace MailCheck.Mx.Contracts.External
{
    public class MxScheduledReminder : ScheduledReminder
    {
        public MxScheduledReminder(string id, string resourceId)
            : base(id, resourceId)
        {
        }
    }
}