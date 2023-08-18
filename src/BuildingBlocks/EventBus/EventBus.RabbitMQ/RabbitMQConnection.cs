using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.RabbitMQ
{
    public class RabbitMQConnection : IRabbitMQConnection
    {
        private IConnection _connection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        public bool Disposed;

        private object _locker = new object();

        public RabbitMQConnection(IConnectionFactory factory, int retryCount)
        {
            _connectionFactory = factory;
            _retryCount = retryCount;
        }
        public bool IsConnected => _connection is { IsOpen: true } && !Disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMq connection is established...");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            _connection.Dispose();
        }

        public bool TryConnect()
        {
            lock (_locker)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                }
            );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });
            }
            if (IsConnected)
            {
                _connection.ConnectionShutdown += ConnectionShutDown;
                _connection.ConnectionBlocked += ConnectionBlocked;
                _connection.CallbackException += CallbackExp;
                return true;
            }
            return false;

        }

        private void CallbackExp(object? sender, CallbackExceptionEventArgs e)
        {
            if(Disposed) return;
            TryConnect();
        }

        private void ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            if (Disposed) return;
            TryConnect();
        }


        private void ConnectionShutDown(object? sender, ShutdownEventArgs e)
        {
            if (Disposed) return;
            TryConnect();
        }
    }
}
