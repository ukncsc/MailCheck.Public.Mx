using System.Collections.Generic;

namespace MailCheck.Mx.Api.Domain
{
    public class DomainListRequest
    {
        public DomainListRequest()
        {
            Domains = new List<string>();
        }

        public List<string> Domains { get; set; }
    }
}