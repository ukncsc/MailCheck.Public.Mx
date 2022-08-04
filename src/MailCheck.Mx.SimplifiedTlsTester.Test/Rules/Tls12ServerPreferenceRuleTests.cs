using System.Collections.Generic;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Rules;
using NUnit.Framework;
using TestContext = MailCheck.Mx.SimplifiedTlsTester.Domain.TestContext;

namespace MailCheck.Mx.SimplifiedTlsTester.Test.Rules
{
    [TestFixture]
    public class Tls12ServerPreferenceRuleTests
    {
        private Tls12ServerPreferenceRule _tls12ServerPreferenceRule;

        [SetUp]
        public void SetUp()
        {
            _tls12ServerPreferenceRule = new Tls12ServerPreferenceRule();
        }

        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384)]
        public void EvaluateCausesAdvisoryIfGoodCipherSelected(CipherSuite cipherSuite)
        {
            LinkedListNode<ITlsRule> nextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>());
            TestContext context = new TestContext
            {
                NextTest = nextTest
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, cipherSuite, null, null, null, null, null);

            LinkedListNode<ITlsRule> result = _tls12ServerPreferenceRule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.P1, context.Advisories[0]);
            Assert.AreSame(nextTest, result);
        }

        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_RC4_128_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_RC4_128_MD5)]
        [TestCase(CipherSuite.TLS_NULL_WITH_NULL_NULL)]
        [TestCase(CipherSuite.TLS_RSA_WITH_NULL_MD5)]
        [TestCase(CipherSuite.TLS_RSA_WITH_NULL_SHA)]
        [TestCase(CipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5)]
        [TestCase(CipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5)]
        [TestCase(CipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_RSA_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA)]
        [TestCase(CipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA)]
        public void EvaluateCausesAdvisoryIfBadCipherSelected(CipherSuite cipherSuite)
        {
            LinkedListNode<ITlsRule> nextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>());
            TestContext context = new TestContext
            {
                NextTest = nextTest
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, cipherSuite, null, null, null, null, null);

            LinkedListNode<ITlsRule> result = _tls12ServerPreferenceRule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.A2, context.Advisories[0]);
            Assert.AreSame(nextTest, result);
        }

        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.HOST_NOT_FOUND)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public void EvaluateMarksTestRunAsInconclusive(TlsError error)
        {
            TestContext context = new TestContext();

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, null, null, null, error, null, null);

            _tls12ServerPreferenceRule.Evaluate(context, bouncyCastleResult);

            Assert.True(context.Inconclusive);
        }
    }
}