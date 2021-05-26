using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using NUnit.Framework;


namespace MailCheck.Mx.Api.Test.Domain
{
    [TestFixture]
    public class DomainTlsEvaluatorResultsFactoryTests
    {
        private DomainTlsEvaluatorResultsFactory _domainTlsEvaluatorResultsFactory;

        [Test]
        public void CaseMismatchDoesNotBreakResultsConstruction()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();
            
            MxEntityState mxState = new MxEntityState("exampledomain.co.uk");
            mxState.HostMxRecords = new List<HostMxRecord>() {new HostMxRecord("MAILHOST.GOOGLE.COM.",1, new List<string>{""})};

            Dictionary<string, TlsEntityState> tlsEntityStates = new Dictionary<string, TlsEntityState>
            {
                ["mailhost.google.com."] = new TlsEntityState("Mailhost.google.com")
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults = _domainTlsEvaluatorResultsFactory.Create(mxState, tlsEntityStates);
            Assert.AreEqual("exampledomain.co.uk", domainTlsEvaluatorResults.Id);
            Assert.AreEqual(1, domainTlsEvaluatorResults.MxTlsEvaluatorResults.Count);
            Assert.AreEqual("mailhost.google.com.", domainTlsEvaluatorResults.MxTlsEvaluatorResults[0].Hostname.ToLower());
        }

        [Test]
        public void PositiviesMessagesAddedToModel()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            string description = "TLS 1.0 is available and a secure cipher suite was selected.";

            MxEntityState mxState = new MxEntityState("exampledomain.co.uk");
            mxState.HostMxRecords = new List<HostMxRecord>()
                {new HostMxRecord("MAILHOST.GOOGLE.COM.", 1, new List<string> {""})};

            Dictionary<string, TlsEntityState> tlsEntityStates = new Dictionary<string, TlsEntityState>
            {
                ["mailhost.google.com."] = new TlsEntityState("Mailhost.google.com")
            };

            List<TlsRecord> passRecords = Enumerable.Range(0, 15)
                .Select(i => new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS))
                .Select(result => new TlsRecord(result))
                .ToList();

            tlsEntityStates["mailhost.google.com."].TlsRecords = new TlsRecords(
                new TlsRecord(new TlsEvaluatedResult(Guid.NewGuid(), EvaluatorResult.PASS, description)),
                passRecords[1],
                passRecords[2],
                passRecords[3],
                passRecords[4],
                passRecords[5],
                passRecords[6],
                passRecords[7],
                passRecords[8],
                passRecords[9],
                passRecords[10],
                passRecords[11],
                passRecords[12],
                passRecords[13],
                passRecords[14]
            );

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create(mxState, tlsEntityStates);
            Assert.AreEqual(1, domainTlsEvaluatorResults.MxTlsEvaluatorResults.First().Positives.Count);
            Assert.AreEqual(description, domainTlsEvaluatorResults.MxTlsEvaluatorResults.First().Positives.First());
        }
    }
}