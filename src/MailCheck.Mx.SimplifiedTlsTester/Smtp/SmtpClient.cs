using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.SimplifiedTlsTester.Smtp
{
    public interface ISmtpClient
    {
        Task<StartTlsResult> TryStartTls(Stream networkStream);
    }

    public class SmtpClient : ISmtpClient
    {
        private const string Starttls = "starttls";
        private const string LineEnding = "\r\n";

        private readonly Random Rand = new Random();

        private readonly ISmtpSerializer _smtpSerializer;
        private readonly ISmtpDeserializer _smtpDeserializer;
        private readonly ISmtpClientConfig _config;

        private readonly ILogger<ISmtpClient> _log;

        public SmtpClient(ISmtpSerializer smtpSerializer,
            ISmtpDeserializer smtpDeserializer,
            ISmtpClientConfig config,
            ILogger<ISmtpClient> log)
        {
            _smtpSerializer = smtpSerializer;
            _smtpDeserializer = smtpDeserializer;
            _config = config;
            _log = log;
        }

        public async Task<StartTlsResult> TryStartTls(Stream networkStream)
        {
            List<string> collectedHandshake = new List<string>();
            try
            {
                using (StreamReader streamReader =
                    new StreamReader(networkStream, Encoding.ASCII, true, 1024, true))
                {
                    using (StreamWriter streamWriter =
                        new StreamWriter(networkStream, Encoding.ASCII, 1024, true)
                        {
                            AutoFlush = true,
                            NewLine = LineEnding
                        })
                    {
                        SmtpResponse response1 = await _smtpDeserializer.Deserialize(streamReader);
                        collectedHandshake.AddRange(response1.Responses.Select(r => $"<: {r}"));

                        if (response1.Responses.Count == 0)
                        {
                            return new SimplifiedStartTlsResult(false, collectedHandshake, "The server did not respond.", Outcome.NoResponse);
                        }

                        if (response1.Responses.FirstOrDefault()?.ResponseCode == ResponseCode.TransientError)
                        {
                            return new SimplifiedStartTlsResult(false, collectedHandshake, "The server presented a transient error (421).", Outcome.TransientError);
                        }

                        if (response1.Responses.FirstOrDefault()?.ResponseCode != ResponseCode.ServiceReady)
                        {
                            return new SimplifiedStartTlsResult(false, collectedHandshake, "The server did not present a service ready response code (220).", Outcome.NotReady);
                        }

                        EhloCommand ehloCommand = new EhloCommand(GetRandomHostname());
                        await _smtpSerializer.Serialize(ehloCommand, streamWriter);
                        collectedHandshake.Add($">: {ehloCommand.CommandString}");
                        
                        SmtpResponse response2 = await _smtpDeserializer.Deserialize(streamReader);
                        collectedHandshake.AddRange(response2.Responses.Select(r => $"<: {r}"));

                        if (!response2.Responses.Any(r => r.ResponseCode == ResponseCode.Ok && StringComparer.OrdinalIgnoreCase.Equals(r.Value, Starttls)))
                        {
                            return new SimplifiedStartTlsResult(false, collectedHandshake, "The server did not present a STARTTLS command with a response code (250).", Outcome.StartTlsNotSupported);
                        }

                        StartTlsCommand startTlsCommand = new StartTlsCommand();
                        await _smtpSerializer.Serialize(startTlsCommand, streamWriter);
                        collectedHandshake.Add($">: {startTlsCommand.CommandString}");

                        SmtpResponse response3 = await _smtpDeserializer.Deserialize(streamReader);
                        collectedHandshake.AddRange(response3.Responses.Select(r => $"<: {r}"));

                        if (response3.Responses.FirstOrDefault()?.ResponseCode == ResponseCode.ServiceReady)
                        {
                            return new SimplifiedStartTlsResult(true, collectedHandshake, string.Empty, Outcome.Ready);
                        }
                        else
                        {
                            return new SimplifiedStartTlsResult(false, collectedHandshake, "The server was not ready after STARTTLS sent", Outcome.StartTlsRequestFailed);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, "SMTP session initialisation failed");
                return new SimplifiedStartTlsResult(false, collectedHandshake, e.Message, Outcome.Exception);
            }
            finally
            {
                _log.LogInformation($"SMTP handshake:{Environment.NewLine}{string.Join(Environment.NewLine, collectedHandshake)}");
            }
        }

        private string GetRandomHostname()
        {
            var randomGatewayIndex = Rand.Next(1, 4); // maxValue is not inclusive so this returns 1, 2 to 3
            return $"gateway{randomGatewayIndex}.{_config.SmtpHostNameSuffix}";
        }
    }
}