using System;
using System.Collections.Generic;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.TlsEntity.Dao;
using NUnit.Framework;

namespace MailCheck.Mx.TlsEntity.Test.Dao
{
    [TestFixture]
    public class SimplifiedTlsEntityDaoTests
    {
        [Test]
        public void CalculateStatuses_NullResult_Omitted()
        {
            var rows = new[]
            {
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah",
                    Hostname = ".com.example.mx1",
                    Statuses = null
                },
            };

            var actual = SimplifiedTlsEntityDao.CalculateStatuses(rows);

            var expected = new Dictionary<string, Status>();

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test]
        public void CalculateStatuses_EmptyResult_Success()
        {
            var rows = new[]
            {
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah",
                    Hostname = ".com.example.mx1",
                    Statuses = @"[]"
                },
            };
            var actual = SimplifiedTlsEntityDao.CalculateStatuses(rows);

            var expected = new Dictionary<string, Status>
            {
                ["blah.com"] = Status.Success
            };

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(@"[""success""]", Status.Success)]
        [TestCase(@"[""info""]", Status.Info)]
        [TestCase(@"[""warning""]", Status.Warning)]
        [TestCase(@"[""error""]", Status.Error)]
        [TestCase(@"[""success"", ""info""]", Status.Info)]
        [TestCase(@"[""success"", ""info"", ""warning""]", Status.Warning)]
        [TestCase(@"[""success"", ""info"", ""warning"", ""error""]", Status.Error)]
        public void CalculateStatuses_Result_AsExpected(string statusJson, Status expectedStatus)
        {
            var rows = new[]
            {
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah",
                    Hostname = ".com.example.mx1",
                    Statuses = statusJson
                },
            };
            var actual = SimplifiedTlsEntityDao.CalculateStatuses(rows);

            var expected = new Dictionary<string, Status>
            {
                ["blah.com"] = expectedStatus
            };

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(@"[""success""]", null, Status.Success)]
        [TestCase(@"[""success"", ""info""]", null, Status.Info)]
        [TestCase(@"[]", null, Status.Success)]
        [TestCase(@"[""info""]", @"[]", Status.Info)]
        [TestCase(@"[""success"", ""info""]", @"[""warning""]", Status.Warning)]
        [TestCase(@"[""success"", ""info""]", @"[""warning"", ""error""]", Status.Error)]
        public void CalculateStatuses_MultiResult_GroupedAsExpected(string statusJson1, string statusJson2, Status expectedStatus)
        {
            var rows = new[]
            {
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah",
                    Hostname = ".com.example.mx1",
                    Statuses = statusJson1
                },
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah",
                    Hostname = ".com.example.mx2",
                    Statuses = statusJson2
                },
            };
            var actual = SimplifiedTlsEntityDao.CalculateStatuses(rows);

            var expected = new Dictionary<string, Status>
            {
                ["blah.com"] = expectedStatus
            };

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCase(@"[]", @"[]", Status.Success, Status.Success)]
        [TestCase(@"[""info""]", @"[]", Status.Info, Status.Success)]
        [TestCase(@"[""success"", ""info""]", @"[""warning""]", Status.Info, Status.Warning)]
        [TestCase(@"[""success"", ""info""]", @"[""warning"", ""error""]", Status.Info, Status.Error)]
        public void CalculateStatuses_MultiResult_MultiResultExpected(string statusJson1, string statusJson2, Status expectedStatus1, Status expectedStatus2)
        {
            var rows = new[]
            {
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah1",
                    Hostname = ".com.example.mx1",
                    Statuses = statusJson1
                },
                new SimplifiedTlsEntityDao.AdvisoryStatusContainer
                {
                    Domain = "com.blah2",
                    Hostname = ".com.example.mx2",
                    Statuses = statusJson2
                },
            };
            var actual = SimplifiedTlsEntityDao.CalculateStatuses(rows);

            var expected = new Dictionary<string, Status>
            {
                ["blah1.com"] = expectedStatus1,
                ["blah2.com"] = expectedStatus2,
            };

            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }
}
