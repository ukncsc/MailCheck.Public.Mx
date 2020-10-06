using System.Collections.Generic;
using MailCheck.Mx.Contracts.Entity;

namespace MailCheck.Mx.TlsEntity.Entity.Notifiers
{
    public interface IChangeNotifiersComposite : IChangeNotifier
    {
    }

    public class ChangeNotifiersComposite : IChangeNotifiersComposite
    {
        private readonly IEnumerable<IChangeNotifier> _notifiers;

        public ChangeNotifiersComposite(IEnumerable<IChangeNotifier> notifiers)
        {
            _notifiers = notifiers;
        }

        public void Handle(TlsEntityState state, Common.Messaging.Abstractions.Message message)
        {
            foreach (IChangeNotifier changeNotifier in _notifiers)
            {
                changeNotifier.Handle(state, message);
            }
        }
    }
}