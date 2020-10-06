using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Poller;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MailCheck.Mx.Poller.Dns
{
    public interface IDnsClient
    {
        Task<DnsResult<List<HostMxRecord>>> GetMxRecords(string domain);
    }

    public class DnsClient : IDnsClient
    {
        private readonly ILookupClient _lookupClient;
        private readonly ILogger<IDnsClient> _log;

        public DnsClient(ILookupClient lookupClient, ILogger<IDnsClient> log)
        {
            _lookupClient = lookupClient;
            _log = log;
        }

        public async Task<DnsResult<List<HostMxRecord>>> GetMxRecords(string domain)
        {
            _log.LogInformation($"Querying mx records for {domain}");

            IDnsQueryResponse response = await _lookupClient.QueryAsync(domain, QueryType.MX);

            if (response.HasError)
            {
                _log.LogInformation($"Error occured quering mx records for {domain}, error: {response.ErrorMessage}");
                return new DnsResult<List<HostMxRecord>>(response.ErrorMessage);
            }

            List<HostMxRecord> records = await GetRecords(domain, response);

            _log.LogInformation($"MX records for {domain}, results: {JsonConvert.SerializeObject(records)}");


            return new DnsResult<List<HostMxRecord>>(records, response.MessageSize);
        }

        private async Task<List<string>> GetARecords(string domain, string host)
        {
            List<string> ipAddresses = new List<string>();

            IDnsQueryResponse response = await _lookupClient.QueryAsync(host, QueryType.A);

            if (!response.HasError)
            {
                ipAddresses.AddRange(response.Answers
                    .OfType<ARecord>()
                    .Select(_ => _.Address.ToString().Escape())
                    .ToList());
            }

            return ipAddresses;
        }

        private async Task<List<HostMxRecord>> GetRecords(string domain, IDnsQueryResponse response)
        {
            List<Task<HostMxRecord>> records = response.Answers.OfType<MxRecord>()
                .Select(async _ => new HostMxRecord(_.Exchange.Value.Escape(), _.Preference,
                    await GetARecords(domain, _.Exchange.Value.Escape())))
                .ToList();

            await Task.WhenAll(records);

            return records.Any()
                ? records.Select(x => x.Result).ToList()
                : new List<HostMxRecord>();
        }
    }
}
