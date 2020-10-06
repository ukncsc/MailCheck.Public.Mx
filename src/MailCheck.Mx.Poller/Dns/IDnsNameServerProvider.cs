using System.Collections.Generic;
using System.Net;

namespace MailCheck.Mx.Poller.Dns
{
    public interface IDnsNameServerProvider
    {
        List<IPAddress> GetNameServers();
    }
}