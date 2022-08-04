using System.Net.Sockets;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.BouncyCastle.Config;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.SimplifiedTlsTester.TlsClient
{
    internal class SmtpTlsClient : BouncyCastle.TlsClient
    {
        private readonly ISmtpClient _smtpClient;

        public SmtpTlsClient(ISmtpClient smtpClient,
            IBouncyCastleClientConfig bouncyCastleClientConfig,
            ILogger<SmtpTlsClient> log)
            : base(log, bouncyCastleClientConfig)
        {
            _smtpClient = smtpClient;
        }

        public override Task<StartTlsResult> TryInitializeSession(NetworkStream stream)
        {
            return _smtpClient.TryStartTls(stream);
        }
    }
}
