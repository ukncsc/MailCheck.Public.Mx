namespace MailCheck.Mx.Poller.Dns
{
    public class DnsResult<T>
        where T : class
    {
        public DnsResult(T value, int messageSize)
            : this(value, null)
        {
            MessageSize = messageSize;
        }

        public DnsResult(string error)
            : this(null, error) { }

        private DnsResult(T value, string error)
        {
            Error = error;
            Value = value;
        }

        public int MessageSize { get; }

        public string Error { get; }

        public bool IsErrored => Error != null;

        public T Value { get; }
    }
}