using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MailCheck.Mx.Contracts.SharedDomain;
using Org.BouncyCastle.Tls;
using CipherSuite = MailCheck.Mx.Contracts.SharedDomain.CipherSuite;

namespace MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi.Mapping
{
    public static class BouncyCastleToDomainExtensions
    {
        public static TlsVersion ToTlsVersion(this ProtocolVersion protocolVersion)
        {
            return Enum.IsDefined(typeof(TlsVersion), (ushort)protocolVersion.FullVersion) ?
                (TlsVersion)protocolVersion.FullVersion : TlsVersion.Unknown;
        }

        public static (CipherSuite cipherSuite, List<X509Certificate2> cerificates) ToSharedDomain(this SecurityParameters securityParameters)
        {
            return ((CipherSuite)securityParameters.CipherSuite, securityParameters.PeerCertificate.ToCertificateList());
        }

        public static List<X509Certificate2> ToCertificateList(this Org.BouncyCastle.Tls.Certificate certificate)
        {
            return certificate.GetCertificateList()
                .Select(bcCert => new X509Certificate2(bcCert.GetEncoded()))
                .ToList();
        }
    }
}
