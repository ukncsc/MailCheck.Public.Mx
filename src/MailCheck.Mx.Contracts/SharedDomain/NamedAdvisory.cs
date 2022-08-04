using System;
using MailCheck.Common.Contracts.Advisories;
using CommonMessageType = MailCheck.Common.Contracts.Advisories.MessageType;

namespace MailCheck.Mx.Contracts.SharedDomain
{
    public class NamedAdvisory : AdvisoryMessage
    {
        public string Name { get; }

        public NamedAdvisory(Guid id, string name, CommonMessageType messageType, string text, string markDown)
            : base(id, messageType, text, markDown)
        {
            Name = name;
        }
    }
}