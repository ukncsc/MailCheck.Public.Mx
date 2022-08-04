using System.IO;
using System.Threading.Tasks;

namespace MailCheck.Mx.SimplifiedTlsTester.Smtp
{
    public interface ISmtpSerializer
    {
        Task Serialize(Command command, TextWriter streamWriter);
    }

    internal class SmtpSerializer : ISmtpSerializer
    {
        public Task Serialize(Command command, TextWriter streamWriter)
        {
            return streamWriter.WriteLineAsync(command.CommandString);
        }
    }
}