using System;
using System.Collections.Generic;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using MailCheck.Mx.SimplifiedTlsTester.Tests;

namespace MailCheck.Mx.SimplifiedTlsTester.Rules
{
    public class Tls13Rule : ITlsRule
    {
        public TestCriteria TestCriteria { get; } = new TestCriteria
        {
            Name = nameof(Tls13Rule),
            Protocol = TlsVersion.TlsV13,
            CipherSuites = SupportedCiphers.Tls13Ciphers
        };

        public LinkedListNode<ITlsRule> Evaluate(TestContext context, BouncyCastleTlsTestResult result)
        {
            if (result.TlsError == TlsError.TCP_CONNECTION_FAILED)
            {
                context.Advisories.Add(Advisories.A3);
                return null;
            }

            if (result.TlsError == TlsError.SESSION_INITIALIZATION_FAILED)
            {
                var outcome = ((SimplifiedStartTlsResult)result.SessionInitialisationResult).Outcome;

                switch (outcome)
                {
                    case Outcome.StartTlsNotSupported:
                        context.Advisories.Add(Advisories.U1);
                        return null;
                    default:
                        context.Inconclusive = true;
                        return null;
                }
            }

            if (result.TlsError == null && result.Version == TlsVersion.TlsV13 && Array.IndexOf(Recommendations.GoodForTls13CipherSuites, result.CipherSuite) != -1)
            {
                context.Advisories.Add(Advisories.P2);
            }
            else
            {
                context.Advisories.Add(Advisories.I1);
            }

            return context.NextTest;
        }
    }
}