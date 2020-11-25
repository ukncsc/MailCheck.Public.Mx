using System.Collections.Generic;
using MailCheck.Mx.Contracts.Entity;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public interface IChangeNotifier
    {
        void Handle(TlsEntityState state, Common.Messaging.Abstractions.Message message, List<string> domains);
    }
}