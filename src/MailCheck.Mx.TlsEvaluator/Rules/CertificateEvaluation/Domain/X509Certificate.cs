using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using X509Extension = System.Security.Cryptography.X509Certificates.X509Extension;

namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
{
    public class X509Certificate
    {
        private readonly X509Certificate2 _msX509;
        private readonly Org.BouncyCastle.X509.X509Certificate _bcX509;

        public X509Certificate(byte[] certificate)
        {
            _msX509 = new X509Certificate2(certificate);

            X509CertificateParser parser = new X509CertificateParser();
            _bcX509 = parser.ReadCertificate(certificate);
        }

        public virtual string ThumbPrint => _msX509.Thumbprint;

        public virtual string Issuer => _msX509.Issuer;

        public virtual string Subject => _msX509.Subject;

        public virtual DateTime ValidFrom => _msX509.NotBefore;

        public virtual DateTime ValidTo => _msX509.NotAfter;

        public virtual string KeyAlgoritm => _msX509.PublicKey.Oid.FriendlyName;

        public virtual int KeyLength => GetKeyLength(_msX509);

        public virtual string SerialNumber => _msX509.SerialNumber;

        public virtual string Version => _msX509.Version.ToString();

        public virtual string SubjectAlternativeName => GetExtension(_msX509, "subject alternative name");

        public virtual string CommonName => _msX509.GetNameInfo(X509NameType.SimpleName, false);

        public virtual bool VerifySignature(byte[] publicKey, string publicKeyIdentifier)
        {
            AsymmetricKeyParameter key = PublicKeyFactory.CreateKey(new SubjectPublicKeyInfo(
                new AlgorithmIdentifier(new DerObjectIdentifier(publicKeyIdentifier)), publicKey));

            try
            {
                _bcX509.Verify(key);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public virtual byte[] Signature => _bcX509.GetSignature();

        public virtual byte[] PublicKey => _msX509.GetPublicKey();

        public virtual byte[] Raw => _msX509.RawData;

        public virtual string PublicKeyIdentifier => _msX509.PublicKey.Oid.Value;

        public virtual bool HasKeyUsage =>  _bcX509.GetKeyUsage() != null;

        public virtual bool HasExtendedKeyUsage => _bcX509.GetExtensionValue(X509Extensions.ExtendedKeyUsage) != null;

        public virtual bool ExtendedKeyUsageIncludesIdKpServerAuth => GetExtendedKeyUsage(OidValues.IdKpServerAuth);

        public virtual bool ExtendedKeyUsageIncludesAnyExtendedKeyUsage => GetExtendedKeyUsage(OidValues.AnyExtendedKeyUsage);

        public virtual bool KeyUsageIncludesKeyCertSign => GetKeyUsage(KeyUsageIndex.KeyCertSign);

        public virtual bool KeyUsageIncludesDigitalSignature => GetKeyUsage(KeyUsageIndex.DigitalSignature);

        public virtual bool KeyUsageIncludesKeyAgreement => GetKeyUsage(KeyUsageIndex.KeyAgreement);

        public virtual bool KeyUsageIncludesKeyEncipherment => GetKeyUsage(KeyUsageIndex.KeyEncipherment);

        private bool GetKeyUsage(KeyUsageIndex index) => HasKeyUsage && _bcX509.GetKeyUsage()[(int)index];

        private bool GetExtendedKeyUsage(string oidValue) => HasExtendedKeyUsage && _bcX509.GetExtendedKeyUsage().Contains(oidValue);

        private static string GetExtension(X509Certificate2 x509Certificate2, string extensionName)
        {
            return x509Certificate2.Extensions.Cast<X509Extension>()
                .FirstOrDefault(_ => (_.Oid.FriendlyName?.ToLower() ?? string.Empty).EndsWith(extensionName))
                ?.Format(false);
        }

        private static int GetKeyLength(X509Certificate2 x509Certificate2)
        {
            return x509Certificate2.PublicKey.Oid.FriendlyName == "RSA"
                ? x509Certificate2.GetRSAPublicKey().KeySize
                : x509Certificate2.GetECDsaPublicKey().KeySize;
        }
    }

    public static class OidValues
    {
        public static readonly string IdKpServerAuth = "1.3.6.1.5.5.7.3.1";

        public static readonly string AnyExtendedKeyUsage = "2.5.29.37.0";
    }

    public enum KeyUsageIndex
    {
        DigitalSignature = 0,
        KeyEncipherment = 2,
        KeyAgreement = 4,
        KeyCertSign = 5
    }
}