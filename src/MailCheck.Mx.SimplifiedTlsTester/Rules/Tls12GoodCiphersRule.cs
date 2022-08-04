using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using MailCheck.Mx.SimplifiedTlsTester.Tests;

namespace MailCheck.Mx.SimplifiedTlsTester.Rules
{
    public class Tls12GoodCiphersRule : ITlsRule
    {
        public TestCriteria TestCriteria { get; } = new TestCriteria
        {
            Name = nameof(Tls12GoodCiphersRule),
            Protocol = TlsVersion.TlsV12,
            CipherSuites = SupportedCiphers.Tls12CiphersDescendingPreference
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

            if (result.TlsError != null)
            {
                context.Advisories.Add(Advisories.U2);
                return null;
            }

            if (Array.IndexOf(Recommendations.GoodForTls12CipherSuites, result.CipherSuite) == -1)
            {
                context.Advisories.Add(Advisories.A1);
                return null;
            }

            return context.NextTest;
        }
    }
}