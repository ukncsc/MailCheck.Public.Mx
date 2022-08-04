using System;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Contracts.Advisories;
using Microsoft.Extensions.Logging;
using MailCheck.Common.Contracts.Messaging;
using NUnit.Framework;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEntity;
using MailCheck.Mx.Contracts.TlsEvaluator;
using MailCheck.Mx.TlsEntity.Config;
using MailCheck.Mx.TlsEntity.Entity.EmailSecurity;

namespace MailCheck.Mx.TlsEntity.Test.Entity.EmailSecurity
{
    [TestFixture]
    public class EntityChangedPublisherTest
    {
        private ITlsEntityConfig _config;
        private IMessageDispatcher _dispatcher;
        private ILogger<EntityChangedPublisher> _log;
        private EntityChangedPublisher _entityChangedPublisher;

        [SetUp]
        public void SetUp()
        {
            _config = A.Fake<ITlsEntityConfig>();
            _dispatcher = A.Fake<IMessageDispatcher>();
            _log = A.Fake<ILogger<EntityChangedPublisher>>();

            _entityChangedPublisher = new EntityChangedPublisher(_config, _dispatcher, _log);
        }

        [Test]
        public void ShouldDispatchEntityChanged()
        {
            TlsEntityState state = new TlsEntityState("test.gov.uk")
            {
                TlsState = TlsState.Evaluated,
                CertificateResults = new CertificateResults(new List<Certificate>(), new List<Error>()),
                FailureCount = 0,
                LastUpdated = DateTime.UtcNow,
                TlsRecords = new TlsRecords(null)
            };
            A.CallTo(() => _config.RecordType).Returns("TLS");

            _entityChangedPublisher.Publish("test.gov.uk", state, nameof(TlsResultsEvaluated));

            A.CallTo(() => _dispatcher.Dispatch(A<EntityChanged>.That.Matches(_ =>
                _.Id == "test.gov.uk" &&
                _.RecordType == "TLS" &&
                _.ReasonForChange == "TlsResultsEvaluated" &&
                ((TlsEntityState)_.NewEntityDetail).CertificateResults.Certificates.Count == 0), A<string>._)
            ).MustHaveHappenedOnceExactly();
        }
    }
}
