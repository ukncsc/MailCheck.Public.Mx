using System;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.TlsEntity;

namespace MailCheck.Mx.Contracts.Entity
{
    public class TlsEntityState 
    {
        public TlsEntityState(string id) 
        {
            Id = id;
            TlsState = TlsState.Created;
            Created = DateTime.UtcNow;
        }

        public virtual string Id { get; }
        
        public virtual TlsState TlsState { get; set; }

        public virtual DateTime Created { get; }

        public virtual CertificateResults CertificateResults { get; set; }

        public virtual TlsRecords TlsRecords { get; set; }
        
        public virtual int FailureCount { get; set; }
        
        public virtual DateTime? LastUpdated { get; set; }
    }
}
