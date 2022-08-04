using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Rules;

namespace MailCheck.Mx.SimplifiedTlsTester.Domain
{
    public class TestContext
    {
        public IList<NamedAdvisory> Advisories { get; set; } = new List<NamedAdvisory>();
        public LinkedListNode<ITlsRule> CurrentTest { get; set; }
        public LinkedListNode<ITlsRule> NextTest { get; set; }
        public bool Inconclusive { get; set; }
        public bool HasPreviousFailure { get; set; }
    }
}