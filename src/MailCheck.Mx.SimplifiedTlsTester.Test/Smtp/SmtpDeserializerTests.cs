using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using NUnit.Framework;

namespace MailCheck.Mx.SimplifiedTlsTester.Test.Smtp
{
    [TestFixture]
    public class SmtpDeserializerTests
    {
        private SmtpDeserializer _smtpDeserializer;

        [SetUp]
        public void SetUp()
        {
            _smtpDeserializer = new SmtpDeserializer();
        }

        [Test]
        public async Task ProcessesSingleLineResponseCorrectly()
        {
            string response = "220 2.0.0 Ready to start TLS";
            SmtpResponse smtpResponse;
            using (TextReader reader = new StringReader(response))
            {
                smtpResponse = await _smtpDeserializer.Deserialize(reader);
            }

            Assert.That(smtpResponse.Responses.Count, Is.EqualTo(1));
            Assert.That(smtpResponse.Responses.First().ResponseCode, Is.EqualTo(ResponseCode.ServiceReady));
            Assert.That(smtpResponse.Responses.First().Value, Is.EqualTo("2.0.0 Ready to start TLS"));
        }

        [Test]
        public async Task ProcessesMultiLineResponseCorrectly()
        {
            string responseLn1 = "250-smtp.com at your service, [123.456.789.101]";
            string responseLn2 = "250-SIZE 35882577";
            string responseLn3 = "250 STARTTLS";

            SmtpResponse smtpResponse;

            using (TextReader reader = new StringReader(string.Join(Environment.NewLine, responseLn1, responseLn2, responseLn3)))
            {
                smtpResponse = await _smtpDeserializer.Deserialize(reader);
            }

            Assert.That(smtpResponse.Responses.Count, Is.EqualTo(3));

            Assert.That(smtpResponse.Responses[0].ResponseCode, Is.EqualTo(ResponseCode.Ok));
            Assert.That(smtpResponse.Responses[0].Value, Is.EqualTo("smtp.com at your service, [123.456.789.101]"));

            Assert.That(smtpResponse.Responses[1].ResponseCode, Is.EqualTo(ResponseCode.Ok));
            Assert.That(smtpResponse.Responses[1].Value, Is.EqualTo("SIZE 35882577"));

            Assert.That(smtpResponse.Responses[2].ResponseCode, Is.EqualTo(ResponseCode.Ok));
            Assert.That(smtpResponse.Responses[2].Value, Is.EqualTo("STARTTLS"));
        }

        [Test]
        public void InvalidResponseLengthThrows()
        {
            string response = "220";

            using (TextReader reader = new StringReader(response))
            {
                Assert.ThrowsAsync<ArgumentException>(() => _smtpDeserializer.Deserialize(reader));
            }
        }

        [Test]
        public void InvalidResponseCodeThrows()
        {
            string response = "ABC response";

            using (TextReader reader = new StringReader(response))
            {
                Assert.ThrowsAsync<ArgumentException>(() => _smtpDeserializer.Deserialize(reader));
            }
        }

        [Test]
        public void InvalidSeparatorThrows()
        {
            string response = "220*response";

            using (TextReader reader = new StringReader(response))
            {
                Assert.ThrowsAsync<ArgumentException>(() => _smtpDeserializer.Deserialize(reader));
            }
        }
    }
}
