using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Util;
using Microsoft.Extensions.Logging;

namespace MailCheck.Mx.TlsTester.Smtp
{
    public interface ISmtpClient
    {
        Task<StartTlsResult> TryStartTls(Stream networkStream);
    }

    public class SmtpClient : ISmtpClient
    {
        private const string Starttls = "starttls";
        private const string LineEnding = "\r\n";

        private readonly ISmtpSerializer _smtpSerializer;
        private readonly ISmtpDeserializer _smtpDeserializer;
        private readonly IMxTesterConfig _mxSecurityTesterConfig;

        private readonly ILogger<ISmtpClient> _log;

        public SmtpClient(ISmtpSerializer smtpSerializer,
            ISmtpDeserializer smtpDeserializer,
            IMxTesterConfig mxSecurityTesterConfig,
            ILogger<ISmtpClient> log)
        {
            _smtpSerializer = smtpSerializer;
            _smtpDeserializer = smtpDeserializer;
            _mxSecurityTesterConfig = mxSecurityTesterConfig;
            _log = log;
        }

        public async Task<StartTlsResult> TryStartTls(Stream networkStream)
        {
            try
            {
                using (IStreamReader streamReader =
                    new StreamReaderWrapper(networkStream, Encoding.ASCII, true, 1024, true))
                {
                    using (IStreamWriter streamWriter =
                        new StreamWriterWrapper(networkStream, Encoding.ASCII, 1024, true)
                        {
                            AutoFlush = true,
                            NewLine = LineEnding
                        })
                    {
                        SmtpResponse response1 = await _smtpDeserializer.Deserialize(streamReader);
                        _log.LogDebug($"<: {response1}");

                        if (response1.Responses.FirstOrDefault()?.ResponseCode != ResponseCode.ServiceReady)
                        {
                            return new StartTlsResult(false, response1.Responses.Select(_ => _.ToString()).ToList(),
                                "The server did not present a service ready response code (220).");
                        }

                        EhloCommand ehloCommand = new EhloCommand(_mxSecurityTesterConfig.SmtpHostName);
                        _log.LogDebug($">: {ehloCommand.CommandString}");
                        await _smtpSerializer.Serialize(ehloCommand, streamWriter);
                        SmtpResponse response2 = await _smtpDeserializer.Deserialize(streamReader);
                        _log.LogDebug($"<: {response2}");
                        if (!response2.Responses.Any(_ =>
                            _.Value.ToLower() == Starttls && _.ResponseCode == ResponseCode.Ok))
                        {
                            return new StartTlsResult(false, response2.Responses.Select(_ => _.ToString()).ToList(),
                                "The server did not present a STARTTLS command with a response code (250).");
                        }

                        StartTlsCommand startTlsCommand = new StartTlsCommand();
                        _log.LogDebug($">: {startTlsCommand.CommandString}");
                        await _smtpSerializer.Serialize(startTlsCommand, streamWriter);
                        SmtpResponse response3 = await _smtpDeserializer.Deserialize(streamReader);
                        _log.LogDebug($"<: {response3}");

                        return new StartTlsResult(
                            response3.Responses.FirstOrDefault()?.ResponseCode == ResponseCode.ServiceReady,
                            response3.Responses.Select(_ => _.Value).ToList(), string.Empty);

                    }
                }
            }
            catch (Exception e)
            {
                _log.LogError(
                    $"SMTP session initalization failed with error: {e.Message} {Environment.NewLine} {e.StackTrace}");
                return new StartTlsResult(false, null, e.Message);
            }
        }
    }
}