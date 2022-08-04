using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.Api.Domain
{
    public interface IDomainTlsEvaluatorResultsFactory
    {
        DomainTlsEvaluatorResults CreatePending(string domainName);
        DomainTlsEvaluatorResults CreateNoTls(string domainName);
        DomainTlsEvaluatorResults CreateNoMx(string domainName);
        DomainTlsEvaluatorResults Create(string domainName, Dictionary<string, int> preferences,
            List<SimplifiedTlsEntityState> states);
    }

    public class DomainTlsEvaluatorResultsFactory : IDomainTlsEvaluatorResultsFactory
    {
        private static readonly List<string> EmptyDescriptionList = new List<string>();

        public DomainTlsEvaluatorResults CreatePending(string domainName)
        {
            return new DomainTlsEvaluatorResults(domainName, true, true);
        }

        public DomainTlsEvaluatorResults CreateNoTls(string domainName)
        {
            return new DomainTlsEvaluatorResults(domainName, false, true);
        }

        public DomainTlsEvaluatorResults CreateNoMx(string domainName)
        {
            return new DomainTlsEvaluatorResults(domainName, false, false);
        }

        public DomainTlsEvaluatorResults Create(string domainName, Dictionary<string, int> preferences, List<SimplifiedTlsEntityState> states)
        {
            List<MxTlsEvaluatorResults> mxTlsEvaluatorResults = new List<MxTlsEvaluatorResults>();
            List<MxTlsCertificateEvaluatorResults> mxTlsCertificateEvaluatorResults = new List<MxTlsCertificateEvaluatorResults>();
            List<IpState> allAssociatedIps = new List<IpState>();

            var statesGroupedByHostname = states.GroupBy(x => x.Hostname);

            Dictionary<string, Certificate> domainCertificates = new Dictionary<string, Certificate>(StringComparer.InvariantCultureIgnoreCase);

            foreach (IGrouping<string, SimplifiedTlsEntityState> stateGrouping in statesGroupedByHostname)
            {
                string hostname = stateGrouping.Key;
                preferences.TryGetValue(hostname, out int preference);

                DateTime certsLastUpdated = stateGrouping.Max(x => x.CertsLastUpdated ?? DateTime.MinValue);
                DateTime tlsLastUpdated = stateGrouping.Max(x => x.TlsLastUpdated ?? DateTime.MinValue);

                var tlsAdvisories = stateGrouping.SelectMany(x => x.TlsAdvisories ?? new List<NamedAdvisory>());
                var distinctAdvisories = tlsAdvisories.GroupBy(x => x.Id).Select(x => x.First()).ToList();
                var advisoriesByType = distinctAdvisories.GroupBy(x => x.MessageType)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Select(er => er.Text).ToList());

                advisoriesByType.TryGetValue(MessageType.error, out List<string> errors);
                advisoriesByType.TryGetValue(MessageType.warning, out List<string> warnings);
                advisoriesByType.TryGetValue(MessageType.info, out List<string> infos);
                advisoriesByType.TryGetValue(MessageType.success, out List<string> successes);

                List<IpState> associatedIpsForHost = stateGrouping.Select(x => new IpState(x.IpAddress, x.TlsLastUpdated, x.CertsLastUpdated)).ToList();
                allAssociatedIps.AddRange(associatedIpsForHost);

                MxTlsEvaluatorResults tlsEvaluatorResults = new MxTlsEvaluatorResults(
                    hostname,
                    preference,
                    tlsLastUpdated,
                    warnings ?? EmptyDescriptionList,
                    errors ?? EmptyDescriptionList,
                    infos ?? EmptyDescriptionList,
                    successes ?? EmptyDescriptionList,
                    associatedIpsForHost
                );

                mxTlsEvaluatorResults.Add(tlsEvaluatorResults);

                Dictionary<string, Certificate> hostCertificates = new Dictionary<string, Certificate>(StringComparer.InvariantCultureIgnoreCase);

                foreach (SimplifiedTlsEntityState state in stateGrouping)
                {
                    if (state.Certificates == null) continue;

                    foreach (KeyValuePair<string, string> kvp in state.Certificates)
                    {
                        if (domainCertificates.TryGetValue(kvp.Key, out Certificate existingCertificate))
                        {
                            hostCertificates.TryAdd(kvp.Key, existingCertificate);
                        }
                        else
                        {
                            Certificate certificate = GetCertificate(kvp.Value);
                            hostCertificates.TryAdd(kvp.Key, certificate);
                            domainCertificates.TryAdd(kvp.Key, certificate);
                        }
                    }
                }

                List<NamedAdvisory> certAdvisories = stateGrouping.SelectMany(x => x.CertAdvisories ?? new List<NamedAdvisory>()).ToList();
                List<NamedAdvisory> distinctCertAdvisories = certAdvisories.GroupBy(x => x.Id).Select(x => x.First()).ToList();

                string[] thumbprints = stateGrouping
                    .Where(x => x.SimplifiedTlsConnectionResults != null)
                    .SelectMany(x => x.SimplifiedTlsConnectionResults)
                    .Select(x => x.CertificateThumbprints)
                    .FirstOrDefault(thumbprint => thumbprint?.Length > 0);

                List<Certificate> orderedCertificates;
                if (thumbprints?.Length > 0)
                {
                    orderedCertificates = thumbprints
                        .Select(thumbprint => { domainCertificates.TryGetValue(thumbprint, out Certificate certificate); return certificate; })
                        .Where(certificate => certificate != null)
                        .ToList();
                }
                else
                {
                    orderedCertificates = hostCertificates.Values.ToList();
                }

                MxTlsCertificateEvaluatorResults tlsCertificateEvaluatorResults = new MxTlsCertificateEvaluatorResults(
                    hostname,
                    preference,
                    certsLastUpdated,
                    orderedCertificates.Count > 0 ? orderedCertificates : null,
                    GetErrors(distinctCertAdvisories),
                    associatedIpsForHost
                );

                mxTlsCertificateEvaluatorResults.Add(tlsCertificateEvaluatorResults);
            }

            allAssociatedIps = allAssociatedIps.Distinct(new IpStateComparer()).ToList();

            DomainTlsEvaluatorResults results = new DomainTlsEvaluatorResults(domainName, false, true, mxTlsEvaluatorResults, mxTlsCertificateEvaluatorResults, allAssociatedIps);

            return results;
        }

        private List<Error> GetErrors(List<NamedAdvisory> messages)
        {
            if (messages == null) return new List<Error>();

            IEnumerable<Error> warnings = messages.Where(x => x.MessageType == MessageType.warning)
                .Select(x => new Error(ErrorType.Warning, x.Text, x.MarkDown));
            IEnumerable<Error> errors = messages.Where(x => x.MessageType == MessageType.error)
                .Select(x => new Error(ErrorType.Error, x.Text, x.MarkDown));

            List<Error> result = new List<Error>(warnings);
            result.AddRange(errors);

            return result;
        }

        internal static Certificate GetCertificate(string rawCertificate)
        {
            byte[] bytes = Convert.FromBase64String(rawCertificate);

            X509Certificate2 raw = new X509Certificate2(bytes);

            Certificate certificate = new Certificate(
                raw.Thumbprint,
                raw.Issuer,
                raw.Subject,
                raw.NotBefore,
                raw.NotAfter,
                raw.PublicKey.Oid.FriendlyName,
                GetKeyLength(raw),
                raw.SerialNumber,
                raw.Version.ToString(),
                GetExtension(raw, "subject alternative name"),
                raw.GetNameInfo(X509NameType.SimpleName, false)
            );

            return certificate;
        }

        private static int GetKeyLength(X509Certificate2 x509Certificate2)
        {
            return x509Certificate2.PublicKey.Oid.FriendlyName == "RSA"
                ? x509Certificate2.GetRSAPublicKey().KeySize
                : x509Certificate2.GetECDsaPublicKey().KeySize;
        }

        private static string GetExtension(X509Certificate2 x509Certificate2, string extensionName)
        {
            return x509Certificate2.Extensions.Cast<X509Extension>()
                .FirstOrDefault(_ => (_.Oid.FriendlyName?.ToLower() ?? string.Empty).EndsWith(extensionName))
                ?.Format(false);
        }
    }
}