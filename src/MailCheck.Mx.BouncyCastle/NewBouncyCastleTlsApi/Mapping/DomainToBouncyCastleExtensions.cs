using System;
using MailCheck.Mx.Contracts.SharedDomain;
using Org.BouncyCastle.Tls;

namespace MailCheck.Mx.BouncyCastle.NewBouncyCastleTlsApi.Mapping
{
    public static class DomainToBouncyCastleExtensions
    {
        public static ProtocolVersion ToProtocolVersion(this TlsVersion version)
        {
            switch (version)
            {
                case TlsVersion.SslV3:
                    return ProtocolVersion.SSLv3;
                case TlsVersion.TlsV1:
                    return ProtocolVersion.TLSv10;
                case TlsVersion.TlsV11:
                    return ProtocolVersion.TLSv11;
                case TlsVersion.TlsV12:
                    return ProtocolVersion.TLSv12;
                case TlsVersion.TlsV13:
                    return ProtocolVersion.TLSv13;
                default:
                    throw new InvalidOperationException($"Cannot convert ({version}) to BouncyCastle ProtocolVersion.");
            }
        }
    }
}
