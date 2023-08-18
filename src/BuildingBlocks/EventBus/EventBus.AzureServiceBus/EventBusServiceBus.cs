using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient topicClient;
        private ManagementClient managementClient;
        public EventBusServiceBus(EventBusConfig config, IServiceProvider provider) : base(config, provider)
        {
            managementClient = new ManagementClient(config.EventBusConnectionString);
            topicClient = CreateTopicClient();
        }

        private ITopicClient CreateTopicClient()
        {
            if(topicClient == null || topicClient.IsClosedOrClosing)
            {
                topicClient = new TopicClient(Config.EventBusConnectionString, Config.DefaultTopicName, RetryPolicy.Default);
            }
            if (managementClient.TopicExistsAsync(Config.DefaultTopicName).GetAwaiter().GetResult())
            {
                managementClient.CreateTopicAsync(Config.DefaultTopicName).GetAwaiter().GetResult();
            }
            return topicClient;
        }

        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;

            eventName = ProcessEventName(eventName);

            var eventStr = JsonConvert.SerializeObject(@event);
            var bodyArray = Encoding.UTF8.GetBytes(eventStr);

            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArray,
                Label = eventName
            };
            topicClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public override void Subscribe<T, TH>()
        {
            var eventName = subscriptionManager.GetEventKey<T>();

            if (!subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                CreateSubscription(eventName);  
            }
            



        }

        public override void Unsubscribe<T, TH>()
        {
            throw new NotImplementedException();
        }

        private SubscriptionClient CreateSubscription(string eventName)
        {
            return new SubscriptionClient(Config.EventBusConnectionString, Config.DefaultTopicName, GetSubName(eventName));
        }
    }
}
