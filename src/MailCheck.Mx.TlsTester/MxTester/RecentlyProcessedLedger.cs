using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MailCheck.Common.Util;
using MailCheck.Mx.TlsTester.Config;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface IRecentlyProcessedLedger
    {
        bool Contains(string host);
        void Set(string host);
    }

    public class RecentlyProcessedLedger : IRecentlyProcessedLedger
    {
        private readonly IClock _clock;
        private readonly ILogger<RecentlyProcessedLedger> _log;

        private readonly ConcurrentDictionary<string, DateTime> _ledgerItems = new ConcurrentDictionary<string, DateTime>();
        private readonly TimeSpan _retestPeriod;

        public RecentlyProcessedLedger(IClock clock, IMxTesterConfig mxTesterConfig, ILogger<RecentlyProcessedLedger> log)
        {
            _clock = clock;
            _log = log;
            _retestPeriod = TimeSpan.FromSeconds(mxTesterConfig.TlsTesterHostRetestPeriodSeconds);
        }

        public bool Contains(string host)
        {
            _log.LogDebug($"Searching ledger for host: {host}");

            DateTime now = _clock.GetDateTimeUtc();

            if (_ledgerItems.TryGetValue(host, out DateTime value) && value > now)
            {
                _log.LogDebug($"Host {host} found in ledger with TTL of {(_ledgerItems[host] - now).TotalSeconds} seconds");
                return true;
            }

            _log.LogDebug($"Host {host} not found in ledger");
            return false;
        }

        public void Set(string host)
        {
            _log.LogDebug($"Setting {host} in ledger to expire in {_retestPeriod.TotalSeconds} seconds");
            _ledgerItems[host] = _clock.GetDateTimeUtc() + _retestPeriod;
        }
    }
}
