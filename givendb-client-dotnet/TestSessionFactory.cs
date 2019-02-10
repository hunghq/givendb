using GivenDb.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace GivenDb.Client
{
    public class TestSessionFactory : IDisposable
    {
        private readonly IConnectionFactory connectionFactory;
        private readonly string clientQueueName;
        private readonly string clientCorrelationId;
        private readonly BlockingCollection<TestSession> testSessions = new BlockingCollection<TestSession>();

        private IConnection connection;
        private IModel channel;

        public TestSessionFactory()
        {
            connectionFactory = new ConnectionFactory()
            {
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                HostName = "localhost"
            };

            clientCorrelationId = Guid.NewGuid().ToString();
            clientQueueName = $"givendb-client-{clientCorrelationId}";
        }

        public void Start()
        {
            connection = connectionFactory.CreateConnection();
            channel = connection.CreateModel();            
            channel.QueueDeclare(clientQueueName, durable: false, exclusive: true, autoDelete: true);
            channel.QueueBind(clientQueueName, Constants.ExchangeMaster, clientQueueName);
            
            // Handle reply for CreateTestSession
            var consumer = new EventingBasicConsumer(channel);
            channel.BasicConsume(clientQueueName, autoAck: true, consumer: consumer);

            consumer.Received += (_, arg) =>
            {
                if (arg.BasicProperties.CorrelationId == clientCorrelationId)
                {
                    var response = Encoding.UTF8.GetString(arg.Body);
                    Console.WriteLine($"Response: {response}");
                    testSessions.Add(new TestSession(arg.BasicProperties.ReplyTo));
                }
            };
        }

        public TestSession CreateTestSession(string dbType)
        {
            Console.WriteLine($"Publish message to create a new test session: dbType = {dbType}");
            var createTestSessionMessage = Encoding.UTF8.GetBytes(dbType);

            var props = channel.CreateBasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = 2;
            props.ReplyTo = clientQueueName;
            props.CorrelationId = clientCorrelationId;
            channel.BasicPublish(Constants.ExchangeMaster, Constants.RoutingKey.NewSession, props, createTestSessionMessage);

            var session = testSessions.Take();
            Console.WriteLine($"Response: {session.SessionId}.");
            return session;
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

    public class TestSession
    {
        public TestSession(string sessionId)
        {
            SessionId = sessionId;
        }

        public string SessionId { get; }
    }
}