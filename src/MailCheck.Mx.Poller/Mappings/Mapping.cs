using System;
using MailCheck.Mx.Contracts.Poller;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Poller.Domain;
using Error = MailCheck.Mx.Poller.Domain.Error;
using ErrorType = MailCheck.Mx.Poller.Domain.ErrorType;

namespace MailCheck.Mx.Poller.Mappings
{
    public static class Mapping
    {
        public static MxRecordsPolled ToMxRecordsPolled(this MxPollResult pollResult)
        {
            return new MxRecordsPolled(pollResult.Id, pollResult.Records, pollResult.Elapsed, pollResult.Error?.ToMessage());
        }

        public static Message ToMessage(this Error error)
        {
            return new Message(error.Id, "MxPoller", GetMessageType(error.ErrorType), error.Message, error.Markdown);
        }

        private static MessageType GetMessageType(ErrorType type)
        {
            switch (type)
            {
                case ErrorType.Error: return MessageType.error;
                case ErrorType.Info: return MessageType.info;
                case ErrorType.Warning: return MessageType.warning;
            }

            throw new InvalidOperationException("Invalid error type");
        }
    }
}
