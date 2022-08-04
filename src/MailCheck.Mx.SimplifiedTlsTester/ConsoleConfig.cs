using System;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    internal class ConsoleConfig : ISmtpClientConfig, IBouncyCastleClientConfig
    {
        public string SmtpHostNameSuffix => "mailcheck.service.ncsc.gov.uk";

        public TimeSpan TcpSendReceiveTimeout => TimeSpan.FromSeconds(30);
        public TimeSpan TcpConnectionTimeout => TimeSpan.FromSeconds(30);
    }
}