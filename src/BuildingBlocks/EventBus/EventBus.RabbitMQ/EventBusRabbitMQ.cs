using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        private RabbitMQConnection connection;
        private readonly IConnectionFactory connectionFactory;
        private readonly IModel rabbitMqChannel;
        public EventBusRabbitMQ(EventBusConfig config, IServiceProvider provider) : base(config, provider)
        {
            if (config.Connection != null)
            {
                var connJson = JsonConvert.SerializeObject(config.Connection, new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson);
            }
            else
            {
                connectionFactory = new ConnectionFactory();
            }
            connection = new RabbitMQConnection(connectionFactory, config.ConnectionRetryCount);
            rabbitMqChannel = CreateChannel();


            subscriptionManager.OnEventRemoved += OnEventRemoved;
        }

        private void OnEventRemoved(object? sender, string e)
        {
            var eventName = ProcessEventName(e);

            if (!connection.IsConnected)
            {
                connection.TryConnect();
            }

            rabbitMqChannel.QueueUnbind(queue: e,
                                        exchange: Config.DefaultTopicName,
                                        routingKey: e);

            if (subscriptionManager.IsEmpty)
            {
                rabbitMqChannel.Close();
            }
        }

        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;
            eventName = ProcessEventName(eventName);

            if (!connection.IsConnected)
            {
                connection.TryConnect();
            }

            var policy = RetryPolicy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(Config.ConnectionRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                });

            var message = JsonConvert.SerializeObject(@event);
            var messageBody = Encoding.UTF8.GetBytes(message);

            rabbitMqChannel.ExchangeDeclare(exchange: Config.DefaultTopicName, type: "direct");

            policy.Execute(() =>
            {
                var props = rabbitMqChannel.CreateBasicProperties();
                props.DeliveryMode = 2;

                rabbitMqChannel.QueueDeclare(queue: GetSubName(eventName), durable: true, exclusive: false, autoDelete: false, arguments: null);

                rabbitMqChannel.BasicPublish(exchange: Config.DefaultTopicName, 
                                             routingKey: eventName, 
                                             mandatory: true, 
                                             basicProperties: props, 
                                             body: messageBody);
            });

        }

        public override void Subscribe<T, TH>()
        {
            var eventName = subscriptionManager.GetEventKey<T>();

            if (!subscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                if (!connection.IsConnected)
                {
                    connection.TryConnect();
                }

                rabbitMqChannel.QueueDeclare(queue: GetSubName(eventName), durable: true, exclusive: false, autoDelete: false, arguments: null);

                rabbitMqChannel.QueueBind(queue: GetSubName(eventName), exchange: Config.DefaultTopicName, routingKey: eventName);

            }

            subscriptionManager.AddSubscription<T, TH>();

            StartConsumeChannel(eventName);

        }

        public override void Unsubscribe<T, TH>()
        {
            subscriptionManager.RemoveSubscription<T, TH>();
        }

        private IModel CreateChannel()
        {
            if (!connection.IsConnected)
            {
                connection.TryConnect();
            }

            var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: Config.DefaultTopicName, type: "direct");
            return channel;
        }

        private void StartConsumeChannel(string eventName)
        {
            if (rabbitMqChannel != null)
            {
                var consumer = new EventingBasicConsumer(rabbitMqChannel);

                consumer.Received += ConsumerReceived;

                rabbitMqChannel.BasicConsume(queue: GetSubName(eventName), autoAck: false, consumer: consumer);
            }
        }

        private async void ConsumerReceived(object? sender, BasicDeliverEventArgs e)
        {
            var eventName = e.RoutingKey;
            eventName = ProcessEventName(eventName);
            var message = Encoding.UTF8.GetString(e.Body.Span);
            try
            {
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex) { }

            rabbitMqChannel.BasicAck(e.DeliveryTag, multiple: false);
        }
    }
}
