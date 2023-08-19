using EventBus.Base;
using EventBus.Base.Abstract;
using EventBus.Factory;
using EventBus.UnitTest.Events.Event;
using EventBus.UnitTest.Events.EventHandler;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Security.Authentication.ExtendedProtection;

namespace EventBus.UnitTest
{
    [TestClass]
    public class EventBusTests
    {
        private ServiceCollection services;
        public EventBusTests()
        {
            services = new ServiceCollection();
        }

        [TestMethod]
        public void Sub_Event_RabbitMQ()
        {
            //Arrange
            services.AddSingleton<IEventBus>(s =>
            {
                return EventBusFactory.Create(GetRabbitMQConfig(), s);

            });

            var sp = services.BuildServiceProvider();

            var rabbitMqBus = sp.GetRequiredService<IEventBus>();
            //Action
            rabbitMqBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
            rabbitMqBus.Unsubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();


        }

        [TestMethod]
        public void SendMessage_Event_RabbitMQ()
        {
            //Arrange
            services.AddSingleton<IEventBus>(s =>
            {
                return EventBusFactory.Create(GetRabbitMQConfig(), s);

            });

            var sp = services.BuildServiceProvider();

            var rabbitMqBus = sp.GetRequiredService<IEventBus>();
            //Action
            rabbitMqBus.Publish(new OrderCreatedIntegrationEvent(11));


        }

        [TestMethod]
        public void ConsumeMessage_Event_RabbitMQ()
        {
            //Arrange
            services.AddSingleton<IEventBus>(s =>
            {
                return EventBusFactory.Create(GetRabbitMQConfig(), s);
            });

            var sp = services.BuildServiceProvider();

            var rabbitMqBus = sp.GetRequiredService<IEventBus>();
            //Action
            rabbitMqBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();
            
        }

        private EventBusConfig GetRabbitMQConfig() //created as static bc Azure is not implemented yet....
        {
            return new()
            {
                ConnectionRetryCount = 7,
                SubscriberClientAppName = "UnitTest",
                DefaultTopicName = "MicroservicesTest",
                EventBusType = EventBusType.RabbitMQ,
                EventNameSuffix = "IntegrationEvent",

            };
        }





    }
}