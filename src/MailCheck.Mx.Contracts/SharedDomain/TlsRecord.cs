using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class TlsRecord
    {
        public TlsEvaluatedResult TlsEvaluatedResult { get; set; }
        public BouncyCastleTlsTestResult BouncyCastleTlsTestResult { get; set; }

        public TlsRecord()
        {

        }

        public TlsRecord(TlsEvaluatedResult tlsEvaluatedResult) : this(tlsEvaluatedResult, null)
        {
        }

        public TlsRecord(TlsEvaluatedResult tlsEvaluatedResult, BouncyCastleTlsTestResult bouncyCastleTlsTestResult)
        {
            TlsEvaluatedResult = tlsEvaluatedResult;
            BouncyCastleTlsTestResult = bouncyCastleTlsTestResult;
        }
    }
}
