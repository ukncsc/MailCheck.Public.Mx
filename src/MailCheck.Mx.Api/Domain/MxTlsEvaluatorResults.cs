using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Api.Domain
{
    public class MxTlsEvaluatorResults
    {
        public MxTlsEvaluatorResults(string hostname, int preference, DateTime lastChecked, List<string> warnings,
            List<string> failures, List<string> informationals, List<string> positives, List<IpState> associatedIps)
        {
            Hostname = hostname;
            Preference = preference;
            LastChecked = lastChecked;
            Warnings = warnings;
            Failures = failures;
            Informationals = informationals;
            Positives = positives;
            AssociatedIps = associatedIps;
        }

        public List<string> Failures { get; }

        public string Hostname { get; }

        public int Preference { get; }

        public List<string> Informationals { get; }

        public List<string> Positives { get; }

        public DateTime LastChecked { get; }

        public List<string> Warnings { get; }

        public List<IpState> AssociatedIps { get; }
    }

    public class MxTlsCertificateEvaluatorResults
    {
        public MxTlsCertificateEvaluatorResults(string hostname, int preference, DateTime lastChecked, 
            List<Certificate> certificates, List<Error> errors, List<IpState> associatedIps)
        {
            HostName = hostname;
            Preference = preference;
            LastChecked = lastChecked;
            Certificates = certificates;
            Errors = errors;
            AssociatedIps = associatedIps;
        }

        public string HostName { get; }

        public int Preference { get; }

        public DateTime LastChecked { get; }

        public List<Certificate> Certificates { get; }

        public List<Error> Errors { get; }

        public List<IpState> AssociatedIps { get; }
    }
}