using System;
using System.Collections.Generic;
using System.Text;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Contracts.TlsEntity
{
    public class TlsEntityCreated : Message
    {
        public TlsEntityCreated(string id)
            : base(id)
        {
        }

        public TlsState State => TlsState.Created;
    }
}
