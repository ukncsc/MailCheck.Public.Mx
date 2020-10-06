using System;
using System.Collections.Generic;
using System.Text;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.Contracts.TlsEntity
{
    public class TlsRecordEvaluationsChanged : Common.Messaging.Abstractions.Message
    {
        public TlsRecordEvaluationsChanged(string id, TlsRecords records, CertificateResults certificateResults) : base(id)
        {
            TlsRecords = records;
            CertificateResults = certificateResults;
        }

        public TlsRecords TlsRecords { get; }
        public CertificateResults CertificateResults { get; }

        public TlsState State => TlsState.Evaluated;
    }
}
