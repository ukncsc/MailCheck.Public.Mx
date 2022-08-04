using System;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Mx.BouncyCastle.Config;

namespace MailCheck.Mx.TlsTester.Config
{
    public class BouncyCastleClientConfig : IBouncyCastleClientConfig
    {
        public BouncyCastleClientConfig(IEnvironmentVariables environmentVariables)
        {
            TcpSendReceiveTimeout = TimeSpan.FromSeconds(environmentVariables.GetAsInt("TcpSendReceiveTimeout"));
            TcpConnectionTimeout = TimeSpan.FromSeconds(environmentVariables.GetAsInt("TcpConnectionTimeoutSeconds"));
        }

        public TimeSpan TcpSendReceiveTimeout { get; }
        public TimeSpan TcpConnectionTimeout { get; }
    }
}
