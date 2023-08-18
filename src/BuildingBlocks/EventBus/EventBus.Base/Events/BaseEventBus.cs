using EventBus.Base.Abstract;
using EventBus.Base.SubscriptionManager;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        public BaseEventBus(EventBusConfig eBConfig, IServiceProvider sProvider)
        {
            Config = eBConfig;
            serviceProvider = sProvider;
            subscriptionManager = new InMemorySubscriptionManager(ProcessEventName);
        }

        public readonly IServiceProvider serviceProvider;
        public readonly IEventBusSubscriptionManager subscriptionManager;
        public EventBusConfig Config { get; set; }

        public abstract void Publish(IntegrationEvent @event);

        public abstract void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        public abstract void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;


        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = ProcessEventName(eventName);

            var IsProcessed = false;

            if (subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subs = subscriptionManager.GetHandlersForEvent(eventName);

                using (var scope = serviceProvider.CreateScope())
                {
                    foreach (var sub in subs)
                    {
                        var handler = serviceProvider.GetService(sub.HandlerType);
                        if (handler == null)
                            continue;

                        var eventType = subscriptionManager.GetEventTypeName($"{Config.EventNamePrefix}{eventName}{Config.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
                IsProcessed = true;
            }
            return IsProcessed;
        }


        public string ProcessEventName(string eventName)
        {
            if (Config.DeleteEventPrefix)
                eventName = eventName.TrimStart(Config.EventNamePrefix.ToArray());
            if (Config.DeleteEventSuffix)
                eventName = eventName.TrimEnd(Config.EventNameSuffix.ToArray());

            return eventName;
        }

        public virtual void Dispose()
        {
            Config = null;
        }
        public virtual string GetSubName(string eventName)
        {
            return $"{Config.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }


    }
}
