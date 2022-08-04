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
    public class Tls12GoodCiphersRuleTests
    {
        private Tls12GoodCiphersRule _tls12GoodCiphersRule;

        [SetUp]
        public void SetUp()
        {
            _tls12GoodCiphersRule = new Tls12GoodCiphersRule();
        }

        [TestCase(TlsError.CLOSE_NOTIFY)]
        [TestCase(TlsError.UNEXPECTED_MESSAGE)]
        [TestCase(TlsError.BAD_RECORD_MAC)]
        [TestCase(TlsError.DECRYPTION_FAILED)]
        [TestCase(TlsError.RECORD_OVERFLOW)]
        [TestCase(TlsError.DECOMPRESSION_FAILURE)]
        [TestCase(TlsError.HANDSHAKE_FAILURE)]
        [TestCase(TlsError.NO_CERTIFICATE)]
        [TestCase(TlsError.BAD_CERTIFICATE)]
        [TestCase(TlsError.UNSUPPORTED_CERTIFICATE)]
        [TestCase(TlsError.CERTIFICATE_REVOKED)]
        [TestCase(TlsError.CERTIFICATE_EXPIRED)]
        [TestCase(TlsError.CERTIFICATE_UNKNOWN)]
        [TestCase(TlsError.ILLEGAL_PARAMETER)]
        [TestCase(TlsError.UNKNOWN_CA)]
        [TestCase(TlsError.ACCESS_DENIED)]
        [TestCase(TlsError.DECODE_ERROR)]
        [TestCase(TlsError.DECRYPT_ERROR)]
        [TestCase(TlsError.EXPORT_RESTRICTION)]
        [TestCase(TlsError.PROTOCOL_VERSION)]
        [TestCase(TlsError.INSUFFICIENT_SECURITY)]
        [TestCase(TlsError.INTERNAL_ERROR)]
        [TestCase(TlsError.INAPPROPRIATE_FALLBACK)]
        [TestCase(TlsError.USER_CANCELED)]
        [TestCase(TlsError.NO_RENEGOTIATION)]
        [TestCase(TlsError.UNSUPPORTED_EXTENSION)]
        [TestCase(TlsError.CERTIFICATE_UNOBTAINABLE)]
        [TestCase(TlsError.UNRECOGNIZED_NAME)]
        [TestCase(TlsError.BAD_CERTIFICATE_STATUS_RESPONSE)]
        [TestCase(TlsError.BAD_CERTIFICATE_HASH_VALUE)]
        [TestCase(TlsError.UNKNOWN_PSK_IDENTITY)]
        [TestCase(TlsError.NO_APPLICATION_PROTOCOL)]
        public void EvaluateCausesStopAdvisoryIfTls12NotSupported(TlsError tlsError)
        {
            TestContext context = new TestContext();
            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(tlsError, null, null);

            LinkedListNode<ITlsRule> result = _tls12GoodCiphersRule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.U2, context.Advisories[0]);
            Assert.Null(result);
        }

        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256)]
        [TestCase(CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384)]
        public void EvaluateCausesNoAdvisoryIfGoodCipherSelected(CipherSuite cipherSuite)
        {
            LinkedListNode<ITlsRule> nextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>());
            TestContext context = new TestContext
            {
                NextTest = nextTest
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, cipherSuite, null, null, null, null, null);

            LinkedListNode<ITlsRule> result = _tls12GoodCiphersRule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(0, context.Advisories.Count);
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
        public void EvaluateCausesStopAdvisoryIfBadCipherSelected(CipherSuite cipherSuite)
        {
            TestContext context = new TestContext
            {
                NextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>())
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, cipherSuite, null, null, null, null, null);

            LinkedListNode<ITlsRule> result = _tls12GoodCiphersRule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.A1, context.Advisories[0]);
            Assert.Null(result);
        }

        [TestCase(TlsError.TCP_CONNECTION_FAILED)]
        [TestCase(TlsError.HOST_NOT_FOUND)]
        [TestCase(TlsError.SESSION_INITIALIZATION_FAILED)]
        public void EvaluateMarksTestRunAsInconclusive(TlsError error)
        {
            TestContext context = new TestContext();

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV12, null, null, null, error, null, null);

            _tls12GoodCiphersRule.Evaluate(context, bouncyCastleResult);

            Assert.True(context.Inconclusive);
        }
    }
}