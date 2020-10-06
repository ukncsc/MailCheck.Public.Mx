using System;
using System.Collections.Concurrent;
using System.Linq;
using MailCheck.Mx.TlsTester.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface IMxSecurityProcessingFilter
    {
        TlsTestPending ApplyFilter(TlsTestPending testPending);
        void RemoveFilter(string host);
        int HostCount { get; }
    }

    public class MxSecurityProcessingFilter : IMxSecurityProcessingFilter
    {
        private readonly ILogger<MxSecurityProcessingFilter> _log;
        private readonly ConcurrentDictionary<string, string> _filterItems = new ConcurrentDictionary<string, string>();

        public MxSecurityProcessingFilter(ILogger<MxSecurityProcessingFilter> log)
        {
            _log = log;
        }

        public TlsTestPending ApplyFilter(TlsTestPending tlsTestPending)
        {
            bool filter = Filter(tlsTestPending.Id);

            if (filter)
            {
                _log.LogDebug($"Filtered {tlsTestPending.Id}");
            }
            else
            {
                _filterItems.AddOrUpdate(tlsTestPending.Id, tlsTestPending.Id, (s, o) => tlsTestPending.Id);
                _log.LogDebug($"Added host {tlsTestPending.Id} to filter.");
            }

            return filter
                ? null
                : tlsTestPending;
        }

        public void RemoveFilter(string host)
        {
            _filterItems.TryRemove(host, out string value);
            _log.LogDebug($"Removed host: {host} from filter");
        }

        private bool Filter(string host)
        {
            return _filterItems.TryGetValue(host, out _);
        }

        public int HostCount => _filterItems.Distinct().Count();
    }
}
