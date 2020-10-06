using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Poller.Config;
using MailCheck.Mx.Poller.Dns;
using MailCheck.Mx.Poller.Domain;
using MailCheck.Mx.Poller.Exception;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.Poller
{
    public interface IMxProcessor
    {
        Task<MxPollResult> Process(string domain);
    }

    public class MxProcessor : IMxProcessor
    {
        private readonly IDnsClient _dnsClient;
        private readonly IMxPollerConfig _config;
        private readonly ILogger<MxProcessor> _log;

        public MxProcessor(IDnsClient dnsClient,
            IMxPollerConfig config,
            ILogger<MxProcessor> log)
        {
            _dnsClient = dnsClient;
            _config = config;
            _log = log;
        }

        public Guid Id => Guid.Parse("8EA38A56-DCFB-4A28-B632-85C9CF0CD27C");

        public async Task<MxPollResult> Process(string domain)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            DnsResult<List<HostMxRecord>> mxHosts = await _dnsClient.GetMxRecords(domain);

            stopwatch.Stop();

            if (!_config.AllowNullResults && (mxHosts.IsErrored ||
                                              mxHosts.Value.TrueForAll(x => string.IsNullOrWhiteSpace(x.Id))))
            {
                throw new MxPollerException($"Unable to retrieve mx hosts for {domain}.");
            }

            if (mxHosts.IsErrored)
            {
                return new MxPollResult(domain,
                    new Error(Id, ErrorType.Error,
                        $"Failed MX hosts query for {domain} with error {mxHosts.Error}", string.Empty));
            }

            if (mxHosts.IsErrored)
            {
                _log.LogError($"Processed request for MX records in {stopwatch.ElapsedMilliseconds} for domain: {domain}");
            }

            return new MxPollResult(domain, mxHosts.Value, stopwatch.Elapsed);
        }
    }
}
