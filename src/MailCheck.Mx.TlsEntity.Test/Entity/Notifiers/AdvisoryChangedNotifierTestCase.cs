using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Mx.Contracts.Entity;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEntity.Test.Entity.Notifiers
{
    public class AdvisoryChangedNotifierTestCase
    {
        public TlsRecords CurrentTlsRecords { get; set; }
        public List<Error> CurrentCertErrors { get; set; }
        public TlsRecords NewTlsRecords { get; set; }
        public List<Error> NewCertErrors { get; set; }
        public List<string> Domains { get; set; }
        public int ExpectedConfigAdded { get; set; }
        public int ExpectedConfigSustained { get; set; }
        public int ExpectedConfigRemoved { get; set; }
        public int ExpectedCertAdded { get; set; }
        public int ExpectedCertSustained { get; set; }
        public int ExpectedCertRemoved { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return Description;
        }
    }
}