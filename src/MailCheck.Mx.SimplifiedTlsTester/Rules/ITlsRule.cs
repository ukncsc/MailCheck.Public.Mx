using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;

namespace MailCheck.Mx.SimplifiedTlsTester.Rules
{
    public interface ITlsRule
    {
        public TestCriteria TestCriteria { get; }

        LinkedListNode<ITlsRule> Evaluate(TestContext context, BouncyCastleTlsTestResult result);
    }
}