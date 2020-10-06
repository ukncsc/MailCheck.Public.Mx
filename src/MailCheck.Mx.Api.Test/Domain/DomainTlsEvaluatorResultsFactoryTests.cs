using System.Collections.Generic;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
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
    }
}