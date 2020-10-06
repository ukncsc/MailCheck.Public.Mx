using FakeItEasy;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MailCheck.Mx.Entity.Dao
{
    [TestFixture]
    public class MxEntityDaoTests
    {
        [Test]
        public async Task MxLookupFailed_EnsureSqlAndParametersBuiltCorrectly()
        {
            var fakeConnectionInfo = A.Fake<IConnectionInfoAsync>();

            string capturedSql = null;
            IDictionary<string, object> capturedParameters = null;

            var fakeLogger = A.Fake<ILogger<MxEntityDao>>();
            var fakeSaveOperation = A.Fake<Func<string, IDictionary<string, object>, Task<int>>>();

            A.CallTo(() => fakeSaveOperation.Invoke(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .Invokes((string sql, IDictionary<string, object> parameters) =>
                {
                    capturedSql = sql;
                    capturedParameters = parameters;
                })
                .Returns(Task.FromResult(10));

            var dao = new MxEntityDao(
                fakeConnectionInfo,
                fakeLogger,
                fakeSaveOperation
            );

            var mxEntityState = new MxEntityState("google.com")
            {
                MxState = MxState.Created,
                LastUpdated = new DateTime(2020, 3, 4, 5, 6, 7),
                Error = new Message(Guid.Empty, "DNS", MessageType.error, "DNS lookup failed", "DNS lookup failed"),
                HostMxRecords = null
            };

            var expectedParameters = new Dictionary<string, object>
            {
                ["domain"] = "com.google",
                ["mxState"] = MxState.Created,
                ["error"] = @"{""Id"":""00000000-0000-0000-0000-000000000000"",""Source"":""DNS"",""MessageType"":2,""Text"":""DNS lookup failed"",""MarkDown"":""DNS lookup failed"",""MessageDisplay"":0}",
                ["lastUpdated"] = new DateTime(2020, 3, 4, 5, 6, 7)
            };

            await dao.Save(mxEntityState);

            A.CallTo(() => fakeSaveOperation(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .MustHaveHappenedOnceExactly();

            Assert.That(capturedSql, Is.EqualTo(ExpectedDnsErrorSql));
            Assert.That(capturedParameters, Is.EquivalentTo(expectedParameters));
        }

        [Test]
        public async Task NoMxRecords_EnsureSqlAndParametersBuiltCorrectly()
        {
            var fakeConnectionInfo = A.Fake<IConnectionInfoAsync>();

            string capturedSql = null;
            IDictionary<string, object> capturedParameters = null;

            var fakeLogger = A.Fake<ILogger<MxEntityDao>>();
            var fakeSaveOperation = A.Fake<Func<string, IDictionary<string, object>, Task<int>>>();

            A.CallTo(() => fakeSaveOperation.Invoke(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .Invokes((string sql, IDictionary<string, object> parameters) =>
                {
                    capturedSql = sql;
                    capturedParameters = parameters;
                })
                .Returns(Task.FromResult(10));

            var dao = new MxEntityDao(
                fakeConnectionInfo,
                fakeLogger,
                fakeSaveOperation
            );

            var mxEntityState = new MxEntityState("google.com")
            {
                MxState = MxState.Created,
                LastUpdated = new DateTime(2020, 3, 4, 5, 6, 7),
                HostMxRecords = new List<Contracts.Poller.HostMxRecord>()
            };

            var expectedParameters = new Dictionary<string, object>
            {
                ["domain"] = "com.google",
                ["mxState"] = MxState.Created,
                ["error"] = "null",
                ["lastUpdated"] = new DateTime(2020, 3, 4, 5, 6, 7)
            };

            await dao.Save(mxEntityState);

            A.CallTo(() => fakeSaveOperation(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .MustHaveHappenedOnceExactly();

            Assert.That(capturedSql, Is.EqualTo(ExpectedNoMxSql));
            Assert.That(capturedParameters, Is.EquivalentTo(expectedParameters));
        }

        [Test]
        public async Task SomeMxRecords_EnsureSqlAndParametersBuiltCorrectly()
        {
            var fakeConnectionInfo = A.Fake<IConnectionInfoAsync>();

            string capturedSql = null;
            IDictionary<string, object> capturedParameters = null;

            var fakeLogger = A.Fake<ILogger<MxEntityDao>>();
            var fakeSaveOperation = A.Fake<Func<string, IDictionary<string, object>, Task<int>>>();

            A.CallTo(() => fakeSaveOperation.Invoke(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .Invokes((string sql, IDictionary<string, object> parameters) =>
                {
                    capturedSql = sql;
                    capturedParameters = parameters;
                })
                .Returns(Task.FromResult(10));

            var dao = new MxEntityDao(
                fakeConnectionInfo,
                fakeLogger,
                fakeSaveOperation
            );

            var mxEntityState = new MxEntityState("google.com")
            {
                MxState = MxState.Created,
                LastUpdated = new DateTime(2020, 3, 4, 5, 6, 7),
                HostMxRecords = new List<Contracts.Poller.HostMxRecord>
                {
                    new Contracts.Poller.HostMxRecord("mx1.com.", 1, new List<string>{"1.1.1.1"}),
                    new Contracts.Poller.HostMxRecord("mx2.com.", 2, new List<string>{"1.1.2.1", "1.1.2.2"}),
                    new Contracts.Poller.HostMxRecord("mx3.com.", 3, new List<string>{"1.1.3.1", "1.1.3.2", "1.1.3.3"})
                }
            };

            var expectedParameters = new Dictionary<string, object>
            {
                ["domain"] = "com.google",
                ["mxState"] = MxState.Created,
                ["error"] = "null",
                ["lastUpdated"] = new DateTime(2020, 3, 4, 5, 6, 7),
                ["domain_0"] = "com.google",
                ["hostname_0"] = ".com.mx1",
                ["hostMxRecord_0"] = @"{""Preference"":1,""IpAddresses"":[""1.1.1.1""],""Id"":""mx1.com."",""CorrelationId"":null,""CausationId"":null,""Type"":null,""MessageId"":null,""Timestamp"":""0001-01-01T00:00:00""}",
                ["lastUpdated_0"] = new DateTime(2020, 3, 4, 5, 6, 7),
                ["preference_0"] = 1,
                ["domain_1"] = "com.google",
                ["hostname_1"] = ".com.mx2",
                ["hostMxRecord_1"] = @"{""Preference"":2,""IpAddresses"":[""1.1.2.1"",""1.1.2.2""],""Id"":""mx2.com."",""CorrelationId"":null,""CausationId"":null,""Type"":null,""MessageId"":null,""Timestamp"":""0001-01-01T00:00:00""}",
                ["lastUpdated_1"] = new DateTime(2020, 3, 4, 5, 6, 7),
                ["preference_1"] = 2,
                ["domain_2"] = "com.google",
                ["hostname_2"] = ".com.mx3",
                ["hostMxRecord_2"] = @"{""Preference"":3,""IpAddresses"":[""1.1.3.1"",""1.1.3.2"",""1.1.3.3""],""Id"":""mx3.com."",""CorrelationId"":null,""CausationId"":null,""Type"":null,""MessageId"":null,""Timestamp"":""0001-01-01T00:00:00""}",
                ["lastUpdated_2"] = new DateTime(2020, 3, 4, 5, 6, 7),
                ["preference_2"] = 3
            };

            await dao.Save(mxEntityState);

            A.CallTo(() => fakeSaveOperation(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .MustHaveHappenedOnceExactly();

            Assert.That(capturedSql, Is.EqualTo(ExpectedMxPresentSql));
            Assert.That(capturedParameters, Is.EquivalentTo(expectedParameters));
        }

        [Test]
        public async Task DeleteMxHosts_EnsureSqlAndParametersBuiltCorrectly()
        {
            var fakeConnectionInfo = A.Fake<IConnectionInfoAsync>();

            string capturedSql = null;
            IDictionary<string, object> capturedParameters = null;

            var fakeLogger = A.Fake<ILogger<MxEntityDao>>();
            var fakeSaveOperation = A.Fake<Func<string, IDictionary<string, object>, Task<int>>>();

            A.CallTo(() => fakeSaveOperation.Invoke(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .Invokes((string sql, IDictionary<string, object> parameters) =>
                {
                    capturedSql = sql;
                    capturedParameters = parameters;
                })
                .Returns(Task.FromResult(10));

            var dao = new MxEntityDao(
                fakeConnectionInfo,
                fakeLogger,
                fakeSaveOperation
            );

            List<string> hostnames = new List<string>{"mx1.com.", "mx2.com.", "mx3.com."};

            var expectedParameters = new Dictionary<string, object>
            {
                ["a0"] = ".com.mx1",
                ["a1"] = ".com.mx2",
                ["a2"] = ".com.mx3"
            };
            
            await dao.DeleteHosts(hostnames);

            A.CallTo(() => fakeSaveOperation(A<string>.Ignored, A<IDictionary<string, object>>.Ignored))
                .MustHaveHappenedOnceExactly();

            Assert.That(capturedSql, Is.EqualTo(ExpectedDeleteMxHostsSql));
            Assert.That(capturedParameters, Is.EquivalentTo(expectedParameters));
        }

        private static readonly string ExpectedDnsErrorSql = @"
INSERT INTO `mx`.`Domain`
(`domain`,
`mxState`,
`lastUpdated`,
`error`)
VALUES
(@domain,
@mxState,
@lastUpdated,
@error)
ON DUPLICATE KEY UPDATE
`mxState` = @mxState,
`lastUpdated` = COALESCE(@lastUpdated, lastUpdated),
`error` = COALESCE(@error, error);
    ";

        private static readonly string ExpectedNoMxSql = @"
INSERT INTO `mx`.`Domain`
(`domain`,
`mxState`,
`lastUpdated`,
`error`)
VALUES
(@domain,
@mxState,
@lastUpdated,
@error)
ON DUPLICATE KEY UPDATE
`mxState` = @mxState,
`lastUpdated` = COALESCE(@lastUpdated, lastUpdated),
`error` = COALESCE(@error, error);
    
DELETE FROM `mx`.`MxRecord`
WHERE domain = @domain;
    ";

        private static readonly string ExpectedMxPresentSql = @"
INSERT INTO `mx`.`Domain`
(`domain`,
`mxState`,
`lastUpdated`,
`error`)
VALUES
(@domain,
@mxState,
@lastUpdated,
@error)
ON DUPLICATE KEY UPDATE
`mxState` = @mxState,
`lastUpdated` = COALESCE(@lastUpdated, lastUpdated),
`error` = COALESCE(@error, error);
    
DELETE FROM `mx`.`MxRecord`
WHERE domain = @domain;
    
INSERT INTO `mx`.`MxHost`
(hostname,
hostMxRecord,
lastUpdated)
VALUES
( @hostname_0, @hostMxRecord_0, @lastUpdated_0 ), ( @hostname_1, @hostMxRecord_1, @lastUpdated_1 ), ( @hostname_2, @hostMxRecord_2, @lastUpdated_2 )
ON DUPLICATE KEY UPDATE
hostMxRecord = VALUES(hostMxRecord),
lastUpdated = VALUES(lastUpdated);
    
INSERT INTO `mx`.`MxRecord`
(domain,
hostname,
preference)
VALUES
( @domain_0, @hostname_0, @preference_0 ), ( @domain_1, @hostname_1, @preference_1 ), ( @domain_2, @hostname_2, @preference_2 )
ON DUPLICATE KEY UPDATE
preference = VALUES(preference);
    ";

    private static readonly string ExpectedDeleteMxHostsSql = @"
DELETE FROM `mx`.`MxHost`
WHERE hostname IN (@a0,@a1,@a2);";
    }
}
