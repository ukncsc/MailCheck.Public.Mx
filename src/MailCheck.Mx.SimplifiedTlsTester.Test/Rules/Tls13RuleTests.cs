using System.Collections.Generic;
using FakeItEasy;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.SimplifiedTlsTester.Domain;
using MailCheck.Mx.SimplifiedTlsTester.Rules;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using NUnit.Framework;
using TestContext = MailCheck.Mx.SimplifiedTlsTester.Domain.TestContext;

namespace MailCheck.Mx.SimplifiedTlsTester.Test.Rules
{
    [TestFixture]
    public class Tls13RuleTests
    {
        private Tls13Rule _tls13Rule;

        [SetUp]
        public void SetUp()
        {
            _tls13Rule = new Tls13Rule();
        }

        [Test]
        public void EvaluateCausesStopAdvisoryIfConnectionFailsTwice()
        {
            TestContext context = new TestContext
            {
                NextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>()),
                HasPreviousFailure = true
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsError.TCP_CONNECTION_FAILED, null, null);

            LinkedListNode<ITlsRule> result = _tls13Rule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.A3, context.Advisories[0]);
            Assert.Null(result);
        }

        [Test]
        public void EvaluateCausesStopAdvisoryIfStartTlsNotSupported()
        {
            TestContext context = new TestContext
            {
                NextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>())
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsError.SESSION_INITIALIZATION_FAILED, null, null)
            {
                SessionInitialisationResult = new SimplifiedStartTlsResult(false, null, null, Outcome.StartTlsNotSupported)
            };

            LinkedListNode<ITlsRule> result = _tls13Rule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreEqual(Advisories.U1, context.Advisories[0]);
            Assert.Null(result);
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
        [TestCase(TlsError.HOST_NOT_FOUND)]
        public void EvaluateCausesAdvisoryIfTls13NotSupported(TlsError tlsError)
        {
            LinkedListNode<ITlsRule> nextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>());
            TestContext context = new TestContext
            {
                NextTest = nextTest
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(tlsError, null, null);

            LinkedListNode<ITlsRule> result = _tls13Rule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.I1, context.Advisories[0]);
            Assert.AreSame(nextTest, result);
        }

        [TestCase(CipherSuite.TLS_AES_256_GCM_SHA384)]
        [TestCase(CipherSuite.TLS_AES_128_GCM_SHA256)]
        public void EvaluateCausesAdvisoryIfGoodCipherSelected(CipherSuite cipherSuite)
        {
            LinkedListNode<ITlsRule> nextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>());
            TestContext context = new TestContext
            {
                NextTest = nextTest
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV13, cipherSuite, null, null, null, null, null);

            LinkedListNode<ITlsRule> result = _tls13Rule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.P2, context.Advisories[0]);
            Assert.AreSame(nextTest, result);
        }

        [TestCase(CipherSuite.TLS_AES_128_CCM_SHA256)]
        [TestCase(CipherSuite.TLS_AES_128_CCM_8_SHA256)]
        [TestCase(CipherSuite.TLS_CHACHA20_POLY1305_SHA256)]
        [TestCase(CipherSuite.TLS_SM4_GCM_SM3)]
        [TestCase(CipherSuite.TLS_SM4_CCM_SM3)]
        public void EvaluateCausesAdvisoryIfBadCipherSelected(CipherSuite cipherSuite)
        {
            LinkedListNode<ITlsRule> nextTest = new LinkedListNode<ITlsRule>(A.Fake<ITlsRule>());
            TestContext context = new TestContext
            {
                NextTest = nextTest
            };

            BouncyCastleTlsTestResult bouncyCastleResult = new BouncyCastleTlsTestResult(TlsVersion.TlsV13, cipherSuite, null, null, null, null, null);

            LinkedListNode<ITlsRule> result = _tls13Rule.Evaluate(context, bouncyCastleResult);

            Assert.AreEqual(1, context.Advisories.Count);
            Assert.AreSame(Advisories.I1, context.Advisories[0]);
            Assert.AreSame(nextTest, result);
        }
    }
}
