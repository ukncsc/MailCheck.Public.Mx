using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Tests;

namespace MailCheck.Mx.SimplifiedTlsTester.Rules
{
    public class Tls12ServerPreferenceRule : ITlsRule
    {
        public TestCriteria TestCriteria { get; } = new TestCriteria
        {
            Name = nameof(Tls12ServerPreferenceRule),
            Protocol = TlsVersion.TlsV12,
            CipherSuites = SupportedCiphers.Tls12CiphersAscendingPreference
        };

        public LinkedListNode<ITlsRule> Evaluate(TestContext context, BouncyCastleTlsTestResult result)
        {
            if (result.TlsError == TlsError.TCP_CONNECTION_FAILED 
                || result.TlsError == TlsError.HOST_NOT_FOUND 
                || result.TlsError == TlsError.SESSION_INITIALIZATION_FAILED)
            {
                context.Inconclusive = true;
                return null;
            }

            if (Array.IndexOf(Recommendations.GoodForTls12CipherSuites, result.CipherSuite) == -1)
            {
                context.Advisories.Add(Advisories.A2);
            }
            else
            {
                context.Advisories.Add(Advisories.P1);
            }

            return context.NextTest;
        }
    }
}