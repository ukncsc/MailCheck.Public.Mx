using System.Threading.Tasks;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.Smtp
{
    public interface ISmtpSerializer
    {
        Task Serialize(Command command, IStreamWriter streamWriter);
    }

    internal class SmtpSerializer : ISmtpSerializer
    {
        public Task Serialize(Command command, IStreamWriter streamWriter)
        {
            return streamWriter.WriteLineAsync(command.CommandString);
        }
    }
}