﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface IMxSecurityProcessingFilter
    {
        bool Reserve(string host);
        void ReleaseReservation(string host);
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

        public bool Reserve(string host)
        {
            _log.LogInformation($"Attempting to add reservation for host: {host}");
            bool result = _filterItems.TryAdd(host, null);

            if (result)
            {
                _log.LogInformation($"Reservation added for host: {host}");
            }
            else
            {
                _log.LogInformation($"Reservation already held by another processor for host: {host}");
            }
            return result;
        }

        public void ReleaseReservation(string host)
        {
            _log.LogInformation($"Releasing reservation for host: {host}");
            _filterItems.TryRemove(host, out string value);
        }

        public int HostCount => _filterItems.Count;
    }
}
