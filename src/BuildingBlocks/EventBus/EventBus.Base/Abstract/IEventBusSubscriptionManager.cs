using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Abstract
{
    //Subscriptions will be hold in a classes which implements this interface.
    public interface IEventBusSubscriptionManager
    {
        bool IsEmpty { get; }

        event EventHandler<string> OnEventRemoved;

        void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        bool HasSubscriptionsForEvent<T>()
            where T : IntegrationEvent;

        bool HasSubscriptionsForEvent(string eventName);

        Type GetEventTypeName(string eventName);

        string GetEventKey<T>();

        void Clear();

        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>()
            where T : IntegrationEvent;




    }
}
