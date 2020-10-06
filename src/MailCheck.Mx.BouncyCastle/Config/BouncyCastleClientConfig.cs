using System;

namespace MailCheck.Mx.BouncyCastle.Config
{
    public interface IBouncyCastleClientConfig
    {
        TimeSpan TlsConnectionTimeOut { get; }
    }
}
