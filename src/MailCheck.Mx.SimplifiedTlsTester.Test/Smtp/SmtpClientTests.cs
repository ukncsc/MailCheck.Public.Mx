﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Mx.BouncyCastle;
using MailCheck.Mx.SimplifiedTlsTester.Smtp;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MailCheck.Mx.SimplifiedTlsTester.Test.Smtp
{
    [TestFixture]
    public class SmtpClientTests
    {
        private readonly Response _unknownReponse = new Response(ResponseCode.Unknown, "", "");
        private readonly Response _startTlsResponse = new Response(ResponseCode.Ok, "STARTTLS", "");
        private readonly Response _serviceReadyReponse = new Response(ResponseCode.ServiceReady, "", "");

        private ISmtpClient _smtpClient;
        private ISmtpSerializer _smtpSerializer;
        private ISmtpDeserializer _smtpDeserializer;

        [SetUp]
        public void SetUp()
        {
            _smtpSerializer = A.Fake<ISmtpSerializer>();
            _smtpDeserializer = A.Fake<ISmtpDeserializer>();
            _smtpClient = new SmtpClient(_smtpSerializer, _smtpDeserializer,
                A.Fake<ISmtpClientConfig>(), A.Fake<ILogger<ISmtpClient>>());
        }

        [Test]
        public async Task InitalResponseIsntServiceReadyReturnsFalse()
        {
            A.CallTo(() => _smtpDeserializer.Deserialize(A<StreamReader>._)).Returns(
                Task.FromResult(new SmtpResponse(new List<Response> { _unknownReponse })));

            StartTlsResult result = await _smtpClient.TryStartTls(Stream.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task ElhoResponseDoesntContainStartTlsReturnsFalse()
        {
            A.CallTo(() => _smtpDeserializer.Deserialize(A<StreamReader>._)).ReturnsNextFromSequence(
                Task.FromResult(new SmtpResponse(new List<Response> { _serviceReadyReponse })),
                Task.FromResult(new SmtpResponse(new List<Response> { _unknownReponse })));

            StartTlsResult result = await _smtpClient.TryStartTls(Stream.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task StartTlsResponseIsntServiceReadyReturnFalse()
        {
            A.CallTo(() => _smtpDeserializer.Deserialize(A<StreamReader>._)).ReturnsNextFromSequence(
                Task.FromResult(new SmtpResponse(new List<Response> { _serviceReadyReponse })),
                Task.FromResult(new SmtpResponse(new List<Response> { _startTlsResponse })),
                Task.FromResult(new SmtpResponse(new List<Response> { _unknownReponse })));

            StartTlsResult result = await _smtpClient.TryStartTls(Stream.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task StartTlsSuccessfulReturnsTrue()
        {
            A.CallTo(() => _smtpDeserializer.Deserialize(A<StreamReader>._)).ReturnsNextFromSequence(
                Task.FromResult(new SmtpResponse(new List<Response> { _serviceReadyReponse })),
                Task.FromResult(new SmtpResponse(new List<Response> { _startTlsResponse })),
                Task.FromResult(new SmtpResponse(new List<Response> { _serviceReadyReponse })));

            StartTlsResult result = await _smtpClient.TryStartTls(Stream.Null);
            Assert.That(result.Success, Is.True);
        }
    }
}
