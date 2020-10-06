using System;
using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;

namespace MailCheck.Mx.Entity.Entity.Notifiers
{
    public class MessageEqualityComparer : IEqualityComparer<Message>
    {
        public bool Equals(Message x, Message y)
        {
            return x.Id.Equals(y.Id);
        }

        public int GetHashCode(Message obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}