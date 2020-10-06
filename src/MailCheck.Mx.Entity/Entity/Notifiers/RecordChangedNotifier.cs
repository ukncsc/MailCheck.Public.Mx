using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Entity.Notifications;

namespace MailCheck.Mx.Entity.Entity.Notifiers
{
    public class RecordChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMxEntityConfig _mxEntityConfig;
        private readonly IEqualityComparer<Message> _hostMxEqualityComparer;

        public RecordChangedNotifier(IMessageDispatcher dispatcher, IMxEntityConfig mxEntityConfig, IEqualityComparer<Message> hostMxEqualityComparer)
        {
            _dispatcher = dispatcher;
            _mxEntityConfig = mxEntityConfig;
            _hostMxEqualityComparer = hostMxEqualityComparer;
        }

        public void Handle(MxEntityState state, Message message)
        {
            string domainName = message.Id;

            if (message is MxRecordsPolled mxRecordsPolled)
            {
                List<HostMxRecord> recordsInMessage = mxRecordsPolled.Records ?? new List<HostMxRecord>();
                List<HostMxRecord> recordsInState = state.HostMxRecords ?? new List<HostMxRecord>();

                List<Message> added = recordsInMessage.Except(recordsInState, _hostMxEqualityComparer).ToList();

                if (added.Any())
                {
                    MxRecordAdded mxRecordAdded = new MxRecordAdded(domainName, added.Cast<HostMxRecord>().ToList());
                    _dispatcher.Dispatch(mxRecordAdded, _mxEntityConfig.SnsTopicArn);
                }

                List<Message> removed = recordsInState.Except(recordsInMessage, _hostMxEqualityComparer).ToList();

                if (removed.Any())
                {
                    MxRecordRemoved mxRecordRemoved = new MxRecordRemoved(domainName, removed.Cast<HostMxRecord>().ToList());
                    _dispatcher.Dispatch(mxRecordRemoved, _mxEntityConfig.SnsTopicArn);
                }
            }
        }
    }
}