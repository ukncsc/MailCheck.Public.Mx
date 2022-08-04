using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Api.Domain;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Simplified;
using NUnit.Framework;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;


namespace MailCheck.Mx.Api.Test.Domain
{
    [TestFixture]
    public class DomainTlsEvaluatorResultsFactoryTests
    {
        private static readonly Dictionary<string, int> EmptyPrefs = new Dictionary<string, int>();

        private DomainTlsEvaluatorResultsFactory _domainTlsEvaluatorResultsFactory;

        [SetUp]
        public void SetUp()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();
        }

        [Test]
        public void CreateMapsProperties()
        {
            var source = new SimplifiedTlsEntityState("testHostName", "testIpAddress1")
            {
                CertAdvisories = null,
                Certificates = null,
                TlsAdvisories = null,
                TlsLastUpdated = null,
                CertsLastUpdated = null,
            };

            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", new Dictionary<string, int> { { "testHostName", 789 } }, new List<SimplifiedTlsEntityState> { source });

            Assert.AreEqual(1, result.CertificateResults.Count);
            Assert.AreEqual("testHostName", result.CertificateResults[0].HostName);
            Assert.AreEqual(DateTime.MinValue, result.CertificateResults[0].LastChecked);
            Assert.AreEqual(789, result.CertificateResults[0].Preference);
            Assert.IsNull(result.CertificateResults[0].Certificates);

            Assert.AreEqual(1, result.MxTlsEvaluatorResults.Count);
            Assert.AreEqual("testHostName", result.MxTlsEvaluatorResults[0].Hostname);
            Assert.AreEqual(DateTime.MinValue, result.MxTlsEvaluatorResults[0].LastChecked);
            Assert.That(result.MxTlsEvaluatorResults[0].Failures, Is.Empty);
            Assert.That(result.MxTlsEvaluatorResults[0].Informationals, Is.Empty);
            Assert.That(result.MxTlsEvaluatorResults[0].Positives, Is.Empty);
            Assert.That(result.MxTlsEvaluatorResults[0].Warnings, Is.Empty);

            Assert.AreEqual(false, result.Pending);
            Assert.AreEqual("testDomain", result.Id);
        }

        [Test]
        public void CreateMapsTlsProperties()
        {
            var source = new SimplifiedTlsEntityState("testHostName", "testIpAddress1")
            {
                TlsAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-00000000000A"), "mailcheck.tls.testname1", MessageType.error, "A", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-00000000000B"), "mailcheck.tls.testname2", MessageType.info, "B", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-00000000000C"), "mailcheck.tls.testname3", MessageType.success, "C", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-00000000000D"), "mailcheck.tls.testname4", MessageType.warning, "D", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-00000000000E"), "mailcheck.tls.testname5", MessageType.warning, "E", null),
                },
                TlsLastUpdated = new DateTime(2000, 01, 01)
            };

            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, new List<SimplifiedTlsEntityState> { source });

            Assert.AreEqual("testDomain", result.Id);
            Assert.AreEqual(1, result.MxTlsEvaluatorResults.Count);
            Assert.AreEqual(new DateTime(2000, 01, 01), result.MxTlsEvaluatorResults[0].LastChecked);
            Assert.AreEqual("A", result.MxTlsEvaluatorResults[0].Failures[0]);
            Assert.AreEqual("B", result.MxTlsEvaluatorResults[0].Informationals[0]);
            Assert.AreEqual("C", result.MxTlsEvaluatorResults[0].Positives[0]);
            Assert.AreEqual("D", result.MxTlsEvaluatorResults[0].Warnings[0]);
            Assert.AreEqual("E", result.MxTlsEvaluatorResults[0].Warnings[1]);
        }

        [Test]
        public void CreateGroupsTlsPropertiesByHostname()
        {
            var source1 = new SimplifiedTlsEntityState("testHostName1", "testIpAddress1")
            {
                TlsAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000001"), "mailcheck.tls.testname1", MessageType.error, "A", null),
                },
                TlsLastUpdated = new DateTime(2000, 01, 01)
            };
            var source2 = new SimplifiedTlsEntityState("testHostName1", "testIpAddress2")
            {
                TlsAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000002"), "mailcheck.tls.testname2", MessageType.error, "B", null)
                },
                TlsLastUpdated = new DateTime(2001, 01, 01)

            };
            var source3 = new SimplifiedTlsEntityState("testHostName2", "testIpAddress3")
            {
                TlsAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000003"), "mailcheck.tls.testname3", MessageType.error, "C", null)
                },
                TlsLastUpdated = new DateTime(2002, 01, 01)

            };
            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, new List<SimplifiedTlsEntityState> { source1, source2, source3 });

            Assert.AreEqual("testDomain", result.Id);

            Assert.AreEqual(2, result.MxTlsEvaluatorResults.Count);

            Assert.AreEqual("testHostName1", result.MxTlsEvaluatorResults[0].Hostname);
            Assert.AreEqual(2, result.MxTlsEvaluatorResults[0].Failures.Count);
            Assert.AreEqual(new DateTime(2001, 01, 01), result.MxTlsEvaluatorResults[0].LastChecked);

            Assert.AreEqual("testHostName2", result.MxTlsEvaluatorResults[1].Hostname);
            Assert.AreEqual(1, result.MxTlsEvaluatorResults[1].Failures.Count);
            Assert.AreEqual(new DateTime(2002, 01, 01), result.MxTlsEvaluatorResults[1].LastChecked);
        }

        [Test]
        public void CreateMapsCertAdvisories()
        {
            var source = new SimplifiedTlsEntityState("testHostName", "testIpAddress")
            {
                CertAdvisories = new List<NamedAdvisory>{
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000001"), "mailcheck.tls.testname1", MessageType.error, "E", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000002"), "mailcheck.tls.testname2", MessageType.info, "F", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000003"), "mailcheck.tls.testname3", MessageType.success, "G", null),
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000004"), "mailcheck.tls.testname4", MessageType.warning, "H", null),
                },
                CertsLastUpdated = new DateTime(2000, 01, 01)

            };
            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, new List<SimplifiedTlsEntityState> { source });

            Assert.AreEqual("testDomain", result.Id);
            Assert.AreEqual(1, result.CertificateResults.Count);
            Assert.AreEqual(new DateTime(2000, 01, 01), result.CertificateResults[0].LastChecked);
            Assert.AreEqual(2, result.CertificateResults[0].Errors.Count);
            Assert.AreEqual("H", result.CertificateResults[0].Errors[0].Message);
            Assert.AreEqual(ErrorType.Warning, result.CertificateResults[0].Errors[0].ErrorType);
            Assert.AreEqual("E", result.CertificateResults[0].Errors[1].Message);
            Assert.AreEqual(ErrorType.Error, result.CertificateResults[0].Errors[1].ErrorType);
        }

        [Test]
        public void CreateGroupsCertAdvisoriesByHostName()
        {
            var source1 = new SimplifiedTlsEntityState("testHostName1", "testIpAddress")
            {
                CertAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000001"), "mailcheck.tls.testname1", MessageType.error, "A", null)
                },
                CertsLastUpdated = new DateTime(2000, 01, 01)
            };
            var source2 = new SimplifiedTlsEntityState("testHostName1", "testIpAddress")
            {
                CertAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000002"), "mailcheck.tls.testname2", MessageType.error, "B", null)
                },
                CertsLastUpdated = new DateTime(2001, 01, 01)
            };
            var source3 = new SimplifiedTlsEntityState("testHostName2", "testIpAddress")
            {
                CertAdvisories = new List<NamedAdvisory>
                {
                    new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000003"), "mailcheck.tls.testname3", MessageType.error, "C", null)
                },
                CertsLastUpdated = new DateTime(2002, 01, 01)
            };
            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, new List<SimplifiedTlsEntityState> { source1, source2, source3 });

            Assert.AreEqual("testDomain", result.Id);
            Assert.AreEqual(2, result.CertificateResults.Count);
            Assert.AreEqual("testHostName1", result.CertificateResults[0].HostName);
            Assert.AreEqual(2, result.CertificateResults[0].Errors.Count);
            Assert.AreEqual(new DateTime(2001, 01, 01), result.CertificateResults[0].LastChecked);
            Assert.AreEqual("testHostName2", result.CertificateResults[1].HostName);
            Assert.AreEqual(1, result.CertificateResults[1].Errors.Count);
            Assert.AreEqual(new DateTime(2002, 01, 01), result.CertificateResults[1].LastChecked);
        }

        [Test]
        public void GetCertificate_ValidCert_ExpectedProperties()
        {
            var certString = @"MIIFMDCCBBigAwIBAgISAwMFsyNCDe42I9UxhB45nwHEMA0GCSqGSIb3DQEBCwUAMDIxCzAJBgNVBAYTAlVTMRYwFAYDVQQKEw1MZXQncyBFbmNyeXB0MQswCQYDVQQDEwJSMzAeFw0yMTEyMDcwMDA1MDVaFw0yMjAzMDcwMDA1MDRaMCExHzAdBgNVBAMMFioubWFpbC5zdGFydHRsc3Rlc3QudWswggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDPXbcGvSA8TEZrlRsfUDzrOEG6FlxErnTQhl2sFSRtb6+/UtaMbAsfwtZytseHQPENcPZsg/X1mSnn3e18PaJIbDVRW1+zTqVzC7hNMSFTGD3dNJ/RQwTtx3hC3DI2ccOWBZ8gAuNZrJU7zWPpaUjH+aZKat6ph7dLedYXEgSzD7j5DwWt9HrXCxJxMhmbQmzb29ZrmgfLpxy1SJR63s9AxnX9Ht4WAXTjJE8kHxIJuUMwjMx7wAyzZqWUmujJQZoUuSIipjQfqROrRcrkI9ejEa9MmdWw9LkLUOifquJQ7VtZqkccLuLCi/bpQZFfIJK1k7Vu6f4geX0QK6wsBf7tAgMBAAGjggJPMIICSzAOBgNVHQ8BAf8EBAMCBaAwHQYDVR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMAwGA1UdEwEB/wQCMAAwHQYDVR0OBBYEFJJSI7RLHsOPNX49RcMNC3Id9PKcMB8GA1UdIwQYMBaAFBQusxe3WFbLrlAJQOYfr52LFMLGMFUGCCsGAQUFBwEBBEkwRzAhBggrBgEFBQcwAYYVaHR0cDovL3IzLm8ubGVuY3Iub3JnMCIGCCsGAQUFBzAChhZodHRwOi8vcjMuaS5sZW5jci5vcmcvMCEGA1UdEQQaMBiCFioubWFpbC5zdGFydHRsc3Rlc3QudWswTAYDVR0gBEUwQzAIBgZngQwBAgEwNwYLKwYBBAGC3xMBAQEwKDAmBggrBgEFBQcCARYaaHR0cDovL2Nwcy5sZXRzZW5jcnlwdC5vcmcwggECBgorBgEEAdZ5AgQCBIHzBIHwAO4AdQBByMqx3yJGShDGoToJQodeTjGLGwPr60vHaPCQYpYG9gAAAX2SbDlLAAAEAwBGMEQCIBFtFjsTiC2og89pdi2GcbP9JvR3J4PKYIdIGjycRblrAiA6DGTsIRiv++AbbR7RlmtS3rbbOdZ7ZW8LhPmLqjH7uQB1AEalVet1+pEgMLWiiWn0830RLEF0vv1JuIWr8vxw/m1HAAABfZJsOYQAAAQDAEYwRAIgdR5Zv+842GV5MLNHF/ulrpyNJfTIs4uY9cBAFonp4OsCIAWR5P/cKJUYTuR5fQgu9NNkPhCKMqBHTzuhxGhIkND7MA0GCSqGSIb3DQEBCwUAA4IBAQCU+Qo6D3ZcuRb4of8XIqBU3iCqUFYWyjwpRHsPfvklDMm9c/sbjyVIMSfww+LADe5iBPtzmotHAuIO7D3dr75eENS7F8rgYJCfkJxF/Aingk6/9Q4+CncfKUaXgD8pkbu4k8DAXE8l8B0X5EGzmYlbf5IpxYLz+blnvMhkkXK9EinDHa+rxVFNX4/slm2aqGv7BzyWFTK3AlFmLJJSCx/YYRYc0XdzsFx15IFbw1obxAK3skadX0fOypx8P3mQ4rHR4mnW7mxPIvrhI2apQeWmKsP1nChO0ZIt74EGQQEfEhUNJEfH0PwxikcMkCAJ+oMYhkiTu+sQnmuG/pFGg/eF";
            var cert = DomainTlsEvaluatorResultsFactory.GetCertificate(certString);
            Assert.That(cert.CommonName, Is.EqualTo("*.mail.starttlstest.uk"));
            Assert.That(cert.Issuer, Is.EqualTo("CN=R3, O=Let's Encrypt, C=US"));
            Assert.That(cert.KeyAlgoritm, Is.EqualTo("RSA"));
            Assert.That(cert.KeyLength, Is.EqualTo(2048));
            Assert.That(cert.SerialNumber, Is.EqualTo("030305B323420DEE3623D531841E399F01C4"));
            Assert.That(cert.Subject, Is.EqualTo("CN=*.mail.starttlstest.uk"));
            StringAssert.EndsWith("*.mail.starttlstest.uk", cert.SubjectAlternativeName);
            StringAssert.StartsWith("DNS", cert.SubjectAlternativeName);
            Assert.That(cert.ThumbPrint, Is.EqualTo("15063C9DF18C18E4FC9989660628DAD95C198FA1"));
            Assert.That(cert.ValidFrom, Is.EqualTo(DateTime.Parse("2021-12-07T00:05:05+00:00")));
            Assert.That(cert.ValidTo, Is.EqualTo(DateTime.Parse("2022-03-07T00:05:04+00:00")));
            Assert.That(cert.Version, Is.EqualTo("3"));
        }

        [Test]
        public void CreateDedupesAdvisories()
        {
            var source1 = new SimplifiedTlsEntityState("testdomain.co.uk", "1.2.3.4")
            {
                TlsAdvisories = new List<NamedAdvisory> { new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000001"), "mailcheck.tls.testname1", MessageType.error, "A", null) },
                CertAdvisories = new List<NamedAdvisory> { new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000002"), "mailcheck.tls.testname2", MessageType.error, "B", null) }
            };
            var source2 = new SimplifiedTlsEntityState("testdomain.co.uk", "1.2.3.4")
            {
                TlsAdvisories = new List<NamedAdvisory> { new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000001"), "mailcheck.tls.testname1", MessageType.error, "A", null) },
                CertAdvisories = new List<NamedAdvisory> { new NamedAdvisory(Guid.Parse("00000000-0000-0000-0000-000000000002"), "mailcheck.tls.testname2", MessageType.error, "B", null) }
            };
            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, new List<SimplifiedTlsEntityState> { source1, source2 });

            Assert.AreEqual("testDomain", result.Id);

            Assert.AreEqual(1, result.MxTlsEvaluatorResults.Count);
            Assert.AreEqual(1, result.CertificateResults.Count);
        }

        [Ignore("local run only")]
        //[Test]
        public void CreateMapsCertificates()
        {
            X509Certificate2 certificate = GenerateCertificate();

            var source = new SimplifiedTlsEntityState("testHostName", "testIpAddress")
            {
                Certificates = new Dictionary<string, string> { { certificate.Thumbprint, Convert.ToBase64String(certificate.RawData) } }
            };

            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, new List<SimplifiedTlsEntityState> { source });

            Assert.AreEqual(1, result.CertificateResults[0].Certificates.Count);
            var cert = result.CertificateResults[0].Certificates[0];
            Assert.AreEqual("subjectDN", cert.CommonName);
            Assert.AreEqual("CN=issuerDN", cert.Issuer);
            Assert.AreEqual("RSA", cert.KeyAlgoritm);
            Assert.AreEqual(1032, cert.KeyLength);
            Assert.AreEqual("04", cert.SerialNumber);
            Assert.AreEqual("CN=subjectDN", cert.Subject);
            Assert.AreEqual(new DateTime(1999, 01, 01), cert.ValidFrom);
            Assert.AreEqual(new DateTime(2000, 01, 01), cert.ValidTo);
            Assert.AreEqual("3", cert.Version);
            Assert.Null(cert.SubjectAlternativeName);
            Assert.AreEqual(certificate.Thumbprint, cert.ThumbPrint);
        }

        static X509Certificate2 GenerateCertificate()
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);
            var keyGenerationParameters = new KeyGenerationParameters(random, 1032);
            var rsaKeyPairGenerator = new RsaKeyPairGenerator();
            rsaKeyPairGenerator.Init(keyGenerationParameters);
            AsymmetricCipherKeyPair subjectKeyPair = rsaKeyPairGenerator.GenerateKeyPair();
            AsymmetricCipherKeyPair issuerKeyPair = subjectKeyPair;

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerKeyPair.Private, random);

            var keyPair = rsaKeyPairGenerator.GenerateKeyPair();

            var generator = new X509V3CertificateGenerator();

            generator.SetSerialNumber(BigInteger.Four);
            generator.SetSubjectDN(new X509Name("CN=subjectDN"));
            generator.SetIssuerDN(new X509Name("CN=issuerDN"));
            generator.SetNotAfter(new DateTime(2000, 01, 01));
            generator.SetNotBefore(new DateTime(1999, 01, 01));

            generator.SetPublicKey(keyPair.Public);

            var newCert = generator.Generate(signatureFactory);

            return new X509Certificate2(DotNetUtilities.ToX509Certificate(newCert));
        }

        [Test]
        public void CertificateOrderMatchesThumbprintOrder()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            string[] thumbprints = new string[]
            {
                "917E732D330F9A12404F73D8BEA36948B929DFFC",
                "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280",
                "2FFA06E6D8E19FEE9CB2C0399D8C29DB117039D9"
            };

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" },
                { "917E732D330F9A12404F73D8BEA36948B929DFFC", "MIIESTCCAzGgAwIBAgITBn+UV4WH6Kx33rJTMlu8mYtWDTANBgkqhkiG9w0BAQsFADA5MQswCQYDVQQGEwJVUzEPMA0GA1UEChMGQW1hem9uMRkwFwYDVQQDExBBbWF6b24gUm9vdCBDQSAxMB4XDTE1MTAyMjAwMDAwMFoXDTI1MTAxOTAwMDAwMFowRjELMAkGA1UEBhMCVVMxDzANBgNVBAoTBkFtYXpvbjEVMBMGA1UECxMMU2VydmVyIENBIDFCMQ8wDQYDVQQDEwZBbWF6b24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDCThZn3c68asg3Wuw6MLAd5tES6BIoSMzoKcG5blPVo+sDORrMd4f2AbnZcMzPa43j4wNxhplty6aUKk4T1qe9BOwKFjwK6zmxxLVYo7bHViXsPlJ6qOMpFge5blDP+18x+B26A0piiQOuPkfyDyeR4xQghfj66Yo19V+emU3nazfvpFA+ROz6WoVmB5x+F2pV8xeKNR7u6azDdU5YVX1TawprmxRC1+WsAYmz6qP+z8ArDITC2FMVy2fw0IjKOtEXc/VfmtTFch5+AfGYMGMqqvJ6LcXiAhqG5TI+Dr0RtM88k+8XUBCeQ8IGKuANaL7TiItKZYxK1MMuTJtV9IblAgMBAAGjggE7MIIBNzASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUWaRmBlKge5WSPKOUByeWdFv5PdAwHwYDVR0jBBgwFoAUhBjMhTTsvAyUlC4IWZzHshBOCggwewYIKwYBBQUHAQEEbzBtMC8GCCsGAQUFBzABhiNodHRwOi8vb2NzcC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbTA6BggrBgEFBQcwAoYuaHR0cDovL2NydC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbS9yb290Y2ExLmNlcjA/BgNVHR8EODA2MDSgMqAwhi5odHRwOi8vY3JsLnJvb3RjYTEuYW1hem9udHJ1c3QuY29tL3Jvb3RjYTEuY3JsMBMGA1UdIAQMMAowCAYGZ4EMAQIBMA0GCSqGSIb3DQEBCwUAA4IBAQCFkr41u3nPo4FCHOTjY3NTOVI159Gt/a6ZiqyJEi+752+a1U5y6iAwYfmXss2lJwJFqMp2PphKg5625kXg8kP2CN5t6G7bMQcT8C8xDZNtYTd7WPD8UZiRKAJPBXa30/AbwuZe0GaFEQ8ugcYQgSn+IGBI8/LwhBNTZTUVEWuCUUBVV18YtbAiPq3yXqMB48Oz+ctBWuZSkbvkNodPLamkB2g1upRyzQ7qDn1X8nn8N8V7YJ6y68AtkHcNSRAnpTitxBKjtKPISLMVCx7i4hncxHZSyLyKQXhw2W2Xs0qLeC1etA+jTGDK4UfLeC0SF7FSi8o5LL21L8IzApar2pR/" },
                { "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280", "MIIEdTCCA12gAwIBAgIJAKcOSkw0grd/MA0GCSqGSIb3DQEBCwUAMGgxCzAJBgNVBAYTAlVTMSUwIwYDVQQKExxTdGFyZmllbGQgVGVjaG5vbG9naWVzLCBJbmMuMTIwMAYDVQQLEylTdGFyZmllbGQgQ2xhc3MgMiBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0wOTA5MDIwMDAwMDBaFw0zNDA2MjgxNzM5MTZaMIGYMQswCQYDVQQGEwJVUzEQMA4GA1UECBMHQXJpem9uYTETMBEGA1UEBxMKU2NvdHRzZGFsZTElMCMGA1UEChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjE7MDkGA1UEAxMyU3RhcmZpZWxkIFNlcnZpY2VzIFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IC0gRzIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDVDDrEKvlO4vW+GZdfjohTsR8/y8+fIBNtKTrID30892t2OGPZNmCom15cAICyL1l/9of5JUOG52kbUpqQ4XHj2C0NTm/2yEnZtvMaVq4rtnQU68/7JuMauh2WLmo7WJSJR1b/JaCTcFOD2oR0FMNnngRoOt+OQFodSk7PQ5E751bWAHDLUu57fa4657wx+UX2wmDPE1kCK4DMNEffud6QZW0CzyyRpqbn3oUYSXxmTqM6bam17jQuug0DuDPfR+uxa40l2ZvOgdFFRjKWcIfeAg5JQ4W2bHO7ZOphQazJ1FTfhy/HIrImzJ9ZVGif/L4qL8RVHHVAYBeFAlU5i38FAgMBAAGjgfAwge0wDwYDVR0TAQH/BAUwAwEB/zAOBgNVHQ8BAf8EBAMCAYYwHQYDVR0OBBYEFJxfAN+qAdcwKziIorhtSpzyEZGDMB8GA1UdIwQYMBaAFL9ft9HO3R+G9FtVrNzXEMIOqYjnME8GCCsGAQUFBwEBBEMwQTAcBggrBgEFBQcwAYYQaHR0cDovL28uc3MyLnVzLzAhBggrBgEFBQcwAoYVaHR0cDovL3guc3MyLnVzL3guY2VyMCYGA1UdHwQfMB0wG6AZoBeGFWh0dHA6Ly9zLnNzMi51cy9yLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQELBQADggEBACMd44pXyn3pF3lM8R5V/cxTbj5HD9/GVfKyBDbtgB9TxF00KGu+x1X8Z+rLP3+QsjPNG1gQggL4+C/1E2DUBc7xgQjB3ad1l08YuW3e95ORCLp+QCztweq7dp4zBncdDQh/U90bZKuCJ/Fp1U1ervShw3WnWEQt8jxwmKy6abaVd38PMV4s/KCHOkdp8Hlf9BRUpJVeEXgSYCfOn8J3/yNTd126/+pZ59vPr5KW7ySaNRB6nJHGDn2Z9j8Z3/VyVOEVqQdZe4O/Ui5GjLIAZHYcSNPYeehuVsyuLAOQ1xk4meTKCRlb/weWsKh/NEnfVqn3sF/tM+2MR7cwA130A4w=" }
            };

            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>()
                    {
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = thumbprints,
                         }
                    },
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            Assert.AreEqual(thumbprints[0], domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(thumbprints[1], domainTlsEvaluatorResults.CertificateResults[0].Certificates[1].ThumbPrint);
            Assert.AreEqual(thumbprints[2], domainTlsEvaluatorResults.CertificateResults[0].Certificates[2].ThumbPrint);
            Assert.AreEqual(3, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }

        [Test]
        public void CertificateOrderIsDefaultDictionaryOrderWhenTlsResultsAreEmpty()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" },
                { "917E732D330F9A12404F73D8BEA36948B929DFFC", "MIIESTCCAzGgAwIBAgITBn+UV4WH6Kx33rJTMlu8mYtWDTANBgkqhkiG9w0BAQsFADA5MQswCQYDVQQGEwJVUzEPMA0GA1UEChMGQW1hem9uMRkwFwYDVQQDExBBbWF6b24gUm9vdCBDQSAxMB4XDTE1MTAyMjAwMDAwMFoXDTI1MTAxOTAwMDAwMFowRjELMAkGA1UEBhMCVVMxDzANBgNVBAoTBkFtYXpvbjEVMBMGA1UECxMMU2VydmVyIENBIDFCMQ8wDQYDVQQDEwZBbWF6b24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDCThZn3c68asg3Wuw6MLAd5tES6BIoSMzoKcG5blPVo+sDORrMd4f2AbnZcMzPa43j4wNxhplty6aUKk4T1qe9BOwKFjwK6zmxxLVYo7bHViXsPlJ6qOMpFge5blDP+18x+B26A0piiQOuPkfyDyeR4xQghfj66Yo19V+emU3nazfvpFA+ROz6WoVmB5x+F2pV8xeKNR7u6azDdU5YVX1TawprmxRC1+WsAYmz6qP+z8ArDITC2FMVy2fw0IjKOtEXc/VfmtTFch5+AfGYMGMqqvJ6LcXiAhqG5TI+Dr0RtM88k+8XUBCeQ8IGKuANaL7TiItKZYxK1MMuTJtV9IblAgMBAAGjggE7MIIBNzASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUWaRmBlKge5WSPKOUByeWdFv5PdAwHwYDVR0jBBgwFoAUhBjMhTTsvAyUlC4IWZzHshBOCggwewYIKwYBBQUHAQEEbzBtMC8GCCsGAQUFBzABhiNodHRwOi8vb2NzcC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbTA6BggrBgEFBQcwAoYuaHR0cDovL2NydC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbS9yb290Y2ExLmNlcjA/BgNVHR8EODA2MDSgMqAwhi5odHRwOi8vY3JsLnJvb3RjYTEuYW1hem9udHJ1c3QuY29tL3Jvb3RjYTEuY3JsMBMGA1UdIAQMMAowCAYGZ4EMAQIBMA0GCSqGSIb3DQEBCwUAA4IBAQCFkr41u3nPo4FCHOTjY3NTOVI159Gt/a6ZiqyJEi+752+a1U5y6iAwYfmXss2lJwJFqMp2PphKg5625kXg8kP2CN5t6G7bMQcT8C8xDZNtYTd7WPD8UZiRKAJPBXa30/AbwuZe0GaFEQ8ugcYQgSn+IGBI8/LwhBNTZTUVEWuCUUBVV18YtbAiPq3yXqMB48Oz+ctBWuZSkbvkNodPLamkB2g1upRyzQ7qDn1X8nn8N8V7YJ6y68AtkHcNSRAnpTitxBKjtKPISLMVCx7i4hncxHZSyLyKQXhw2W2Xs0qLeC1etA+jTGDK4UfLeC0SF7FSi8o5LL21L8IzApar2pR/" },
                { "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280", "MIIEdTCCA12gAwIBAgIJAKcOSkw0grd/MA0GCSqGSIb3DQEBCwUAMGgxCzAJBgNVBAYTAlVTMSUwIwYDVQQKExxTdGFyZmllbGQgVGVjaG5vbG9naWVzLCBJbmMuMTIwMAYDVQQLEylTdGFyZmllbGQgQ2xhc3MgMiBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0wOTA5MDIwMDAwMDBaFw0zNDA2MjgxNzM5MTZaMIGYMQswCQYDVQQGEwJVUzEQMA4GA1UECBMHQXJpem9uYTETMBEGA1UEBxMKU2NvdHRzZGFsZTElMCMGA1UEChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjE7MDkGA1UEAxMyU3RhcmZpZWxkIFNlcnZpY2VzIFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IC0gRzIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDVDDrEKvlO4vW+GZdfjohTsR8/y8+fIBNtKTrID30892t2OGPZNmCom15cAICyL1l/9of5JUOG52kbUpqQ4XHj2C0NTm/2yEnZtvMaVq4rtnQU68/7JuMauh2WLmo7WJSJR1b/JaCTcFOD2oR0FMNnngRoOt+OQFodSk7PQ5E751bWAHDLUu57fa4657wx+UX2wmDPE1kCK4DMNEffud6QZW0CzyyRpqbn3oUYSXxmTqM6bam17jQuug0DuDPfR+uxa40l2ZvOgdFFRjKWcIfeAg5JQ4W2bHO7ZOphQazJ1FTfhy/HIrImzJ9ZVGif/L4qL8RVHHVAYBeFAlU5i38FAgMBAAGjgfAwge0wDwYDVR0TAQH/BAUwAwEB/zAOBgNVHQ8BAf8EBAMCAYYwHQYDVR0OBBYEFJxfAN+qAdcwKziIorhtSpzyEZGDMB8GA1UdIwQYMBaAFL9ft9HO3R+G9FtVrNzXEMIOqYjnME8GCCsGAQUFBwEBBEMwQTAcBggrBgEFBQcwAYYQaHR0cDovL28uc3MyLnVzLzAhBggrBgEFBQcwAoYVaHR0cDovL3guc3MyLnVzL3guY2VyMCYGA1UdHwQfMB0wG6AZoBeGFWh0dHA6Ly9zLnNzMi51cy9yLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQELBQADggEBACMd44pXyn3pF3lM8R5V/cxTbj5HD9/GVfKyBDbtgB9TxF00KGu+x1X8Z+rLP3+QsjPNG1gQggL4+C/1E2DUBc7xgQjB3ad1l08YuW3e95ORCLp+QCztweq7dp4zBncdDQh/U90bZKuCJ/Fp1U1ervShw3WnWEQt8jxwmKy6abaVd38PMV4s/KCHOkdp8Hlf9BRUpJVeEXgSYCfOn8J3/yNTd126/+pZ59vPr5KW7ySaNRB6nJHGDn2Z9j8Z3/VyVOEVqQdZe4O/Ui5GjLIAZHYcSNPYeehuVsyuLAOQ1xk4meTKCRlb/weWsKh/NEnfVqn3sF/tM+2MR7cwA130A4w=" }
            };

            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>(),
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            List<string> defaultThumbprint = new List<string>(certificates.Keys);

            Assert.AreEqual(defaultThumbprint[0], domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(defaultThumbprint[1], domainTlsEvaluatorResults.CertificateResults[0].Certificates[1].ThumbPrint);
            Assert.AreEqual(defaultThumbprint[2], domainTlsEvaluatorResults.CertificateResults[0].Certificates[2].ThumbPrint);
            Assert.AreEqual(3, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }

        [Test]
        public void CertificateOrderIsDefaultDictionaryOrderWhenTlsResultsAreNull()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" },
                { "917E732D330F9A12404F73D8BEA36948B929DFFC", "MIIESTCCAzGgAwIBAgITBn+UV4WH6Kx33rJTMlu8mYtWDTANBgkqhkiG9w0BAQsFADA5MQswCQYDVQQGEwJVUzEPMA0GA1UEChMGQW1hem9uMRkwFwYDVQQDExBBbWF6b24gUm9vdCBDQSAxMB4XDTE1MTAyMjAwMDAwMFoXDTI1MTAxOTAwMDAwMFowRjELMAkGA1UEBhMCVVMxDzANBgNVBAoTBkFtYXpvbjEVMBMGA1UECxMMU2VydmVyIENBIDFCMQ8wDQYDVQQDEwZBbWF6b24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDCThZn3c68asg3Wuw6MLAd5tES6BIoSMzoKcG5blPVo+sDORrMd4f2AbnZcMzPa43j4wNxhplty6aUKk4T1qe9BOwKFjwK6zmxxLVYo7bHViXsPlJ6qOMpFge5blDP+18x+B26A0piiQOuPkfyDyeR4xQghfj66Yo19V+emU3nazfvpFA+ROz6WoVmB5x+F2pV8xeKNR7u6azDdU5YVX1TawprmxRC1+WsAYmz6qP+z8ArDITC2FMVy2fw0IjKOtEXc/VfmtTFch5+AfGYMGMqqvJ6LcXiAhqG5TI+Dr0RtM88k+8XUBCeQ8IGKuANaL7TiItKZYxK1MMuTJtV9IblAgMBAAGjggE7MIIBNzASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUWaRmBlKge5WSPKOUByeWdFv5PdAwHwYDVR0jBBgwFoAUhBjMhTTsvAyUlC4IWZzHshBOCggwewYIKwYBBQUHAQEEbzBtMC8GCCsGAQUFBzABhiNodHRwOi8vb2NzcC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbTA6BggrBgEFBQcwAoYuaHR0cDovL2NydC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbS9yb290Y2ExLmNlcjA/BgNVHR8EODA2MDSgMqAwhi5odHRwOi8vY3JsLnJvb3RjYTEuYW1hem9udHJ1c3QuY29tL3Jvb3RjYTEuY3JsMBMGA1UdIAQMMAowCAYGZ4EMAQIBMA0GCSqGSIb3DQEBCwUAA4IBAQCFkr41u3nPo4FCHOTjY3NTOVI159Gt/a6ZiqyJEi+752+a1U5y6iAwYfmXss2lJwJFqMp2PphKg5625kXg8kP2CN5t6G7bMQcT8C8xDZNtYTd7WPD8UZiRKAJPBXa30/AbwuZe0GaFEQ8ugcYQgSn+IGBI8/LwhBNTZTUVEWuCUUBVV18YtbAiPq3yXqMB48Oz+ctBWuZSkbvkNodPLamkB2g1upRyzQ7qDn1X8nn8N8V7YJ6y68AtkHcNSRAnpTitxBKjtKPISLMVCx7i4hncxHZSyLyKQXhw2W2Xs0qLeC1etA+jTGDK4UfLeC0SF7FSi8o5LL21L8IzApar2pR/" },
                { "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280", "MIIEdTCCA12gAwIBAgIJAKcOSkw0grd/MA0GCSqGSIb3DQEBCwUAMGgxCzAJBgNVBAYTAlVTMSUwIwYDVQQKExxTdGFyZmllbGQgVGVjaG5vbG9naWVzLCBJbmMuMTIwMAYDVQQLEylTdGFyZmllbGQgQ2xhc3MgMiBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0wOTA5MDIwMDAwMDBaFw0zNDA2MjgxNzM5MTZaMIGYMQswCQYDVQQGEwJVUzEQMA4GA1UECBMHQXJpem9uYTETMBEGA1UEBxMKU2NvdHRzZGFsZTElMCMGA1UEChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjE7MDkGA1UEAxMyU3RhcmZpZWxkIFNlcnZpY2VzIFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IC0gRzIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDVDDrEKvlO4vW+GZdfjohTsR8/y8+fIBNtKTrID30892t2OGPZNmCom15cAICyL1l/9of5JUOG52kbUpqQ4XHj2C0NTm/2yEnZtvMaVq4rtnQU68/7JuMauh2WLmo7WJSJR1b/JaCTcFOD2oR0FMNnngRoOt+OQFodSk7PQ5E751bWAHDLUu57fa4657wx+UX2wmDPE1kCK4DMNEffud6QZW0CzyyRpqbn3oUYSXxmTqM6bam17jQuug0DuDPfR+uxa40l2ZvOgdFFRjKWcIfeAg5JQ4W2bHO7ZOphQazJ1FTfhy/HIrImzJ9ZVGif/L4qL8RVHHVAYBeFAlU5i38FAgMBAAGjgfAwge0wDwYDVR0TAQH/BAUwAwEB/zAOBgNVHQ8BAf8EBAMCAYYwHQYDVR0OBBYEFJxfAN+qAdcwKziIorhtSpzyEZGDMB8GA1UdIwQYMBaAFL9ft9HO3R+G9FtVrNzXEMIOqYjnME8GCCsGAQUFBwEBBEMwQTAcBggrBgEFBQcwAYYQaHR0cDovL28uc3MyLnVzLzAhBggrBgEFBQcwAoYVaHR0cDovL3guc3MyLnVzL3guY2VyMCYGA1UdHwQfMB0wG6AZoBeGFWh0dHA6Ly9zLnNzMi51cy9yLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQELBQADggEBACMd44pXyn3pF3lM8R5V/cxTbj5HD9/GVfKyBDbtgB9TxF00KGu+x1X8Z+rLP3+QsjPNG1gQggL4+C/1E2DUBc7xgQjB3ad1l08YuW3e95ORCLp+QCztweq7dp4zBncdDQh/U90bZKuCJ/Fp1U1ervShw3WnWEQt8jxwmKy6abaVd38PMV4s/KCHOkdp8Hlf9BRUpJVeEXgSYCfOn8J3/yNTd126/+pZ59vPr5KW7ySaNRB6nJHGDn2Z9j8Z3/VyVOEVqQdZe4O/Ui5GjLIAZHYcSNPYeehuVsyuLAOQ1xk4meTKCRlb/weWsKh/NEnfVqn3sF/tM+2MR7cwA130A4w=" }
            };

            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = null,
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            List<string> defaultThumbprints = new List<string>(certificates.Keys);

            Assert.AreEqual(defaultThumbprints[0], domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(defaultThumbprints[1], domainTlsEvaluatorResults.CertificateResults[0].Certificates[1].ThumbPrint);
            Assert.AreEqual(defaultThumbprints[2], domainTlsEvaluatorResults.CertificateResults[0].Certificates[2].ThumbPrint);
            Assert.AreEqual(3, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }

        [Test]
        public void CertificateOrderMatchesThumbprintOrderOfFirstPopulatedThumbprintList()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            string[] thumbprintsToUse = new string[]
            {
                "917E732D330F9A12404F73D8BEA36948B929DFFC",
                "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280",
                "2FFA06E6D8E19FEE9CB2C0399D8C29DB117039D9"
            };

            string[] thumbprintsToIgnore = new string[]
            {
                "917E732D330F9A12404F73D8BEA36948B929DFFC",
                "2FFA06E6D8E19FEE9CB2C0399D8C29DB117039D9",
                "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280"
            };

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" },
                { "917E732D330F9A12404F73D8BEA36948B929DFFC", "MIIESTCCAzGgAwIBAgITBn+UV4WH6Kx33rJTMlu8mYtWDTANBgkqhkiG9w0BAQsFADA5MQswCQYDVQQGEwJVUzEPMA0GA1UEChMGQW1hem9uMRkwFwYDVQQDExBBbWF6b24gUm9vdCBDQSAxMB4XDTE1MTAyMjAwMDAwMFoXDTI1MTAxOTAwMDAwMFowRjELMAkGA1UEBhMCVVMxDzANBgNVBAoTBkFtYXpvbjEVMBMGA1UECxMMU2VydmVyIENBIDFCMQ8wDQYDVQQDEwZBbWF6b24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDCThZn3c68asg3Wuw6MLAd5tES6BIoSMzoKcG5blPVo+sDORrMd4f2AbnZcMzPa43j4wNxhplty6aUKk4T1qe9BOwKFjwK6zmxxLVYo7bHViXsPlJ6qOMpFge5blDP+18x+B26A0piiQOuPkfyDyeR4xQghfj66Yo19V+emU3nazfvpFA+ROz6WoVmB5x+F2pV8xeKNR7u6azDdU5YVX1TawprmxRC1+WsAYmz6qP+z8ArDITC2FMVy2fw0IjKOtEXc/VfmtTFch5+AfGYMGMqqvJ6LcXiAhqG5TI+Dr0RtM88k+8XUBCeQ8IGKuANaL7TiItKZYxK1MMuTJtV9IblAgMBAAGjggE7MIIBNzASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUWaRmBlKge5WSPKOUByeWdFv5PdAwHwYDVR0jBBgwFoAUhBjMhTTsvAyUlC4IWZzHshBOCggwewYIKwYBBQUHAQEEbzBtMC8GCCsGAQUFBzABhiNodHRwOi8vb2NzcC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbTA6BggrBgEFBQcwAoYuaHR0cDovL2NydC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbS9yb290Y2ExLmNlcjA/BgNVHR8EODA2MDSgMqAwhi5odHRwOi8vY3JsLnJvb3RjYTEuYW1hem9udHJ1c3QuY29tL3Jvb3RjYTEuY3JsMBMGA1UdIAQMMAowCAYGZ4EMAQIBMA0GCSqGSIb3DQEBCwUAA4IBAQCFkr41u3nPo4FCHOTjY3NTOVI159Gt/a6ZiqyJEi+752+a1U5y6iAwYfmXss2lJwJFqMp2PphKg5625kXg8kP2CN5t6G7bMQcT8C8xDZNtYTd7WPD8UZiRKAJPBXa30/AbwuZe0GaFEQ8ugcYQgSn+IGBI8/LwhBNTZTUVEWuCUUBVV18YtbAiPq3yXqMB48Oz+ctBWuZSkbvkNodPLamkB2g1upRyzQ7qDn1X8nn8N8V7YJ6y68AtkHcNSRAnpTitxBKjtKPISLMVCx7i4hncxHZSyLyKQXhw2W2Xs0qLeC1etA+jTGDK4UfLeC0SF7FSi8o5LL21L8IzApar2pR/" },
                { "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280", "MIIEdTCCA12gAwIBAgIJAKcOSkw0grd/MA0GCSqGSIb3DQEBCwUAMGgxCzAJBgNVBAYTAlVTMSUwIwYDVQQKExxTdGFyZmllbGQgVGVjaG5vbG9naWVzLCBJbmMuMTIwMAYDVQQLEylTdGFyZmllbGQgQ2xhc3MgMiBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0wOTA5MDIwMDAwMDBaFw0zNDA2MjgxNzM5MTZaMIGYMQswCQYDVQQGEwJVUzEQMA4GA1UECBMHQXJpem9uYTETMBEGA1UEBxMKU2NvdHRzZGFsZTElMCMGA1UEChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjE7MDkGA1UEAxMyU3RhcmZpZWxkIFNlcnZpY2VzIFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IC0gRzIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDVDDrEKvlO4vW+GZdfjohTsR8/y8+fIBNtKTrID30892t2OGPZNmCom15cAICyL1l/9of5JUOG52kbUpqQ4XHj2C0NTm/2yEnZtvMaVq4rtnQU68/7JuMauh2WLmo7WJSJR1b/JaCTcFOD2oR0FMNnngRoOt+OQFodSk7PQ5E751bWAHDLUu57fa4657wx+UX2wmDPE1kCK4DMNEffud6QZW0CzyyRpqbn3oUYSXxmTqM6bam17jQuug0DuDPfR+uxa40l2ZvOgdFFRjKWcIfeAg5JQ4W2bHO7ZOphQazJ1FTfhy/HIrImzJ9ZVGif/L4qL8RVHHVAYBeFAlU5i38FAgMBAAGjgfAwge0wDwYDVR0TAQH/BAUwAwEB/zAOBgNVHQ8BAf8EBAMCAYYwHQYDVR0OBBYEFJxfAN+qAdcwKziIorhtSpzyEZGDMB8GA1UdIwQYMBaAFL9ft9HO3R+G9FtVrNzXEMIOqYjnME8GCCsGAQUFBwEBBEMwQTAcBggrBgEFBQcwAYYQaHR0cDovL28uc3MyLnVzLzAhBggrBgEFBQcwAoYVaHR0cDovL3guc3MyLnVzL3guY2VyMCYGA1UdHwQfMB0wG6AZoBeGFWh0dHA6Ly9zLnNzMi51cy9yLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQELBQADggEBACMd44pXyn3pF3lM8R5V/cxTbj5HD9/GVfKyBDbtgB9TxF00KGu+x1X8Z+rLP3+QsjPNG1gQggL4+C/1E2DUBc7xgQjB3ad1l08YuW3e95ORCLp+QCztweq7dp4zBncdDQh/U90bZKuCJ/Fp1U1ervShw3WnWEQt8jxwmKy6abaVd38PMV4s/KCHOkdp8Hlf9BRUpJVeEXgSYCfOn8J3/yNTd126/+pZ59vPr5KW7ySaNRB6nJHGDn2Z9j8Z3/VyVOEVqQdZe4O/Ui5GjLIAZHYcSNPYeehuVsyuLAOQ1xk4meTKCRlb/weWsKh/NEnfVqn3sF/tM+2MR7cwA130A4w=" }
            };

            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>()
                    {
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = null,
                         },
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = new string[0],
                         },
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = thumbprintsToUse,
                         },
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = thumbprintsToIgnore,
                         }
                    },
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            Assert.AreEqual(thumbprintsToUse[0], domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(thumbprintsToUse[1], domainTlsEvaluatorResults.CertificateResults[0].Certificates[1].ThumbPrint);
            Assert.AreEqual(thumbprintsToUse[2], domainTlsEvaluatorResults.CertificateResults[0].Certificates[2].ThumbPrint);
            Assert.AreEqual(3, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }

        [Test]
        public void CertificateOrderIngnoresCertificatesFromOtherTlsTests()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            string[] thumbprints = new string[]
            {
                "917E732D330F9A12404F73D8BEA36948B929DFFC",
                "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280",
                "2FFA06E6D8E19FEE9CB2C0399D8C29DB117039D9"
            };

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" },
                { "917E732D330F9A12404F73D8BEA36948B929DFFC", "MIIESTCCAzGgAwIBAgITBn+UV4WH6Kx33rJTMlu8mYtWDTANBgkqhkiG9w0BAQsFADA5MQswCQYDVQQGEwJVUzEPMA0GA1UEChMGQW1hem9uMRkwFwYDVQQDExBBbWF6b24gUm9vdCBDQSAxMB4XDTE1MTAyMjAwMDAwMFoXDTI1MTAxOTAwMDAwMFowRjELMAkGA1UEBhMCVVMxDzANBgNVBAoTBkFtYXpvbjEVMBMGA1UECxMMU2VydmVyIENBIDFCMQ8wDQYDVQQDEwZBbWF6b24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDCThZn3c68asg3Wuw6MLAd5tES6BIoSMzoKcG5blPVo+sDORrMd4f2AbnZcMzPa43j4wNxhplty6aUKk4T1qe9BOwKFjwK6zmxxLVYo7bHViXsPlJ6qOMpFge5blDP+18x+B26A0piiQOuPkfyDyeR4xQghfj66Yo19V+emU3nazfvpFA+ROz6WoVmB5x+F2pV8xeKNR7u6azDdU5YVX1TawprmxRC1+WsAYmz6qP+z8ArDITC2FMVy2fw0IjKOtEXc/VfmtTFch5+AfGYMGMqqvJ6LcXiAhqG5TI+Dr0RtM88k+8XUBCeQ8IGKuANaL7TiItKZYxK1MMuTJtV9IblAgMBAAGjggE7MIIBNzASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUWaRmBlKge5WSPKOUByeWdFv5PdAwHwYDVR0jBBgwFoAUhBjMhTTsvAyUlC4IWZzHshBOCggwewYIKwYBBQUHAQEEbzBtMC8GCCsGAQUFBzABhiNodHRwOi8vb2NzcC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbTA6BggrBgEFBQcwAoYuaHR0cDovL2NydC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbS9yb290Y2ExLmNlcjA/BgNVHR8EODA2MDSgMqAwhi5odHRwOi8vY3JsLnJvb3RjYTEuYW1hem9udHJ1c3QuY29tL3Jvb3RjYTEuY3JsMBMGA1UdIAQMMAowCAYGZ4EMAQIBMA0GCSqGSIb3DQEBCwUAA4IBAQCFkr41u3nPo4FCHOTjY3NTOVI159Gt/a6ZiqyJEi+752+a1U5y6iAwYfmXss2lJwJFqMp2PphKg5625kXg8kP2CN5t6G7bMQcT8C8xDZNtYTd7WPD8UZiRKAJPBXa30/AbwuZe0GaFEQ8ugcYQgSn+IGBI8/LwhBNTZTUVEWuCUUBVV18YtbAiPq3yXqMB48Oz+ctBWuZSkbvkNodPLamkB2g1upRyzQ7qDn1X8nn8N8V7YJ6y68AtkHcNSRAnpTitxBKjtKPISLMVCx7i4hncxHZSyLyKQXhw2W2Xs0qLeC1etA+jTGDK4UfLeC0SF7FSi8o5LL21L8IzApar2pR/" },
                { "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280", "MIIEdTCCA12gAwIBAgIJAKcOSkw0grd/MA0GCSqGSIb3DQEBCwUAMGgxCzAJBgNVBAYTAlVTMSUwIwYDVQQKExxTdGFyZmllbGQgVGVjaG5vbG9naWVzLCBJbmMuMTIwMAYDVQQLEylTdGFyZmllbGQgQ2xhc3MgMiBDZXJ0aWZpY2F0aW9uIEF1dGhvcml0eTAeFw0wOTA5MDIwMDAwMDBaFw0zNDA2MjgxNzM5MTZaMIGYMQswCQYDVQQGEwJVUzEQMA4GA1UECBMHQXJpem9uYTETMBEGA1UEBxMKU2NvdHRzZGFsZTElMCMGA1UEChMcU3RhcmZpZWxkIFRlY2hub2xvZ2llcywgSW5jLjE7MDkGA1UEAxMyU3RhcmZpZWxkIFNlcnZpY2VzIFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IC0gRzIwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDVDDrEKvlO4vW+GZdfjohTsR8/y8+fIBNtKTrID30892t2OGPZNmCom15cAICyL1l/9of5JUOG52kbUpqQ4XHj2C0NTm/2yEnZtvMaVq4rtnQU68/7JuMauh2WLmo7WJSJR1b/JaCTcFOD2oR0FMNnngRoOt+OQFodSk7PQ5E751bWAHDLUu57fa4657wx+UX2wmDPE1kCK4DMNEffud6QZW0CzyyRpqbn3oUYSXxmTqM6bam17jQuug0DuDPfR+uxa40l2ZvOgdFFRjKWcIfeAg5JQ4W2bHO7ZOphQazJ1FTfhy/HIrImzJ9ZVGif/L4qL8RVHHVAYBeFAlU5i38FAgMBAAGjgfAwge0wDwYDVR0TAQH/BAUwAwEB/zAOBgNVHQ8BAf8EBAMCAYYwHQYDVR0OBBYEFJxfAN+qAdcwKziIorhtSpzyEZGDMB8GA1UdIwQYMBaAFL9ft9HO3R+G9FtVrNzXEMIOqYjnME8GCCsGAQUFBwEBBEMwQTAcBggrBgEFBQcwAYYQaHR0cDovL28uc3MyLnVzLzAhBggrBgEFBQcwAoYVaHR0cDovL3guc3MyLnVzL3guY2VyMCYGA1UdHwQfMB0wG6AZoBeGFWh0dHA6Ly9zLnNzMi51cy9yLmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQELBQADggEBACMd44pXyn3pF3lM8R5V/cxTbj5HD9/GVfKyBDbtgB9TxF00KGu+x1X8Z+rLP3+QsjPNG1gQggL4+C/1E2DUBc7xgQjB3ad1l08YuW3e95ORCLp+QCztweq7dp4zBncdDQh/U90bZKuCJ/Fp1U1ervShw3WnWEQt8jxwmKy6abaVd38PMV4s/KCHOkdp8Hlf9BRUpJVeEXgSYCfOn8J3/yNTd126/+pZ59vPr5KW7ySaNRB6nJHGDn2Z9j8Z3/VyVOEVqQdZe4O/Ui5GjLIAZHYcSNPYeehuVsyuLAOQ1xk4meTKCRlb/weWsKh/NEnfVqn3sF/tM+2MR7cwA130A4w=" },

                // Certificates not used in the chain displayed
                { "81B68D6CD2F221F8F534E677523BB236BBA1DC56", "MIIEkjCCA3qgAwIBAgIQAZ7Bxr0/WXuyDDM45VHYdzANBgkqhkiG9w0BAQsFADBhMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSAwHgYDVQQDExdEaWdpQ2VydCBHbG9iYWwgUm9vdCBDQTAeFw0xNTA4MDQxMjAwMDBaFw0zMDA4MDQxMjAwMDBaMEsxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxJTAjBgNVBAMTHERpZ2lDZXJ0IENsb3VkIFNlcnZpY2VzIENBLTEwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDRrfaHFHlUJ1fHLwVoPJs8zWfsRRAshPKkR8TZU0JFCbvk/jPB17xGL9GL5re1Z3h8anC+/bjltlTPTF6suCJ0c1UpCHPIZPfQlQkOeYNQv1/11MybQmGOgAS5QarOThKZm6zWxb5bAnO1FqSrcWLUmOpAOYWm9rsv6OeHwov2nDLN7Pg+v4nndCOCS9rqv3OmJTz9v6nlaP/4MKJgxzsuo/PFfzs7/Q8xoXx0D9C/FMS9aPGl52un35sAfkYlTuboE/P2BsfUbwsnIEJdYbw/YNJ8lnLJfLCL//lIBVME+iKvt81RXW3dkHQD8DNP9MfAPlZGR69zIIvcej6j8l3/AgMBAAGjggFaMIIBVjASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjA0BggrBgEFBQcBAQQoMCYwJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTB7BgNVHR8EdDByMDegNaAzhjFodHRwOi8vY3JsNC5kaWdpY2VydC5jb20vRGlnaUNlcnRHbG9iYWxSb290Q0EuY3JsMDegNaAzhjFodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRHbG9iYWxSb290Q0EuY3JsMD0GA1UdIAQ2MDQwMgYEVR0gADAqMCgGCCsGAQUFBwIBFhxodHRwczovL3d3dy5kaWdpY2VydC5jb20vQ1BTMB0GA1UdDgQWBBTdUdCiMXOpc66PtAF+XYxXy5/w9zAfBgNVHSMEGDAWgBQD3lA1VtFMu2bwo+IbG8OXsj3RVTANBgkqhkiG9w0BAQsFAAOCAQEACCnEyKb+tDgo96MZZ4zqBTsOS0BiYh48FMYF3DanTzJxRgVegB1ca/Btbdkhdgu9RsS5ZpdN/4AUeodphLLW/8kWcL6jzIshre5cjSStwo+Z4MyeigkDuA+atVuQKyr316UvSmWoxOTFx3GplkZPq21LKhbL8ak79h8hObTrrWAEgpsSv96r0kYdDA07dgL5C9XOU4VCeylNRtGLzWTsIRZPLwFDWNFl7Vyl+0Sg0lDo3mbEtjGehzMDsMnGSxLnWzWU2UbOMeu/uPaeC4SFgiJWxCOEVOdSMwwlyxrsRFUPY5Zys80ZXn4OJ4XVpOqw4qXcBiklkOjOLOnp0Hzvzg==" },
                { "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" }
            };

            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>()
                    {
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = thumbprints,
                         }
                    },
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            Assert.AreEqual(thumbprints[0], domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(thumbprints[1], domainTlsEvaluatorResults.CertificateResults[0].Certificates[1].ThumbPrint);
            Assert.AreEqual(thumbprints[2], domainTlsEvaluatorResults.CertificateResults[0].Certificates[2].ThumbPrint);
            Assert.AreEqual(3, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }


        [Test]
        public void CertificateOrderHandlesCertificateNotFound()
        {
            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            string[] thumbprints = new string[]
            {
                "917E732D330F9A12404F73D8BEA36948B929DFFC",
                "9E99A48A9960B14926BB7F3B02E22DA2B0AB7280", // <-- Missing from certificates
                "2FFA06E6D8E19FEE9CB2C0399D8C29DB117039D9"
            };

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" },
                { "917E732D330F9A12404F73D8BEA36948B929DFFC", "MIIESTCCAzGgAwIBAgITBn+UV4WH6Kx33rJTMlu8mYtWDTANBgkqhkiG9w0BAQsFADA5MQswCQYDVQQGEwJVUzEPMA0GA1UEChMGQW1hem9uMRkwFwYDVQQDExBBbWF6b24gUm9vdCBDQSAxMB4XDTE1MTAyMjAwMDAwMFoXDTI1MTAxOTAwMDAwMFowRjELMAkGA1UEBhMCVVMxDzANBgNVBAoTBkFtYXpvbjEVMBMGA1UECxMMU2VydmVyIENBIDFCMQ8wDQYDVQQDEwZBbWF6b24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDCThZn3c68asg3Wuw6MLAd5tES6BIoSMzoKcG5blPVo+sDORrMd4f2AbnZcMzPa43j4wNxhplty6aUKk4T1qe9BOwKFjwK6zmxxLVYo7bHViXsPlJ6qOMpFge5blDP+18x+B26A0piiQOuPkfyDyeR4xQghfj66Yo19V+emU3nazfvpFA+ROz6WoVmB5x+F2pV8xeKNR7u6azDdU5YVX1TawprmxRC1+WsAYmz6qP+z8ArDITC2FMVy2fw0IjKOtEXc/VfmtTFch5+AfGYMGMqqvJ6LcXiAhqG5TI+Dr0RtM88k+8XUBCeQ8IGKuANaL7TiItKZYxK1MMuTJtV9IblAgMBAAGjggE7MIIBNzASBgNVHRMBAf8ECDAGAQH/AgEAMA4GA1UdDwEB/wQEAwIBhjAdBgNVHQ4EFgQUWaRmBlKge5WSPKOUByeWdFv5PdAwHwYDVR0jBBgwFoAUhBjMhTTsvAyUlC4IWZzHshBOCggwewYIKwYBBQUHAQEEbzBtMC8GCCsGAQUFBzABhiNodHRwOi8vb2NzcC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbTA6BggrBgEFBQcwAoYuaHR0cDovL2NydC5yb290Y2ExLmFtYXpvbnRydXN0LmNvbS9yb290Y2ExLmNlcjA/BgNVHR8EODA2MDSgMqAwhi5odHRwOi8vY3JsLnJvb3RjYTEuYW1hem9udHJ1c3QuY29tL3Jvb3RjYTEuY3JsMBMGA1UdIAQMMAowCAYGZ4EMAQIBMA0GCSqGSIb3DQEBCwUAA4IBAQCFkr41u3nPo4FCHOTjY3NTOVI159Gt/a6ZiqyJEi+752+a1U5y6iAwYfmXss2lJwJFqMp2PphKg5625kXg8kP2CN5t6G7bMQcT8C8xDZNtYTd7WPD8UZiRKAJPBXa30/AbwuZe0GaFEQ8ugcYQgSn+IGBI8/LwhBNTZTUVEWuCUUBVV18YtbAiPq3yXqMB48Oz+ctBWuZSkbvkNodPLamkB2g1upRyzQ7qDn1X8nn8N8V7YJ6y68AtkHcNSRAnpTitxBKjtKPISLMVCx7i4hncxHZSyLyKQXhw2W2Xs0qLeC1etA+jTGDK4UfLeC0SF7FSi8o5LL21L8IzApar2pR/" }
            };

            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>()
                    {
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = thumbprints,
                         }
                    },
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            Assert.AreEqual(thumbprints[0], domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(thumbprints[2], domainTlsEvaluatorResults.CertificateResults[0].Certificates[1].ThumbPrint);
            Assert.AreEqual(2, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }

        [Test]
        public void CertificateThumbprintsAreConvertedToUppercase()
        {
            // The CamelCasePropertyNamesContractResolver converts all properties to camel case
            // This incudes the dictionary key for the certificte thumbprints when they are deserialised

            _domainTlsEvaluatorResultsFactory = new DomainTlsEvaluatorResultsFactory();

            Dictionary<string, int> preferences = new Dictionary<string, int>();

            string[] thumbprints = new string[]
            {
                "D0E2C5EB4A705F5560036BD253ACBB567FA15FB0"
            };

            Dictionary<string, string> certificates = new Dictionary<string, string>
            {
                { "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" }
                // ^ lowercase when deserialised with CamelCasePropertyNamesContractResolver
            };
            List<SimplifiedTlsEntityState> states = new List<SimplifiedTlsEntityState>()
            {
                new SimplifiedTlsEntityState()
                {
                    Hostname = "exampledomain.co.uk",
                    SimplifiedTlsConnectionResults = new List<SimplifiedTlsConnectionResult>()
                    {
                         new SimplifiedTlsConnectionResult()
                         {
                             CertificateThumbprints = thumbprints,
                         }
                    },
                    Certificates = new Dictionary<string, string>(certificates)
                }
            };

            DomainTlsEvaluatorResults domainTlsEvaluatorResults =
                _domainTlsEvaluatorResultsFactory.Create("", preferences, states);

            Assert.AreEqual(thumbprints[0].ToUpper(), domainTlsEvaluatorResults.CertificateResults[0].Certificates[0].ThumbPrint);
            Assert.AreEqual(1, domainTlsEvaluatorResults.CertificateResults[0].Certificates.Count);
        }

        [Test]
        public void ContainsCorrectAssociatedIps()
        {
            var states = new List<SimplifiedTlsEntityState>
            {
                new SimplifiedTlsEntityState("testHostName1", "testIpAddress1")
                {
                    TlsLastUpdated = new DateTime(2000, 01, 01),
                    CertsLastUpdated = new DateTime(2000, 01, 02),
                },
                new SimplifiedTlsEntityState("testHostName1", "testIpAddress2")
                {
                    TlsLastUpdated = new DateTime(2000, 01, 03),
                    CertsLastUpdated = new DateTime(2000, 01, 04),
                },
                new SimplifiedTlsEntityState("testHostName2", "testIpAddress2")
                {
                    TlsLastUpdated = new DateTime(2000, 01, 03),
                    CertsLastUpdated = new DateTime(2000, 01, 04),
                },
                new SimplifiedTlsEntityState("testHostName2", "testIpAddress3")
                {
                    TlsLastUpdated = new DateTime(2000, 01, 05),
                    CertsLastUpdated = new DateTime(2000, 01, 06),
                },
            };

            var result = _domainTlsEvaluatorResultsFactory.Create("testDomain", EmptyPrefs, states);

            Assert.AreEqual("testDomain", result.Id);
            Assert.AreEqual(3, result.AssociatedIps.Count);
            Assert.That(
                result.AssociatedIps[0].IpAddress == "testIpAddress1" &&
                result.AssociatedIps[0].TlsLastUpdated == new DateTime(2000, 01, 01) &&
                result.AssociatedIps[0].CertsLastUpdated == new DateTime(2000, 01, 02) &&

                result.AssociatedIps[1].IpAddress == "testIpAddress2" &&
                result.AssociatedIps[1].TlsLastUpdated == new DateTime(2000, 01, 03) &&
                result.AssociatedIps[1].CertsLastUpdated == new DateTime(2000, 01, 04) &&

                result.AssociatedIps[2].IpAddress == "testIpAddress3" &&
                result.AssociatedIps[2].TlsLastUpdated == new DateTime(2000, 01, 05) &&
                result.AssociatedIps[2].CertsLastUpdated == new DateTime(2000, 01, 06)
            );
        }
    }
}