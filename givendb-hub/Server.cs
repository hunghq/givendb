using System;
using System.Text;
using GivenDb.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GivenDb.Hub
{
    class Server : IDisposable
    {
        private readonly IConnectionFactory connectionFactory;
        private IConnection connection;
        private IModel channel;

        public Server()
        {
            connectionFactory = new ConnectionFactory()
            {
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                HostName = "localhost"
            };
        }

        /// <summary>
        /// Hub starts one exchanged named "givendb"
        /// Message: Start test session (sessionId) -> create a new queue
        /// Message: Create database (sessionId, schema) -> route to the corresponding queue
        /// </summary>
        internal void Start()
        {
            connection = connectionFactory.CreateConnection();

            channel = connection.CreateModel();
            channel.ExchangeDeclare(Constants.ExchangeMaster, ExchangeType.Direct, durable: false, autoDelete: true);
            RegisterQueueSession();
        }

        private void RegisterQueueSession()
        {
            channel.QueueDeclare(Constants.Queues.Sessions, durable: false, exclusive: false, autoDelete: true);
            channel.QueueBind(Constants.Queues.Sessions, Constants.ExchangeMaster, Constants.RoutingKey.NewSession);

            RegisterConsumer();

            void RegisterConsumer()
            {
                var consumer = new EventingBasicConsumer(channel);
                channel.BasicConsume(Constants.Queues.Sessions, autoAck: false, consumer: consumer);

                consumer.Received += (_, arg) =>
                {
                    // Processing
                    var dbType = Encoding.UTF8.GetString(arg.Body);
                    Console.WriteLine($"Processing the message: dbType = {dbType}");
                    string sessionId = $"{Constants.Queues.Sessions}/{Guid.NewGuid().ToString()}";

                    // Create new queue to handle session requests
                    Console.WriteLine($"Create queue '{sessionId}' with routing key of the same name");
                    channel.QueueDeclare(sessionId, durable: false, exclusive: false, autoDelete: true);
                    channel.QueueBind(sessionId, Constants.ExchangeMaster, sessionId);

                    // Reply
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = arg.BasicProperties.CorrelationId;
                    replyProps.ReplyTo = sessionId;

                    var replyBody = Encoding.UTF8.GetBytes($"Send create db request using this binding key: {sessionId}");

                    channel.BasicPublish(
                        Constants.ExchangeMaster,
                        routingKey: arg.BasicProperties.ReplyTo,
                        basicProperties: replyProps,
                        body: replyBody);

                    // Acknowledge
                    channel.BasicAck(arg.DeliveryTag, multiple: false);
                };
            }
        }

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue) return;

            if (disposing)
            {
                channel?.Close();
                connection?.Close();
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}