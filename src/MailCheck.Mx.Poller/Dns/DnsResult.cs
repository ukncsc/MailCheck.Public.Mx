namespace MailCheck.Mx.Poller.Dns
{
    public class DnsResult<T>
        where T : class
    {
        public DnsResult(T value, int messageSize)
            : this(value, null, null)
        {
            MessageSize = messageSize;
        }

        public DnsResult(string error, string auditTrail)
            : this(null, error, auditTrail) { }

        private DnsResult(T value, string error, string auditTrail)
        {
            Error = error;
            AuditTrail = auditTrail;
            Value = value;
        }

        public int MessageSize { get; }

        public string Error { get; }

        public string AuditTrail { get; }

        public bool IsErrored => Error != null;

        public T Value { get; }
    }
}