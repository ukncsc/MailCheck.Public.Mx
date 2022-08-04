using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using MailCheck.Mx.TlsEvaluator.Config;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Message = MailCheck.Common.Messaging.Abstractions.Message;

namespace MailCheck.Mx.TlsEvaluator.Test
{
    [TestFixture]
    public class SimplifiedEvaluationHandlerTests
    {
        [Test]
        public async Task Handle_PerformsEvaluation_DispatchesResult()
        {
            var fakeEvaluator = A.Fake<IEvaluator<HostCertificates>>();
            var fakeNamedEvaluator = A.Fake<IEvaluator<HostCertificatesWithName>>();
            var fakeDispatcher = A.Fake<IMessageDispatcher>();
            var fakeConfig = A.Fake<ITlsRptEvaluatorConfig>();
            var fakeLogger = A.Fake<ILogger<SimplifiedEvaluationHandler>>();
            var fakeExtractor = A.Fake<Func<SimplifiedHostCertificateResult, List<HostCertificates>>>();
            var handler = new SimplifiedEvaluationHandler(fakeEvaluator, fakeNamedEvaluator, fakeDispatcher, fakeConfig, fakeLogger, fakeExtractor);

            var message = JsonConvert.DeserializeObject<SimplifiedHostCertificateResult>(SingleChainResult);
            var certsA = new HostCertificates("127.0.0.1", false, null, null);
            var certsB = new HostCertificates("127.0.0.2", false, null, null);
            var hostCerts = new List<HostCertificates> { certsA, certsB };

            var certsAWithName = new HostCertificatesWithName("testa.com", certsA);
            var certsBWithName = new HostCertificatesWithName("testb.com", certsB);

            var resultA = new EvaluationResult<HostCertificates>(certsA, new List<EvaluationError>());
            var resultB = new EvaluationResult<HostCertificates>(certsB, new List<EvaluationError> { new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Cert has error") });

            var resultAWithName = new EvaluationResult<HostCertificatesWithName>(certsAWithName, new List<EvaluationError>());
            var resultBWithName = new EvaluationResult<HostCertificatesWithName>(certsBWithName, new List<EvaluationError> { new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Hostname missing") });

            A.CallTo(() => fakeExtractor(message)).Returns(hostCerts);
            A.CallTo(() => fakeEvaluator.Evaluate(certsA)).Returns(resultA);
            A.CallTo(() => fakeEvaluator.Evaluate(certsB)).Returns(resultB);
            A.CallTo(() => fakeNamedEvaluator.Evaluate(certsAWithName)).Returns(resultAWithName);
            A.CallTo(() => fakeNamedEvaluator.Evaluate(certsBWithName)).Returns(resultBWithName);

            A.CallTo(() => fakeConfig.SnsTopicArn).Returns("topic");

            await handler.Handle(message);

            A.CallTo(() => fakeDispatcher.Dispatch(A<Message>.That.IsInstanceOf(typeof(SimplifiedHostCertificateEvaluated)), "topic")).MustHaveHappened();
        }

        [Test]
        public void ExtractCertificateEvaluationParams_SingleChainResult_ExtractsSingleChain()
        {
            var message = JsonConvert.DeserializeObject<SimplifiedHostCertificateResult>(SingleChainResult);
            var hostCerts = SimplifiedEvaluationHandler.ExtractCertificateEvaluationParams(message);
            Assert.That(hostCerts.Count, Is.EqualTo(1));
            Assert.That(hostCerts[0].Certificates.Count, Is.EqualTo(3));
            Assert.That(hostCerts[0].SelectedCipherSuites.Count, Is.EqualTo(3));
            Assert.That(hostCerts[0].Host, Is.EqualTo("173.194.79.27"));
            Assert.That(hostCerts[0].HostNotFound, Is.EqualTo(false));

            // Certificates should be in chain order presented in connection results
            Assert.That(hostCerts[0].Certificates[0].ThumbPrint, Is.EqualTo("A5450389594342DB19BCC9E4E6F9F0755FD1A173"));
            Assert.That(hostCerts[0].Certificates[1].ThumbPrint, Is.EqualTo("1E7EF647CBA150281C60897257102878C4BD8CDC"));
            Assert.That(hostCerts[0].Certificates[2].ThumbPrint, Is.EqualTo("08745487E891C19E3078C1F2A07E452950EF36F6"));
        }

        [Test]
        public void ExtractCertificateEvaluationParams_MultiChainResult_ExtractsMaultiChain()
        {
            var message = JsonConvert.DeserializeObject<SimplifiedHostCertificateResult>(MultiChainResult);
            var hostCerts = SimplifiedEvaluationHandler.ExtractCertificateEvaluationParams(message);
            Assert.That(hostCerts.Count, Is.EqualTo(2));
            Assert.That(hostCerts[0].Certificates.Count, Is.EqualTo(3));
            Assert.That(hostCerts[0].SelectedCipherSuites.Count, Is.EqualTo(1));
            Assert.That(hostCerts[0].Host, Is.EqualTo("173.194.79.27"));
            Assert.That(hostCerts[0].HostNotFound, Is.EqualTo(false));

            // Certificates should be in chain order presented in connection results
            Assert.That(hostCerts[0].Certificates[0].ThumbPrint, Is.EqualTo("A5450389594342DB19BCC9E4E6F9F0755FD1A173"));
            Assert.That(hostCerts[0].Certificates[1].ThumbPrint, Is.EqualTo("1E7EF647CBA150281C60897257102878C4BD8CDC"));
            Assert.That(hostCerts[0].Certificates[2].ThumbPrint, Is.EqualTo("08745487E891C19E3078C1F2A07E452950EF36F6"));

            Assert.That(hostCerts[1].Certificates.Count, Is.EqualTo(2));
            Assert.That(hostCerts[1].SelectedCipherSuites.Count, Is.EqualTo(1));
            Assert.That(hostCerts[1].Host, Is.EqualTo("173.194.79.27"));
            Assert.That(hostCerts[1].HostNotFound, Is.EqualTo(false));

            // Certificates should be in chain order presented in connection results
            Assert.That(hostCerts[1].Certificates[0].ThumbPrint, Is.EqualTo("56BD74636CBAF92BEA8424B904819E1AF7BD95F3"));
            Assert.That(hostCerts[1].Certificates[1].ThumbPrint, Is.EqualTo("4C27431717565A3A07F3E6D0032C4258949CF9EC"));
        }

        [Test]
        public void ExtractCertificateEvaluationParams_ResultsWithFailure_ExtractsSingleChain()
        {
            var message = JsonConvert.DeserializeObject<SimplifiedHostCertificateResult>(ResultsWithFailure);
            var hostCerts = SimplifiedEvaluationHandler.ExtractCertificateEvaluationParams(message);
            Assert.That(hostCerts.Count, Is.EqualTo(1));
            Assert.That(hostCerts[0].Certificates.Count, Is.EqualTo(2));
            Assert.That(hostCerts[0].SelectedCipherSuites.Count, Is.EqualTo(2));
            Assert.That(hostCerts[0].Host, Is.EqualTo("173.194.79.27"));
            Assert.That(hostCerts[0].HostNotFound, Is.EqualTo(false));

            // Certificates should be in chain order presented in connection results
            Assert.That(hostCerts[0].Certificates[0].ThumbPrint, Is.EqualTo("56BD74636CBAF92BEA8424B904819E1AF7BD95F3"));
            Assert.That(hostCerts[0].Certificates[1].ThumbPrint, Is.EqualTo("4C27431717565A3A07F3E6D0032C4258949CF9EC"));
        }

        [Test]
        public void Batch_NoHostAdvisories_DoesntBatch()
        {
            var hostnames = new List<string> { "host1", "host2", "host3", "host4", "host5" };
            var globals = new List<NamedAdvisory>
            {
                new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname1", Common.Contracts.Advisories.MessageType.error, "global advisory 1", null)
            };

            var result = new SimplifiedHostCertificateEvaluated("127.0.0.1")
            {
                Hostnames = hostnames,
                CertificateAdvisoryMessages = globals,
                HostSpecificCertificateAdvisoryMessages = new Dictionary<string, List<NamedAdvisory>>()
            };

            var batches = SimplifiedEvaluationHandler.Batch(result, 2).ToList();
            Assert.That(batches, Is.EquivalentTo(new[] { result }));
        }

        [Test]
        public void Batch_SomeHostAdvisories_SplitsIntoBatches()
        {
            var ip = "127.0.0.1";
            var hostnames = new List<string> { "host1", "host2", "host3", "host4", "host5" };

            var globals = new List<NamedAdvisory>
            {
                new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname1", Common.Contracts.Advisories.MessageType.error, "global advisory 1", null)
            };

            var host1Advs = new List<NamedAdvisory>
            {
                new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname2", Common.Contracts.Advisories.MessageType.error, "host 1 advisory 1", null)
            };

            var host2Advs = new List<NamedAdvisory>
            {
                new NamedAdvisory(Guid.Empty, "mailcheck.tls.testname3", Common.Contracts.Advisories.MessageType.error, "host 2 advisory 1", null)
            };

            var result = new SimplifiedHostCertificateEvaluated(ip)
            {
                Hostnames = hostnames,
                Certificates = new Dictionary<string, string>{ { "thumbprint", "cert" } },
                CertificateAdvisoryMessages = globals,
                HostSpecificCertificateAdvisoryMessages = new Dictionary<string, List<NamedAdvisory>>
                {
                    ["host1"] = host1Advs,
                    ["host2"] = host2Advs,
                }
            };

            var batches = SimplifiedEvaluationHandler.Batch(result, 2).ToList();
            Assert.That(batches, Has.Count.EqualTo(3));
            Assert.That(batches[0].Id, Is.EqualTo(ip));
            Assert.That(batches[1].Id, Is.EqualTo(ip));
            Assert.That(batches[2].Id, Is.EqualTo(ip));

            Assert.That(batches[0].Certificates.Count, Is.EqualTo(1));
            Assert.That(batches[1].Certificates.Count, Is.EqualTo(1));
            Assert.That(batches[2].Certificates.Count, Is.EqualTo(1));

            Assert.That(batches[0].Hostnames, Is.EquivalentTo(new[] { "host1", "host2" }));
            Assert.That(batches[1].Hostnames, Is.EquivalentTo(new[] { "host3", "host4" }));
            Assert.That(batches[2].Hostnames, Is.EquivalentTo(new[] { "host5" }));

            Assert.That(batches[0].CertificateAdvisoryMessages, Is.SameAs(globals));
            Assert.That(batches[1].CertificateAdvisoryMessages, Is.SameAs(globals));
            Assert.That(batches[2].CertificateAdvisoryMessages, Is.SameAs(globals));

            Assert.That(batches[0].HostSpecificCertificateAdvisoryMessages, Is.EquivalentTo(new[] { KeyValuePair.Create("host1", host1Advs), KeyValuePair.Create("host2", host2Advs), }));
            Assert.That(batches[1].HostSpecificCertificateAdvisoryMessages, Has.Count.EqualTo(0));
            Assert.That(batches[2].HostSpecificCertificateAdvisoryMessages, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task EvaluatedCertificatesSetsRootCorrectly()
        {
            var fakeEvaluator = A.Fake<IEvaluator<HostCertificates>>();
            var fakeNamedEvaluator = A.Fake<IEvaluator<HostCertificatesWithName>>();
            var fakeDispatcher = A.Fake<IMessageDispatcher>();
            var fakeConfig = A.Fake<ITlsRptEvaluatorConfig>();
            var fakeLogger = A.Fake<ILogger<SimplifiedEvaluationHandler>>();
            var fakeExtractor = A.Fake<Func<SimplifiedHostCertificateResult, List<HostCertificates>>>();
            var handler = new SimplifiedEvaluationHandler(fakeEvaluator, fakeNamedEvaluator, fakeDispatcher, fakeConfig, fakeLogger, fakeExtractor);

            var message = JsonConvert.DeserializeObject<SimplifiedHostCertificateResult>(SingleChainResult);

            X509Certificate certARootCert = A.Fake<X509Certificate>();
            A.CallTo(() => certARootCert.Issuer).Returns("Root");
            A.CallTo(() => certARootCert.Subject).Returns("Root");
            A.CallTo(() => certARootCert.ThumbPrint).Returns("Root Thumbprint A");
            var certsACertificates = new List<X509Certificate> { certARootCert };

            X509Certificate certBRootCert = A.Fake<X509Certificate>();
            A.CallTo(() => certBRootCert.Issuer).Returns("Root");
            A.CallTo(() => certBRootCert.Subject).Returns("Root");
            A.CallTo(() => certBRootCert.ThumbPrint).Returns("Root Thumbprint B");
            var certsBCertificates= new List<X509Certificate>{ certBRootCert };

            var certsA = new HostCertificates("127.0.0.1", false, certsACertificates, null);
            var certsB = new HostCertificates("127.0.0.2", false, certsBCertificates, null);
            var hostCerts = new List<HostCertificates> { certsA, certsB };

            var certsAWithName = new HostCertificatesWithName("testa.com", certsA);
            var certsBWithName = new HostCertificatesWithName("testb.com", certsB);

            var resultA = new EvaluationResult<HostCertificates>(certsA, new List<EvaluationError>());
            var resultB = new EvaluationResult<HostCertificates>(certsB, new List<EvaluationError> { new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Cert has error") });

            var resultAWithName = new EvaluationResult<HostCertificatesWithName>(certsAWithName, new List<EvaluationError>());
            var resultBWithName = new EvaluationResult<HostCertificatesWithName>(certsBWithName, new List<EvaluationError> { new EvaluationError(new Guid(), "mailcheck.tlsCert.testName1", EvaluationErrorType.Error, "Hostname missing") });

            A.CallTo(() => fakeExtractor(message)).Returns(hostCerts);
            A.CallTo(() => fakeEvaluator.Evaluate(certsA)).Returns(resultA);
            A.CallTo(() => fakeEvaluator.Evaluate(certsB)).Returns(resultB);
            A.CallTo(() => fakeNamedEvaluator.Evaluate(certsAWithName)).Returns(resultAWithName);
            A.CallTo(() => fakeNamedEvaluator.Evaluate(certsBWithName)).Returns(resultBWithName);

            A.CallTo(() => fakeConfig.SnsTopicArn).Returns("topic");

            await handler.Handle(message);
            A.CallTo(() => fakeDispatcher.Dispatch(A<SimplifiedHostCertificateEvaluated>.That.Matches(a =>
                a.RootCertificateThumbprint == "Root Thumbprint A"), A<string>._)).MustHaveHappened();
        }


        const string SingleChainResult = @"
{
    ""SimplifiedTlsConnectionResults"": [
        {
            ""TestName"": ""Tls13Rule"",
            ""CipherSuite"": ""TLS_AES_256_GCM_SHA384"",
            ""CertificateThumbprints"": [
                ""A5450389594342DB19BCC9E4E6F9F0755FD1A173"",
                ""1E7EF647CBA150281C60897257102878C4BD8CDC"",
                ""08745487E891C19E3078C1F2A07E452950EF36F6""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        },
        {
            ""TestName"": ""Tls12GoodCiphersRule"",
            ""CipherSuite"": ""TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256"",
            ""CertificateThumbprints"": [
                ""A5450389594342DB19BCC9E4E6F9F0755FD1A173"",
                ""1E7EF647CBA150281C60897257102878C4BD8CDC"",
                ""08745487E891C19E3078C1F2A07E452950EF36F6""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        },
        {
            ""TestName"": ""Tls12ServerPreferenceRule"",
            ""CipherSuite"": ""TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256"",
            ""CertificateThumbprints"": [
                ""A5450389594342DB19BCC9E4E6F9F0755FD1A173"",
                ""1E7EF647CBA150281C60897257102878C4BD8CDC"",
                ""08745487E891C19E3078C1F2A07E452950EF36F6""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        }
    ],
    ""Certificates"": {
        ""08745487E891C19E3078C1F2A07E452950EF36F6"": ""MIIFYjCCBEqgAwIBAgIQd70NbNs2+RrqIQ/E8FjTDTANBgkqhkiG9w0BAQsFADBXMQswCQYDVQQGEwJCRTEZMBcGA1UEChMQR2xvYmFsU2lnbiBudi1zYTEQMA4GA1UECxMHUm9vdCBDQTEbMBkGA1UEAxMSR2xvYmFsU2lnbiBSb290IENBMB4XDTIwMDYxOTAwMDA0MloXDTI4MDEyODAwMDA0MlowRzELMAkGA1UEBhMCVVMxIjAgBgNVBAoTGUdvb2dsZSBUcnVzdCBTZXJ2aWNlcyBMTEMxFDASBgNVBAMTC0dUUyBSb290IFIxMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAthECix7joXebO9y/lD63ladAPKH9gvl9MgaCcfb2jH/76Nu8ai6Xl6OMS/kr9rH5zoQdsfnFl97vufKj6bwSiV6nqlKr+CMny6SxnGPb15l+8Ape62im9MZaRw1NEDPjTrETo8gYbEvs/AmQ351kKSUjB6G00j0uYODP0gmHu81I8E3CwnqIiru6z1kZ1q+PsAewnjHxgsHA3y6mbWwZDrXYfiYaRQM9sHmklCitD38m5agI/pboPGiUU+6DOogrFZYJsuB6jC511pzrp1Zkj5ZPaK49l8KEj8C8QMALXL32h7M1bKwYUH+E4EzNktMg6TO8UpmvMrUpsyUqtEj5cuHKZPfmghCN6J3Cioj6OGaK/GP5Afl4/Xtcd/p2h/rs37EOeZVXtL0m79YB0esWCruOC7XFxYpVq9Os6pFLKcwZpDIlTirxZUTQAs6qzkm06p98g7BAe+dDq6dso499iYH6TKX/1Y7DzkvgtdizjkXPdsDtQCv9Uw+wp9U7DbGKogPeMa3Md+pvez7W35EiEua++tgy/BBjFFFy3l3WFpO9KWgz7zpm7AeKJt8T11dleCfeXkkUAKIAf5qoIbapsZWwpbkNFhHax2xIPEDgfg1azVY80ZcFuctL7TlLnMQ/0lUTbiSw1nH69MG6zO0b9f6BQdgAmD06yK56mDcYBZUCAwEAAaOCATgwggE0MA4GA1UdDwEB/wQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTkrysmcRorSCeFL1JmLO/wiRNxPjAfBgNVHSMEGDAWgBRge2YaRQ2XyolQL30EzTSo//z9SzBgBggrBgEFBQcBAQRUMFIwJQYIKwYBBQUHMAGGGWh0dHA6Ly9vY3NwLnBraS5nb29nL2dzcjEwKQYIKwYBBQUHMAKGHWh0dHA6Ly9wa2kuZ29vZy9nc3IxL2dzcjEuY3J0MDIGA1UdHwQrMCkwJ6AloCOGIWh0dHA6Ly9jcmwucGtpLmdvb2cvZ3NyMS9nc3IxLmNybDA7BgNVHSAENDAyMAgGBmeBDAECATAIBgZngQwBAgIwDQYLKwYBBAHWeQIFAwIwDQYLKwYBBAHWeQIFAwMwDQYJKoZIhvcNAQELBQADggEBADSkHrEoo9C0dhemMXoh6dFSPsjbdBZBiLg9NR3t5P+T4Vxfq7vqfM/b5A3Ri1fyJm9bvhdGaJQ3b2t6yMAYN/olUazsaL+yyEn9WprKASOshIArAoyZl+tJaox118fessmXn1hIVw41oeQa1v1vg4Fv74zPl6/AhSrw9U5pCZEt4Wi4wStz6dTZ/CLANx8LZh1J7QJVj2fhMtfTJr9w4z30Z209fOU0iOMy+qduBmpvvYuR7hZL6Dupszfnw0Skfths18dG9ZKb59UhvmaSGZRVbNQpsg3BZlvid0lIKO2d1xozclOzgjXPYovJJIultzkMu34qQb9Sz/yilrbCgj8="",
        ""a5450389594342DB19BCC9E4E6F9F0755FD1A173"": ""MIIG9zCCBd+gAwIBAgIRAI3R7wF8yk7nCgAAAAEiTccwDQYJKoZIhvcNAQELBQAwRjELMAkGA1UEBhMCVVMxIjAgBgNVBAoTGUdvb2dsZSBUcnVzdCBTZXJ2aWNlcyBMTEMxEzARBgNVBAMTCkdUUyBDQSAxQzMwHhcNMjExMTA4MDMxMjMzWhcNMjIwMTMxMDMxMjMyWjAYMRYwFAYDVQQDEw1teC5nb29nbGUuY29tMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAENC6zq4ikKXIOR5VPaoRQ0v1BAB553hJY93f6qlbY/wP4uIW01XWDuw23ppHJJpcRWSyo5KTbgPIm7RXGXal2NaOCBNcwggTTMA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAKBggrBgEFBQcDATAMBgNVHRMBAf8EAjAAMB0GA1UdDgQWBBTfVI2uP6SiUWXLD3PFzw7KWvMnrjAfBgNVHSMEGDAWgBSKdH+vhc3ulc09nNDiRhTzcTUdJzBqBggrBgEFBQcBAQReMFwwJwYIKwYBBQUHMAGGG2h0dHA6Ly9vY3NwLnBraS5nb29nL2d0czFjMzAxBggrBgEFBQcwAoYlaHR0cDovL3BraS5nb29nL3JlcG8vY2VydHMvZ3RzMWMzLmRlcjCCAoYGA1UdEQSCAn0wggJ5gg1teC5nb29nbGUuY29tgg9zbXRwLmdvb2dsZS5jb22CEmFzcG14LmwuZ29vZ2xlLmNvbYIXYWx0MS5hc3BteC5sLmdvb2dsZS5jb22CF2FsdDIuYXNwbXgubC5nb29nbGUuY29tghdhbHQzLmFzcG14LmwuZ29vZ2xlLmNvbYIXYWx0NC5hc3BteC5sLmdvb2dsZS5jb22CGmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQxLmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQyLmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQzLmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQ0LmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tghhnbXItc210cC1pbi5sLmdvb2dsZS5jb22CHWFsdDEuZ21yLXNtdHAtaW4ubC5nb29nbGUuY29tgh1hbHQyLmdtci1zbXRwLWluLmwuZ29vZ2xlLmNvbYIdYWx0My5nbXItc210cC1pbi5sLmdvb2dsZS5jb22CHWFsdDQuZ21yLXNtdHAtaW4ubC5nb29nbGUuY29tgg1teDEuc210cC5nb29ngg1teDIuc210cC5nb29ngg1teDMuc210cC5nb29ngg1teDQuc210cC5nb29nghVhc3BteDIuZ29vZ2xlbWFpbC5jb22CFWFzcG14My5nb29nbGVtYWlsLmNvbYIVYXNwbXg0Lmdvb2dsZW1haWwuY29tghVhc3BteDUuZ29vZ2xlbWFpbC5jb22CEWdtci1teC5nb29nbGUuY29tMCEGA1UdIAQaMBgwCAYGZ4EMAQIBMAwGCisGAQQB1nkCBQMwPAYDVR0fBDUwMzAxoC+gLYYraHR0cDovL2NybHMucGtpLmdvb2cvZ3RzMWMzL2ZWSnhiVi1LdG1rLmNybDCCAQUGCisGAQQB1nkCBAIEgfYEgfMA8QB3ACl5vvCeOTkh8FZzn2Old+W+V32cYAr4+U1dJlwlXceEAAABfP2/e7QAAAQDAEgwRgIhANP/LWJbK/6Q2oroxHLg+gQ9f5HfknTvozmO061+uAY6AiEAybiQl3YEaNq+RuPLOZe5kfVcusOFTplpjl2DerWDpBcAdgBByMqx3yJGShDGoToJQodeTjGLGwPr60vHaPCQYpYG9gAAAXz9v3ybAAAEAwBHMEUCIQDI8cwvR3yvnSp4lOKMtU7BdkkCodgU80HQBN8lb1E3hgIgZIvubhACYgQj9tSs/hJt0LU5gAWjZ4AgAgLqxKjcy7kwDQYJKoZIhvcNAQELBQADggEBAMZ4JcJFQcZWCVS8IQvQY1OQIUixEN/5e15gMwq9vA2ba5faa7j/Bw6rWDcFhxOaG6L4jkXBy5DUPXxmR95XcsU8/WyyNZG2OMNiWFsfzroo8tz+rmJManK/FVXBaFbaMkm7p4/rbRpe5rUTAvhwUUsheB9YMkPsPR7h2lAb4KmNWFFBfcYQDElLMnXB53dahJtdSZngHRzIhbWGQfcMivbiANUlSX1a+u6zPT1kyBq8KjM+jO17nydO+SZPffVwNeC5P5Tc0+TX1M5oXK7NCNxEsdxD2OIFSPxpfxeMXFRHLLq5nIgVgnX2n0Xo3vcynu/q5WXXGH6zI+zxAoRSU4c="",
        ""1E7EF647CBA150281C60897257102878C4BD8CDC"": ""MIIFljCCA36gAwIBAgINAgO8U1lrNMcY9QFQZjANBgkqhkiG9w0BAQsFADBHMQswCQYDVQQGEwJVUzEiMCAGA1UEChMZR29vZ2xlIFRydXN0IFNlcnZpY2VzIExMQzEUMBIGA1UEAxMLR1RTIFJvb3QgUjEwHhcNMjAwODEzMDAwMDQyWhcNMjcwOTMwMDAwMDQyWjBGMQswCQYDVQQGEwJVUzEiMCAGA1UEChMZR29vZ2xlIFRydXN0IFNlcnZpY2VzIExMQzETMBEGA1UEAxMKR1RTIENBIDFDMzCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAPWI3+dijB43+DdCkH9sh9D7ZYIl/ejLa6T/belaI+KZ9hzpkgOZE3wJCor6QtZeViSqejOEH9Hpabu5dOxXTGZok3c3VVP+ORBNtzS7XyV3NzsXlOo85Z3VvMO0Q+sup0fvsEQRY9i0QYXdQTBIkxu/t/bgRQIh4JZCF8/ZK2VWNAcmBA2o/X3KLu/qSHw3TT8An4Pf73WELnlXXPxXbhqW//yMmqaZviXZf5YsBvcRKgKAgOtjGDxQSYflispfGStZloEAoPtR28p3CwvJlk/vcEnHXG0g/Zm0tOLKLnf9LdwLtmsTDIwZKxeWmLnwi/agJ7u2441Rj72ux5uxiZ0CAwEAAaOCAYAwggF8MA4GA1UdDwEB/wQEAwIBhjAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwEgYDVR0TAQH/BAgwBgEB/wIBADAdBgNVHQ4EFgQUinR/r4XN7pXNPZzQ4kYU83E1HScwHwYDVR0jBBgwFoAU5K8rJnEaK0gnhS9SZizv8IkTcT4waAYIKwYBBQUHAQEEXDBaMCYGCCsGAQUFBzABhhpodHRwOi8vb2NzcC5wa2kuZ29vZy9ndHNyMTAwBggrBgEFBQcwAoYkaHR0cDovL3BraS5nb29nL3JlcG8vY2VydHMvZ3RzcjEuZGVyMDQGA1UdHwQtMCswKaAnoCWGI2h0dHA6Ly9jcmwucGtpLmdvb2cvZ3RzcjEvZ3RzcjEuY3JsMFcGA1UdIARQME4wOAYKKwYBBAHWeQIFAzAqMCgGCCsGAQUFBwIBFhxodHRwczovL3BraS5nb29nL3JlcG9zaXRvcnkvMAgGBmeBDAECATAIBgZngQwBAgIwDQYJKoZIhvcNAQELBQADggIBAIl9rCBcDDy+mqhXlRu0rvqrpXJxtDaV/d9AEQNMwkYUuxQkq/BQcSLbrcRuf8/xam/IgxvYzolfh2yHuKkMo5uhYpSTld9brmYZCwKWnvy15xBpPnrLRklfRuFBsdeYTWU0AIAaP0+fbH9JAIFTQaSSIYKCGvGjRFsqUBITTcFTNvNCCK9U+o53UxtkOCcXCb1YyRt8OS1b887U7ZfbFAO/CVMkH8IMBHmYJvJh8VNS/UKMG2YrPxWhu//2m+OBmgEGcYk1KCTd4b3rGS3hSMs9WYNRtHTGnXzGsYZbr8w0xNPM1IERlQCh9BIiAfq0g3GvjLeMcySsN1PCAJA/Ef5c7TaUEDu9Ka7ixzpiO2xj2YC/WXGsYye5TBeg2vZzFb8q3o/zpWwygTMD0IZRcZk0upONXbVRWPeyk+gB9lm+cZv9TSjOz23HFtz30dZGm6fKa+l3D/2gthsjgx0QGtkJAITgRNOidSOzNIb2ILCkXhAd4FJGAJ2xDx8hcFH1mt0G/FX0Kw4zd8NLQsLxdxP8c4CU6x+7Nz/OAipmsHMdMqUybDKwjuDEI/9bfU1lcKwrmz3O2+BtjjKAvpafkmO8l7tdufThcV4q5O8DIrGKZTqPwJNl1IXNDw9bg1kWRxYtnCQ6yICmJhSFm/Y3m6xv+cXDBlHz4n/FsRC6UfTd""
    },
    ""Id"": ""173.194.79.27"",
    ""Hostnames"": [""aspmx.l.google.com""],
    ""CorrelationId"": null,
    ""CausationId"": null,
    ""Type"": null,
    ""MessageId"": null,
    ""Timestamp"": ""0001-01-01T00:00:00""
}";

        const string ResultsWithFailure = @"
{
    ""SimplifiedTlsConnectionResults"": [
        {
            ""TestName"": ""Tls13Rule"",
            ""CipherSuite"": """",
            ""CertificateThumbprints"": [],
            ""Error"": ""HANDSHAKE_FAILURE"",
            ""ErrorDescription"": ""handshake_failure(40)""
        },
        {
            ""TestName"": ""Tls12GoodCiphersRule"",
            ""CipherSuite"": ""TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384"",
            ""CertificateThumbprints"": [
                ""56BD74636CBAF92BEA8424B904819E1AF7BD95F3"",
                ""4C27431717565A3A07F3E6D0032C4258949CF9EC""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        },
        {
            ""TestName"": ""Tls12ServerPreferenceRule"",
            ""CipherSuite"": ""TLS_RSA_WITH_RC4_128_MD5"",
            ""CertificateThumbprints"": [
                ""56BD74636CBAF92BEA8424B904819E1AF7BD95F3"",
                ""4C27431717565A3A07F3E6D0032C4258949CF9EC""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        }
    ],
    ""Certificates"": {
        ""56BD74636CBAF92BEA8424B904819E1AF7BD95F3"": ""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"",
        ""4C27431717565A3A07F3E6D0032C4258949CF9EC"": ""MIIETTCCAzWgAwIBAgILBAAAAAABRE7wNjEwDQYJKoZIhvcNAQELBQAwVzELMAkGA1UEBhMCQkUxGTAXBgNVBAoTEEdsb2JhbFNpZ24gbnYtc2ExEDAOBgNVBAsTB1Jvb3QgQ0ExGzAZBgNVBAMTEkdsb2JhbFNpZ24gUm9vdCBDQTAeFw0xNDAyMjAxMDAwMDBaFw0yNDAyMjAxMDAwMDBaMEwxCzAJBgNVBAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMSIwIAYDVQQDExlBbHBoYVNTTCBDQSAtIFNIQTI1NiAtIEcyMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2gHs5OxzYPt+j2q3xhfjkmQy1KwA2aIPue3ua4qGypJn2XTXXUcCPI9A1p5tFM3D2ik5pw8FCmiiZhoexLKLdljlq10dj0CzOYvvHoN9ItDjqQAu7FPPYhmFRChMwCfLew7sEGQAEKQFzKByvkFsMVtI5LHsuSPrVU3QfWJKpbSlpFmFxSWRpv6mCZ8GEG2PgQxkQF5zAJrgLmWYVBAAcJjI4e00X9icxw3A1iNZRfz+VXqG7pRgIvGu0eZVRvaZxRsIdF+ssGSEj4k4HKGnkCFPAm694GFn1PhChw8K98kEbSqpL+9Cpd/do1PbmB6B+Zpye1reTz5/olig4hetZwIDAQABo4IBIzCCAR8wDgYDVR0PAQH/BAQDAgEGMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYDVR0OBBYEFPXN1TwIUPlqTzq3l9pWg+Zp0mj3MEUGA1UdIAQ+MDwwOgYEVR0gADAyMDAGCCsGAQUFBwIBFiRodHRwczovL3d3dy5hbHBoYXNzbC5jb20vcmVwb3NpdG9yeS8wMwYDVR0fBCwwKjAooCagJIYiaHR0cDovL2NybC5nbG9iYWxzaWduLm5ldC9yb290LmNybDA9BggrBgEFBQcBAQQxMC8wLQYIKwYBBQUHMAGGIWh0dHA6Ly9vY3NwLmdsb2JhbHNpZ24uY29tL3Jvb3RyMTAfBgNVHSMEGDAWgBRge2YaRQ2XyolQL30EzTSo//z9SzANBgkqhkiG9w0BAQsFAAOCAQEAYEBoFkfnFo3bXKFWKsv0XJuwHqJL9csCP/gLofKnQtS3TOvjZoDzJUN4LhsXVgdSGMvRqOzm+3M+pGKMgLTSxRJzo9P6Aji+Yz2EuJnB8br3n8NA0VgYU8Fi3a8YQn80TsVD1XGwMADH45CuP1eGl87qDBKOInDjZqdUfy4oy9RU0LMeYmcI+Sfhy+NmuCQbiWqJRGXy2UzSWByMTsCVodTvZy84IOgu/5ZR8LrYPZJwR2UcnnNytGAMXOLRc3bgr07i5TelRS+KIz6HxzDmMTh89N1SyvNTBCVXVmaU6Avu5gMUTu79bZRknl7OedSyps9AsUSoPocZXun4IRZZUw==""
    },
    ""Id"": ""173.194.79.27"",
    ""Hostnames"": [""aspmx.l.google.com""]
}
";

        const string MultiChainResult = @"
{
    ""SimplifiedTlsConnectionResults"": [
        {
            ""TestName"": ""Tls13Rule"",
            ""CipherSuite"": ""TLS_AES_256_GCM_SHA384"",
            ""CertificateThumbprints"": [
                ""A5450389594342DB19BCC9E4E6F9F0755FD1A173"",
                ""1E7EF647CBA150281C60897257102878C4BD8CDC"",
                ""08745487E891C19E3078C1F2A07E452950EF36F6""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        },
        {
            ""TestName"": ""Tls12ServerPreferenceRule"",
            ""CipherSuite"": ""TLS_RSA_WITH_RC4_128_MD5"",
            ""CertificateThumbprints"": [
                ""56BD74636CBAF92BEA8424B904819E1AF7BD95F3"",
                ""4C27431717565A3A07F3E6D0032C4258949CF9EC""
            ],
            ""Error"": null,
            ""ErrorDescription"": null
        }
    ],
    ""Certificates"": {
        ""08745487E891C19E3078C1F2A07E452950EF36F6"": ""MIIFYjCCBEqgAwIBAgIQd70NbNs2+RrqIQ/E8FjTDTANBgkqhkiG9w0BAQsFADBXMQswCQYDVQQGEwJCRTEZMBcGA1UEChMQR2xvYmFsU2lnbiBudi1zYTEQMA4GA1UECxMHUm9vdCBDQTEbMBkGA1UEAxMSR2xvYmFsU2lnbiBSb290IENBMB4XDTIwMDYxOTAwMDA0MloXDTI4MDEyODAwMDA0MlowRzELMAkGA1UEBhMCVVMxIjAgBgNVBAoTGUdvb2dsZSBUcnVzdCBTZXJ2aWNlcyBMTEMxFDASBgNVBAMTC0dUUyBSb290IFIxMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAthECix7joXebO9y/lD63ladAPKH9gvl9MgaCcfb2jH/76Nu8ai6Xl6OMS/kr9rH5zoQdsfnFl97vufKj6bwSiV6nqlKr+CMny6SxnGPb15l+8Ape62im9MZaRw1NEDPjTrETo8gYbEvs/AmQ351kKSUjB6G00j0uYODP0gmHu81I8E3CwnqIiru6z1kZ1q+PsAewnjHxgsHA3y6mbWwZDrXYfiYaRQM9sHmklCitD38m5agI/pboPGiUU+6DOogrFZYJsuB6jC511pzrp1Zkj5ZPaK49l8KEj8C8QMALXL32h7M1bKwYUH+E4EzNktMg6TO8UpmvMrUpsyUqtEj5cuHKZPfmghCN6J3Cioj6OGaK/GP5Afl4/Xtcd/p2h/rs37EOeZVXtL0m79YB0esWCruOC7XFxYpVq9Os6pFLKcwZpDIlTirxZUTQAs6qzkm06p98g7BAe+dDq6dso499iYH6TKX/1Y7DzkvgtdizjkXPdsDtQCv9Uw+wp9U7DbGKogPeMa3Md+pvez7W35EiEua++tgy/BBjFFFy3l3WFpO9KWgz7zpm7AeKJt8T11dleCfeXkkUAKIAf5qoIbapsZWwpbkNFhHax2xIPEDgfg1azVY80ZcFuctL7TlLnMQ/0lUTbiSw1nH69MG6zO0b9f6BQdgAmD06yK56mDcYBZUCAwEAAaOCATgwggE0MA4GA1UdDwEB/wQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTkrysmcRorSCeFL1JmLO/wiRNxPjAfBgNVHSMEGDAWgBRge2YaRQ2XyolQL30EzTSo//z9SzBgBggrBgEFBQcBAQRUMFIwJQYIKwYBBQUHMAGGGWh0dHA6Ly9vY3NwLnBraS5nb29nL2dzcjEwKQYIKwYBBQUHMAKGHWh0dHA6Ly9wa2kuZ29vZy9nc3IxL2dzcjEuY3J0MDIGA1UdHwQrMCkwJ6AloCOGIWh0dHA6Ly9jcmwucGtpLmdvb2cvZ3NyMS9nc3IxLmNybDA7BgNVHSAENDAyMAgGBmeBDAECATAIBgZngQwBAgIwDQYLKwYBBAHWeQIFAwIwDQYLKwYBBAHWeQIFAwMwDQYJKoZIhvcNAQELBQADggEBADSkHrEoo9C0dhemMXoh6dFSPsjbdBZBiLg9NR3t5P+T4Vxfq7vqfM/b5A3Ri1fyJm9bvhdGaJQ3b2t6yMAYN/olUazsaL+yyEn9WprKASOshIArAoyZl+tJaox118fessmXn1hIVw41oeQa1v1vg4Fv74zPl6/AhSrw9U5pCZEt4Wi4wStz6dTZ/CLANx8LZh1J7QJVj2fhMtfTJr9w4z30Z209fOU0iOMy+qduBmpvvYuR7hZL6Dupszfnw0Skfths18dG9ZKb59UhvmaSGZRVbNQpsg3BZlvid0lIKO2d1xozclOzgjXPYovJJIultzkMu34qQb9Sz/yilrbCgj8="",
        ""A5450389594342DB19BCC9E4E6F9F0755FD1A173"": ""MIIG9zCCBd+gAwIBAgIRAI3R7wF8yk7nCgAAAAEiTccwDQYJKoZIhvcNAQELBQAwRjELMAkGA1UEBhMCVVMxIjAgBgNVBAoTGUdvb2dsZSBUcnVzdCBTZXJ2aWNlcyBMTEMxEzARBgNVBAMTCkdUUyBDQSAxQzMwHhcNMjExMTA4MDMxMjMzWhcNMjIwMTMxMDMxMjMyWjAYMRYwFAYDVQQDEw1teC5nb29nbGUuY29tMFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAENC6zq4ikKXIOR5VPaoRQ0v1BAB553hJY93f6qlbY/wP4uIW01XWDuw23ppHJJpcRWSyo5KTbgPIm7RXGXal2NaOCBNcwggTTMA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAKBggrBgEFBQcDATAMBgNVHRMBAf8EAjAAMB0GA1UdDgQWBBTfVI2uP6SiUWXLD3PFzw7KWvMnrjAfBgNVHSMEGDAWgBSKdH+vhc3ulc09nNDiRhTzcTUdJzBqBggrBgEFBQcBAQReMFwwJwYIKwYBBQUHMAGGG2h0dHA6Ly9vY3NwLnBraS5nb29nL2d0czFjMzAxBggrBgEFBQcwAoYlaHR0cDovL3BraS5nb29nL3JlcG8vY2VydHMvZ3RzMWMzLmRlcjCCAoYGA1UdEQSCAn0wggJ5gg1teC5nb29nbGUuY29tgg9zbXRwLmdvb2dsZS5jb22CEmFzcG14LmwuZ29vZ2xlLmNvbYIXYWx0MS5hc3BteC5sLmdvb2dsZS5jb22CF2FsdDIuYXNwbXgubC5nb29nbGUuY29tghdhbHQzLmFzcG14LmwuZ29vZ2xlLmNvbYIXYWx0NC5hc3BteC5sLmdvb2dsZS5jb22CGmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQxLmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQyLmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQzLmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tgh9hbHQ0LmdtYWlsLXNtdHAtaW4ubC5nb29nbGUuY29tghhnbXItc210cC1pbi5sLmdvb2dsZS5jb22CHWFsdDEuZ21yLXNtdHAtaW4ubC5nb29nbGUuY29tgh1hbHQyLmdtci1zbXRwLWluLmwuZ29vZ2xlLmNvbYIdYWx0My5nbXItc210cC1pbi5sLmdvb2dsZS5jb22CHWFsdDQuZ21yLXNtdHAtaW4ubC5nb29nbGUuY29tgg1teDEuc210cC5nb29ngg1teDIuc210cC5nb29ngg1teDMuc210cC5nb29ngg1teDQuc210cC5nb29nghVhc3BteDIuZ29vZ2xlbWFpbC5jb22CFWFzcG14My5nb29nbGVtYWlsLmNvbYIVYXNwbXg0Lmdvb2dsZW1haWwuY29tghVhc3BteDUuZ29vZ2xlbWFpbC5jb22CEWdtci1teC5nb29nbGUuY29tMCEGA1UdIAQaMBgwCAYGZ4EMAQIBMAwGCisGAQQB1nkCBQMwPAYDVR0fBDUwMzAxoC+gLYYraHR0cDovL2NybHMucGtpLmdvb2cvZ3RzMWMzL2ZWSnhiVi1LdG1rLmNybDCCAQUGCisGAQQB1nkCBAIEgfYEgfMA8QB3ACl5vvCeOTkh8FZzn2Old+W+V32cYAr4+U1dJlwlXceEAAABfP2/e7QAAAQDAEgwRgIhANP/LWJbK/6Q2oroxHLg+gQ9f5HfknTvozmO061+uAY6AiEAybiQl3YEaNq+RuPLOZe5kfVcusOFTplpjl2DerWDpBcAdgBByMqx3yJGShDGoToJQodeTjGLGwPr60vHaPCQYpYG9gAAAXz9v3ybAAAEAwBHMEUCIQDI8cwvR3yvnSp4lOKMtU7BdkkCodgU80HQBN8lb1E3hgIgZIvubhACYgQj9tSs/hJt0LU5gAWjZ4AgAgLqxKjcy7kwDQYJKoZIhvcNAQELBQADggEBAMZ4JcJFQcZWCVS8IQvQY1OQIUixEN/5e15gMwq9vA2ba5faa7j/Bw6rWDcFhxOaG6L4jkXBy5DUPXxmR95XcsU8/WyyNZG2OMNiWFsfzroo8tz+rmJManK/FVXBaFbaMkm7p4/rbRpe5rUTAvhwUUsheB9YMkPsPR7h2lAb4KmNWFFBfcYQDElLMnXB53dahJtdSZngHRzIhbWGQfcMivbiANUlSX1a+u6zPT1kyBq8KjM+jO17nydO+SZPffVwNeC5P5Tc0+TX1M5oXK7NCNxEsdxD2OIFSPxpfxeMXFRHLLq5nIgVgnX2n0Xo3vcynu/q5WXXGH6zI+zxAoRSU4c="",
        ""1E7EF647CBA150281C60897257102878C4BD8CDC"": ""MIIFljCCA36gAwIBAgINAgO8U1lrNMcY9QFQZjANBgkqhkiG9w0BAQsFADBHMQswCQYDVQQGEwJVUzEiMCAGA1UEChMZR29vZ2xlIFRydXN0IFNlcnZpY2VzIExMQzEUMBIGA1UEAxMLR1RTIFJvb3QgUjEwHhcNMjAwODEzMDAwMDQyWhcNMjcwOTMwMDAwMDQyWjBGMQswCQYDVQQGEwJVUzEiMCAGA1UEChMZR29vZ2xlIFRydXN0IFNlcnZpY2VzIExMQzETMBEGA1UEAxMKR1RTIENBIDFDMzCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAPWI3+dijB43+DdCkH9sh9D7ZYIl/ejLa6T/belaI+KZ9hzpkgOZE3wJCor6QtZeViSqejOEH9Hpabu5dOxXTGZok3c3VVP+ORBNtzS7XyV3NzsXlOo85Z3VvMO0Q+sup0fvsEQRY9i0QYXdQTBIkxu/t/bgRQIh4JZCF8/ZK2VWNAcmBA2o/X3KLu/qSHw3TT8An4Pf73WELnlXXPxXbhqW//yMmqaZviXZf5YsBvcRKgKAgOtjGDxQSYflispfGStZloEAoPtR28p3CwvJlk/vcEnHXG0g/Zm0tOLKLnf9LdwLtmsTDIwZKxeWmLnwi/agJ7u2441Rj72ux5uxiZ0CAwEAAaOCAYAwggF8MA4GA1UdDwEB/wQEAwIBhjAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwEgYDVR0TAQH/BAgwBgEB/wIBADAdBgNVHQ4EFgQUinR/r4XN7pXNPZzQ4kYU83E1HScwHwYDVR0jBBgwFoAU5K8rJnEaK0gnhS9SZizv8IkTcT4waAYIKwYBBQUHAQEEXDBaMCYGCCsGAQUFBzABhhpodHRwOi8vb2NzcC5wa2kuZ29vZy9ndHNyMTAwBggrBgEFBQcwAoYkaHR0cDovL3BraS5nb29nL3JlcG8vY2VydHMvZ3RzcjEuZGVyMDQGA1UdHwQtMCswKaAnoCWGI2h0dHA6Ly9jcmwucGtpLmdvb2cvZ3RzcjEvZ3RzcjEuY3JsMFcGA1UdIARQME4wOAYKKwYBBAHWeQIFAzAqMCgGCCsGAQUFBwIBFhxodHRwczovL3BraS5nb29nL3JlcG9zaXRvcnkvMAgGBmeBDAECATAIBgZngQwBAgIwDQYJKoZIhvcNAQELBQADggIBAIl9rCBcDDy+mqhXlRu0rvqrpXJxtDaV/d9AEQNMwkYUuxQkq/BQcSLbrcRuf8/xam/IgxvYzolfh2yHuKkMo5uhYpSTld9brmYZCwKWnvy15xBpPnrLRklfRuFBsdeYTWU0AIAaP0+fbH9JAIFTQaSSIYKCGvGjRFsqUBITTcFTNvNCCK9U+o53UxtkOCcXCb1YyRt8OS1b887U7ZfbFAO/CVMkH8IMBHmYJvJh8VNS/UKMG2YrPxWhu//2m+OBmgEGcYk1KCTd4b3rGS3hSMs9WYNRtHTGnXzGsYZbr8w0xNPM1IERlQCh9BIiAfq0g3GvjLeMcySsN1PCAJA/Ef5c7TaUEDu9Ka7ixzpiO2xj2YC/WXGsYye5TBeg2vZzFb8q3o/zpWwygTMD0IZRcZk0upONXbVRWPeyk+gB9lm+cZv9TSjOz23HFtz30dZGm6fKa+l3D/2gthsjgx0QGtkJAITgRNOidSOzNIb2ILCkXhAd4FJGAJ2xDx8hcFH1mt0G/FX0Kw4zd8NLQsLxdxP8c4CU6x+7Nz/OAipmsHMdMqUybDKwjuDEI/9bfU1lcKwrmz3O2+BtjjKAvpafkmO8l7tdufThcV4q5O8DIrGKZTqPwJNl1IXNDw9bg1kWRxYtnCQ6yICmJhSFm/Y3m6xv+cXDBlHz4n/FsRC6UfTd"",
        ""56BD74636CBAF92BEA8424B904819E1AF7BD95F3"": ""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"",
        ""4C27431717565A3A07F3E6D0032C4258949CF9EC"": ""MIIETTCCAzWgAwIBAgILBAAAAAABRE7wNjEwDQYJKoZIhvcNAQELBQAwVzELMAkGA1UEBhMCQkUxGTAXBgNVBAoTEEdsb2JhbFNpZ24gbnYtc2ExEDAOBgNVBAsTB1Jvb3QgQ0ExGzAZBgNVBAMTEkdsb2JhbFNpZ24gUm9vdCBDQTAeFw0xNDAyMjAxMDAwMDBaFw0yNDAyMjAxMDAwMDBaMEwxCzAJBgNVBAYTAkJFMRkwFwYDVQQKExBHbG9iYWxTaWduIG52LXNhMSIwIAYDVQQDExlBbHBoYVNTTCBDQSAtIFNIQTI1NiAtIEcyMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA2gHs5OxzYPt+j2q3xhfjkmQy1KwA2aIPue3ua4qGypJn2XTXXUcCPI9A1p5tFM3D2ik5pw8FCmiiZhoexLKLdljlq10dj0CzOYvvHoN9ItDjqQAu7FPPYhmFRChMwCfLew7sEGQAEKQFzKByvkFsMVtI5LHsuSPrVU3QfWJKpbSlpFmFxSWRpv6mCZ8GEG2PgQxkQF5zAJrgLmWYVBAAcJjI4e00X9icxw3A1iNZRfz+VXqG7pRgIvGu0eZVRvaZxRsIdF+ssGSEj4k4HKGnkCFPAm694GFn1PhChw8K98kEbSqpL+9Cpd/do1PbmB6B+Zpye1reTz5/olig4hetZwIDAQABo4IBIzCCAR8wDgYDVR0PAQH/BAQDAgEGMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYDVR0OBBYEFPXN1TwIUPlqTzq3l9pWg+Zp0mj3MEUGA1UdIAQ+MDwwOgYEVR0gADAyMDAGCCsGAQUFBwIBFiRodHRwczovL3d3dy5hbHBoYXNzbC5jb20vcmVwb3NpdG9yeS8wMwYDVR0fBCwwKjAooCagJIYiaHR0cDovL2NybC5nbG9iYWxzaWduLm5ldC9yb290LmNybDA9BggrBgEFBQcBAQQxMC8wLQYIKwYBBQUHMAGGIWh0dHA6Ly9vY3NwLmdsb2JhbHNpZ24uY29tL3Jvb3RyMTAfBgNVHSMEGDAWgBRge2YaRQ2XyolQL30EzTSo//z9SzANBgkqhkiG9w0BAQsFAAOCAQEAYEBoFkfnFo3bXKFWKsv0XJuwHqJL9csCP/gLofKnQtS3TOvjZoDzJUN4LhsXVgdSGMvRqOzm+3M+pGKMgLTSxRJzo9P6Aji+Yz2EuJnB8br3n8NA0VgYU8Fi3a8YQn80TsVD1XGwMADH45CuP1eGl87qDBKOInDjZqdUfy4oy9RU0LMeYmcI+Sfhy+NmuCQbiWqJRGXy2UzSWByMTsCVodTvZy84IOgu/5ZR8LrYPZJwR2UcnnNytGAMXOLRc3bgr07i5TelRS+KIz6HxzDmMTh89N1SyvNTBCVXVmaU6Avu5gMUTu79bZRknl7OedSyps9AsUSoPocZXun4IRZZUw==""
    },
    ""Id"": ""173.194.79.27"",
    ""Hostnames"": [""aspmx.l.google.com""]
}
";
    }
}
