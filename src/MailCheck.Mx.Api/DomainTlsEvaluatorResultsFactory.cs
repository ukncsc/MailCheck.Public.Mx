using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Api
{
    public interface IDomainTlsEvaluatorResultsFactory
    {
        DomainTlsEvaluatorResults Create(MxEntityState mxState, Dictionary<string, TlsEntityState> tlsEntityStates);
        DomainTlsEvaluatorResults CreatePending(string domainName);
    }

    public class DomainTlsEvaluatorResultsFactory : IDomainTlsEvaluatorResultsFactory
    {
        public DomainTlsEvaluatorResults CreatePending(string domainName)
        {
            return new DomainTlsEvaluatorResults(domainName, true);
        }

        public DomainTlsEvaluatorResults Create(MxEntityState mxState,
            Dictionary<string, TlsEntityState> tlsEntityStates)
        {
            List<MxTlsEvaluatorResults> mxTlsEvaluatorResults = new List<MxTlsEvaluatorResults>();
            List<MxTlsCertificateEvaluatorResults> mxTlsCertificateEvaluatorResults =
                new List<MxTlsCertificateEvaluatorResults>();

            foreach (HostMxRecord hostMxRecord in mxState.HostMxRecords)
            { 
                // tlsEntityStates keys are always lowercase due to ReverseUrl() in MxApiDao.GetTlsEntityStates
                TlsEntityState tlsEntityState = tlsEntityStates[hostMxRecord.Id.ToLower()];

                List<TlsRecord> records = new List<TlsRecord>();

                TlsRecords tlsRecords = tlsEntityState.TlsRecords;

                if (tlsRecords != null)
                {
                    records.Add(tlsRecords.Tls12AvailableWithBestCipherSuiteSelected);
                    records.Add(tlsRecords.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList);
                    records.Add(tlsRecords.Tls12AvailableWithSha2HashFunctionSelected);
                    records.Add(tlsRecords.Tls12AvailableWithWeakCipherSuiteNotSelected);
                    records.Add(tlsRecords.Tls11AvailableWithBestCipherSuiteSelected);
                    records.Add(tlsRecords.Tls11AvailableWithWeakCipherSuiteNotSelected);
                    records.Add(tlsRecords.Tls10AvailableWithBestCipherSuiteSelected);
                    records.Add(tlsRecords.Tls10AvailableWithWeakCipherSuiteNotSelected);
                    records.Add(tlsRecords.Ssl3FailsWithBadCipherSuite);
                    records.Add(tlsRecords.TlsSecureEllipticCurveSelected);
                    records.Add(tlsRecords.TlsSecureDiffieHellmanGroupSelected);
                    records.Add(tlsRecords.TlsWeakCipherSuitesRejected);
                    records.Add(tlsRecords.Tls12Available);
                    records.Add(tlsRecords.Tls11Available);
                    records.Add(tlsRecords.Tls10Available);
                }

                List<string> warnings = records.Where(_ => _.TlsEvaluatedResult.Result == EvaluatorResult.WARNING)
                    .Select(_ => _.TlsEvaluatedResult.Description).ToList();
                List<string> failures = records.Where(_ => _.TlsEvaluatedResult.Result == EvaluatorResult.FAIL)
                    .Select(_ => _.TlsEvaluatedResult.Description).ToList();
                List<string> informational = records
                    .Where(_ => _.TlsEvaluatedResult.Result == EvaluatorResult.INFORMATIONAL ||
                                _.TlsEvaluatedResult.Result == EvaluatorResult.INCONCLUSIVE)
                    .Select(_ => _.TlsEvaluatedResult.Description).ToList();

                mxTlsEvaluatorResults.Add(
                    new MxTlsEvaluatorResults(
                        hostMxRecord.Id,
                        hostMxRecord.Preference ?? 0,
                        mxState.LastUpdated ?? DateTime.MinValue,
                        warnings,
                        failures,
                        informational));

                mxTlsCertificateEvaluatorResults.Add(CreateMxTlsCertificateEvaluatorResults(hostMxRecord.Id,
                    hostMxRecord.Preference ?? 0, mxState.LastUpdated ?? DateTime.MinValue, tlsEntityState.CertificateResults));
            }

            return new DomainTlsEvaluatorResults(mxState.Id, mxState.MxState == MxState.PollPending,
                mxTlsEvaluatorResults, mxTlsCertificateEvaluatorResults);
        }

        private MxTlsCertificateEvaluatorResults CreateMxTlsCertificateEvaluatorResults(string hostName, int preference, DateTime lastChecked,
            CertificateResults certificateResults)
        {
            return new MxTlsCertificateEvaluatorResults(hostName, preference, lastChecked, certificateResults?.Certificates,
                certificateResults?.Errors);
        }
    }
}