using System;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Mx.Contracts.SharedDomain;
using MessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.SimplifiedTlsTester.Domain
{
    public static class Advisories
    {
        public static readonly NamedAdvisory U1 = new NamedAdvisory(
            new Guid("ef10c26e-5e6e-4ccb-974e-8966998bba25"),
            "mailcheck.tls.doesNotOfferStarttls",
            MessageType.error, 
            "This mailserver did not offer STARTTLS, and so does not support encrypted connections. Email to this mailserver is likely unencrypted. Or, if MTA-STS is published at 'enforce', many mailservers will not send email to this mailserver.", 
            null);

        public static readonly NamedAdvisory U2 = new NamedAdvisory(
            new Guid("0e93214d-3c5c-4b22-8dd5-1766a28411fb"),
            "mailcheck.tls.doesNotSupportTls12",
            MessageType.error, 
            "This mailserver does not support TLS 1.2 with any broadly used ciphersuites. As major mailservers only support TLS 1.2 with a limited set of ciphersuites, most email to this mailserver is likely unencrypted. Or, if MTA-STS is published at 'enforce', many mailservers will not send email to this mailserver.", 
            null);

        public static readonly NamedAdvisory P1 = new NamedAdvisory(
            new Guid("00f0b884-84a9-4cec-b7d1-d8903819a70a"),
            "mailcheck.tls.supportsTls12GoodCiphersuites",
            MessageType.success, 
            "This mailserver supports TLS 1.2 with recommended ciphersuites.", 
            null);

        public static readonly NamedAdvisory P2 = new NamedAdvisory(
            new Guid("264469de-483e-4a94-9d70-1c07aef26bf0"),
            "mailcheck.tls.supportsTls13GoodCiphersuites",
            MessageType.success, 
            "This mailserver supports TLS 1.3 with recommended ciphersuites.", 
            null);

        public static readonly NamedAdvisory I1 = new NamedAdvisory(
            new Guid("ee554c70-0536-4980-adce-5a31a7f1e85b"),
            "mailcheck.tls.doesNotSupportTls13",
            MessageType.info,
            "This mailserver does not support TLS 1.3 with the recommended ciphersuites.", 
            null);

        public static readonly NamedAdvisory A1 = new NamedAdvisory(
            new Guid("b12b44d0-b75f-4fcd-90ee-ea9f84d26ca1"),
            "mailcheck.tls.supportsTls12BadCiphersuites",
            MessageType.warning,
            "This mailserver supports TLS 1.2 but does not support recommended ciphersuites.", 
            null);

        public static readonly NamedAdvisory A2 = new NamedAdvisory(
            new Guid("9ddf78af-0fd9-4407-94b9-62d2687f26e1"),
            "mailcheck.tls.doesNotPreferRecommendedCiphersuites",
            MessageType.warning,
            "This mailserver supports TLS 1.2, but does not prefer recommended ciphersuites which may result in weaker ciphersuites being used with some senders.", 
            null);

        public static readonly NamedAdvisory A3 = new NamedAdvisory(
            new Guid("3ee73a82-3940-4865-bea3-6b3e5f85a083"),
            "mailcheck.tls.unableToCreateConnection",
            MessageType.warning,
            "We were unable to create a connection to the mail server. We will keep trying, so please check back later.",
            null);
    }
}