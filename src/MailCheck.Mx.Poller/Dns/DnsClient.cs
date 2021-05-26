using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using MailCheck.Common.Util;
using MailCheck.Mx.Contracts.Poller;
using Microsoft.Extensions.Logging;

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
            IDnsQueryResponse response = await _lookupClient.QueryAsync(domain, QueryType.MX);

            if (response.HasError)
            {
                return new DnsResult<List<HostMxRecord>>(response.ErrorMessage, response.AuditTrail);
            }

            List<HostMxRecord> records = await GetRecords(domain, response);

            return new DnsResult<List<HostMxRecord>>(records, response.MessageSize);
        }

        private async Task<List<string>> GetARecords(string domain, string host)
        {
            List<string> ipAddresses = new List<string>();

            IDnsQueryResponse response = await _lookupClient.QueryAsync(host, QueryType.A);

            if (response.HasError)
            {
                _log.LogWarning($"DNS A record lookup for host {host} (from MX for domain {domain}) failed with error {response.ErrorMessage}{Environment.NewLine}{response.AuditTrail}");
            }
            else
            {
                ipAddresses.AddRange(response.Answers
                    .OfType<ARecord>()
                    .Select(aRecord => aRecord.Address.ToString().Escape())
                    .ToList());
            }

            return ipAddresses;
        }

        private async Task<List<HostMxRecord>> GetRecords(string domain, IDnsQueryResponse response)
        {
            List<Task<HostMxRecord>> records = response.Answers
                .OfType<MxRecord>()
                .Select(async mxRecord => {
                    string mxEntry = mxRecord.Exchange.Value.Escape();

                    List<string> aRecords = await GetARecords(domain, mxEntry);

                    return new HostMxRecord(mxEntry, mxRecord.Preference, aRecords);
                })
                .ToList();

            return (await Task.WhenAll(records)).ToList();
        }
    }
}
