using MailCheck.Mx.Contracts.Entity;

namespace MailCheck.Mx.Entity.Entity.Notifiers
{
    public interface IChangeNotifier
    {
        void Handle(MxEntityState state, Common.Messaging.Abstractions.Message message);
    }
}