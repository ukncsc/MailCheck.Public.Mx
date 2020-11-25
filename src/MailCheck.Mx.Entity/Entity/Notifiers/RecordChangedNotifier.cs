using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Entity.Config;
using MailCheck.Mx.Entity.Entity.Notifications;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MailCheck.Mx.Entity.Entity.Notifiers
{
    public class RecordChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMxEntityConfig _mxEntityConfig;
        private readonly IEqualityComparer<HostMxRecord> _comparer;
        private readonly ILogger<RecordChangedNotifier> _logger;

        public RecordChangedNotifier(IMessageDispatcher dispatcher, IMxEntityConfig mxEntityConfig, IEqualityComparer<HostMxRecord> comparer, ILogger<RecordChangedNotifier> logger)
        {
            _dispatcher = dispatcher;
            _mxEntityConfig = mxEntityConfig;
            _comparer = comparer;
            _logger = logger;
        }

        public void Handle(MxEntityState state, Message message)
        {
            string domainName = message.Id;

            if (message is MxRecordsPolled mxRecordsPolled)
            {
                List<HostMxRecord> recordsInMessage = mxRecordsPolled.Records ?? new List<HostMxRecord>();
                List<HostMxRecord> recordsInState = state.HostMxRecords ?? new List<HostMxRecord>();
                
                List<HostMxRecord> added = recordsInMessage.Except(recordsInState, _comparer).ToList();
                List<HostMxRecord> removed = recordsInState.Except(recordsInMessage, _comparer).ToList();

                bool hasAddedRecords = added.Any();
                bool hasRemovedRecords = removed.Any();

                if (hasAddedRecords)
                {
                    MxRecordAdded mxRecordAdded = new MxRecordAdded(domainName, added.ToList());
                    _dispatcher.Dispatch(mxRecordAdded, _mxEntityConfig.SnsTopicArn);
                }

                if (hasRemovedRecords)
                {
                    MxRecordRemoved mxRecordRemoved = new MxRecordRemoved(domainName, removed.ToList());
                    _dispatcher.Dispatch(mxRecordRemoved, _mxEntityConfig.SnsTopicArn);
                }

                if (hasAddedRecords || hasRemovedRecords)
                {
                    _logger.LogInformation($"recordsInMessage: {JsonConvert.SerializeObject(recordsInMessage)} for domain: {domainName}");
                    _logger.LogInformation($"recordsInState: {JsonConvert.SerializeObject(recordsInState)} for domain: {domainName}");

                    if (hasAddedRecords)
                    {
                        _logger.LogInformation($"added records: {JsonConvert.SerializeObject(added)} for domain: {domainName}");
                    }

                    if (hasRemovedRecords)
                    {
                        _logger.LogInformation($"removed records: {JsonConvert.SerializeObject(removed)} for domain: {domainName}");
                    }
                }
            }
        }
    }
}