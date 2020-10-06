using System;
using System.Collections.Generic;
using System.Text;
using MailCheck.Mx.Contracts.Tester;

namespace MailCheck.Mx.TlsTester.Domain
{
    public class MxHostTestDetails
    {
        public MxHostTestDetails(TlsTestResults testResults, string messageId, string receiptHandle)
        {
            TestResults = testResults;
            ReceiptHandle = receiptHandle;
            MessageId = messageId;
        }

        public TlsTestResults TestResults { get; }
        public string MessageId { get;  }
        public string ReceiptHandle { get; }
    }
}
