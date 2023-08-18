using EventBus.Base.Abstract;
using EventBus.Base.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.SubscriptionManager
{
    public class InMemorySubscriptionManager : IEventBusSubscriptionManager
    {
        public InMemorySubscriptionManager(Func<string, string> eventNameGetter)
        {
            _handlers = new Dictionary<string, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
            this.eventNameGetter = eventNameGetter;
        }

        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;

        public event EventHandler<string> OnEventRemoved;
        public Func<string, string> eventNameGetter;


        public bool IsEmpty => !_handlers.Keys.Any();


        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();

            AddSubscription(typeof(TH), eventName);

            if (!_eventTypes.Contains(typeof(T)))
            {
                _eventTypes.Add(typeof(T));
            }
        }

        private void AddSubscription(Type type, string eventName)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                _handlers.Add(eventName, new List<SubscriptionInfo>());
            }
            if (_handlers[eventName].Any(x => x.HandlerType == type))
            {
                throw new Exception();
            }
            _handlers[eventName].Add(SubscriptionInfo.Typed(type));
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        public string GetEventKey<T>()
        {
            string eventName = typeof(T).Name;
            return eventNameGetter(eventName);
        }

        public Type GetEventTypeName(string eventName)
        {
            return _eventTypes.SingleOrDefault(x => x.Name == eventName);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName)
        {
            return _handlers[eventName];
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var eventKey = GetEventKey<T>();

            return GetHandlersForEvent(eventKey);

        }

        public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            var eventName = GetEventKey<T>();

            return HasSubscriptionsForEvent(eventName);

        }

        public bool HasSubscriptionsForEvent(string eventName)
        {
            return _handlers.ContainsKey(eventName);
        }

        public void RemoveSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var handlerToDelete = FindSubsToDelete<T, TH>();
            var eventKey = GetEventKey<T>();
            RemoveHandler(handlerToDelete, eventKey);
        }

        private void RemoveHandler(SubscriptionInfo handlerToDelete, string eventKey)
        {
            if (handlerToDelete != null)
            {
                _handlers[eventKey].Remove(handlerToDelete);

                if (!_handlers[eventKey].Any())
                {
                    _handlers.Remove(eventKey);
                    var eventType = _eventTypes.SingleOrDefault(x => x.Name == eventKey);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RaiseEventRemoved(eventKey);
                }
            }

        }

        private void RaiseEventRemoved(string eventKey)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventKey);
        }

        private SubscriptionInfo FindSubsToDelete<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventKey = GetEventKey<T>();
            return FindSubsToDelete(eventKey, typeof(TH));

        }

        private SubscriptionInfo FindSubsToDelete(string eventKey, Type type)
        {
            if (!HasSubscriptionsForEvent(eventKey))
                return null;
            return _handlers[eventKey].SingleOrDefault(x => x.HandlerType == type);
        }
    }
}
