using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using MailCheck.Mx.BouncyCastle.OldBouncyCastleTlsApi.KeyExchange;
using MailCheck.Mx.BouncyCastle.OldBouncyCastleTlsApi.Mapping;
using MailCheck.Mx.Contracts.SharedDomain;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using CipherSuite = MailCheck.Mx.Contracts.SharedDomain.CipherSuite;

namespace MailCheck.Mx.BouncyCastle.OldBouncyCastleTlsApi
{
    internal class TestTlsClientProtocol : TlsClientProtocol
    {
        private TlsError? _tlsError;
        private string _errorMessage;

        public TestTlsClientProtocol(Stream stream, SecureRandom secureRandom)
            : base(stream, secureRandom)
        {
        }

        public TestTlsClientProtocol(Stream input, Stream output, SecureRandom secureRandom)
            : base(input, output, secureRandom)
        {
        }

        public TestTlsClientProtocol(SecureRandom secureRandom)
            : base(secureRandom)
        {
        }

        public TestTlsClientProtocol(Stream input)
            : this(input, SecureRandom.GetInstance("SHA256PRNG"))
        {
        }

        public BouncyCastleTlsTestResult ConnectWithResults(Org.BouncyCastle.Crypto.Tls.TlsClient tlsClient)
        {
            try
            {
                Connect(tlsClient);
            }
            catch (TlsFatalAlertReceived e)
            {
                _tlsError = (TlsError)e.AlertDescription;
                _errorMessage = e.Message;
                return new BouncyCastleTlsTestResult(_tlsError.Value, e.Message, null);
            }
            catch (TlsFatalAlert e)
            {
                _tlsError = (TlsError)e.AlertDescription;
                _errorMessage = e.Message;
                return new BouncyCastleTlsTestResult(_tlsError.Value, e.Message, null);
            }
            catch (Exception e)
            {
                _tlsError = TlsError.INTERNAL_ERROR;
                _errorMessage = e.Message;
                return new BouncyCastleTlsTestResult(_tlsError.Value, e.Message, null);
            }

            switch (mKeyExchange.GetType().Name)
            {
                case nameof(TestTlsDheKeyExchange):
                    return ProcessKeyExchange((TestTlsDheKeyExchange)mKeyExchange);

                case nameof(TestTlsDhKeyExchange):
                    return ProcessKeyExchange((TestTlsDhKeyExchange)mKeyExchange);

                case nameof(TestTlsEcDheKeyExchange):
                    return ProcessKeyExchange((TestTlsEcDheKeyExchange)mKeyExchange);

                case nameof(TestTlsEcDhKeyExchange):
                    return ProcessKeyExchange((TestTlsEcDhKeyExchange)mKeyExchange);

                case nameof(TlsRsaKeyExchange):
                    return ProcessKeyExchange((TlsRsaKeyExchange)mKeyExchange);

                default:
                    throw new InvalidOperationException($"{mKeyExchange.GetType()} is not recognised key exchange.");
            }
        }

        private BouncyCastleTlsTestResult ProcessKeyExchange(TestTlsDheKeyExchange keyExchange)
        {
            CurveGroup group = keyExchange.DhParameters.ToGroup();

            TlsVersion version = Context.ServerVersion.ToTlsVersion();
            CipherSuite cipherSuite = mSecurityParameters.CipherSuite.ToCipherSuite();
            SignatureHashAlgorithm signatureHashAlgorithm = keyExchange.EcSignatureAndHashAlgorithm.ToSignatureAlgorithm();
            List<X509Certificate2> certificates = mPeerCertificate.ToCertificateList();

            base.CleanupHandshake();
            return new BouncyCastleTlsTestResult(version, cipherSuite, group, signatureHashAlgorithm, _tlsError, _errorMessage, null, certificates);
        }

        private BouncyCastleTlsTestResult ProcessKeyExchange(TestTlsDhKeyExchange keyExchange)
        {
            CurveGroup group = keyExchange.DhParameters.ToGroup();

            TlsVersion version = Context.ServerVersion.ToTlsVersion();
            CipherSuite cipherSuite = mSecurityParameters.CipherSuite.ToCipherSuite();
            List<X509Certificate2> certificates = mPeerCertificate.ToCertificateList();

            base.CleanupHandshake();
            return new BouncyCastleTlsTestResult(version, cipherSuite, group, null, _tlsError, _errorMessage, null, certificates);
        }

        private BouncyCastleTlsTestResult ProcessKeyExchange(TestTlsEcDheKeyExchange keyExchange)
        {
            string curveName = keyExchange.EcPublicKeyParameters.Parameters.Curve.GetType().Name.ToLower();

            CurveGroup curve = curveName.ToCurve();
            TlsVersion version = Context.ServerVersion.ToTlsVersion();
            CipherSuite cipherSuite = mSecurityParameters.CipherSuite.ToCipherSuite();
            SignatureHashAlgorithm signatureHashAlgorithm = keyExchange.EcSignatureAndHashAlgorithm.ToSignatureAlgorithm();
            List<X509Certificate2> certificates = mPeerCertificate.ToCertificateList();

            base.CleanupHandshake();
            return new BouncyCastleTlsTestResult(version, cipherSuite, curve, signatureHashAlgorithm, _tlsError, _errorMessage, null, certificates);
        }

        private BouncyCastleTlsTestResult ProcessKeyExchange(TestTlsEcDhKeyExchange keyExchange)
        {
            string curveName = keyExchange.EcPublicKeyParameters.Parameters.Curve.GetType().Name.ToLower();

            CurveGroup curve = curveName.ToCurve();
            TlsVersion version = Context.ServerVersion.ToTlsVersion();
            CipherSuite cipherSuite = mSecurityParameters.CipherSuite.ToCipherSuite();
            List<X509Certificate2> certificates = mPeerCertificate.ToCertificateList();

            base.CleanupHandshake();
            return new BouncyCastleTlsTestResult(version, cipherSuite, curve, null, _tlsError, _errorMessage, null, certificates);
        }

        private BouncyCastleTlsTestResult ProcessKeyExchange(TlsRsaKeyExchange keyExchange)
        {
            TlsVersion version = Context.ServerVersion.ToTlsVersion();
            CipherSuite cipherSuite = mSecurityParameters.CipherSuite.ToCipherSuite();
            List<X509Certificate2> certificates = mPeerCertificate.ToCertificateList();

            base.CleanupHandshake();
            return new BouncyCastleTlsTestResult(version, cipherSuite, null, null, _tlsError, _errorMessage, null, certificates);
        }

        protected override void CleanupHandshake() { }
    }
}