using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.Mx.Poller.Domain
{
    public class Error
    {
        public Error(Guid id, ErrorType errorType, string message, string markdown)
        {
            Id = id;
            ErrorType = errorType;
            Message = message;
            Markdown = markdown;
        }

        public Guid Id { get; }
        public ErrorType ErrorType { get; }

        public string Message { get; }
        public string Markdown { get; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(ErrorType)}: {ErrorType}, {nameof(Message)}: {Message}";
        }
    }
}
