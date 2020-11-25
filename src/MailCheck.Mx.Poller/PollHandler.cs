using System.Threading.Tasks;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Poller.Config;
using MailCheck.Mx.Poller.Domain;
using MailCheck.Mx.Poller.Mappings;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.Poller
{
    public class PollHandler : IHandle<MxPollPending>
    {
        private readonly IMxProcessor _processor;
        private readonly IMessageDispatcher _dispatcher;
        private readonly IMxPollerConfig _config;
        private readonly ILogger<PollHandler> _log;

        public PollHandler(IMxProcessor processor,
            IMessageDispatcher dispatcher,
            IMxPollerConfig config,
            ILogger<PollHandler> log)
        {
            _processor = processor;
            _dispatcher = dispatcher;
            _config = config;
            _log = log;
        }

        public async Task Handle(MxPollPending message)
        {
            try
            {
                MxPollResult dmarcPollResult = await _processor.Process(message.Id);

                _log.LogInformation("Polled MX records for {Domain}", message.Id);

                MxRecordsPolled mxRecordsPolled = dmarcPollResult.ToMxRecordsPolled();

                _dispatcher.Dispatch(mxRecordsPolled, _config.SnsTopicArn);

                _log.LogInformation("Published MX records for {Domain}", message.Id);
            }
            catch (System.Exception ex)
            {
                _log.LogError(ex, $"Error occurred polling domain {message.Id}");
                throw;
            }
        }
    }
}
