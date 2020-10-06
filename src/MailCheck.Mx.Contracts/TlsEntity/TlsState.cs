using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.Mx.Contracts.TlsEntity
{
    public enum TlsState
    {
        Created,
        PollPending,
        EvaluationPending,
        Unchanged,
        Evaluated
    }
}
