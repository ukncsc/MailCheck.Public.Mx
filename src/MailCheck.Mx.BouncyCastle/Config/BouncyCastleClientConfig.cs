using System;

namespace MailCheck.Mx.BouncyCastle.Config
{
    public interface IBouncyCastleClientConfig
    {
        TimeSpan TcpSendReceiveTimeout { get; }
        TimeSpan TcpConnectionTimeout { get; }
    }
}
