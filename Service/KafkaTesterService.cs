using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaTester.Model;
using Microsoft.Extensions.Logging;

namespace KafkaTester.Service
{
    public class KafkaTesterService
    {
        private readonly ILogger<KafkaTesterService> _logger;

        public KafkaTesterService(ILogger<KafkaTesterService> logger)
        {
            _logger = logger;
        }

        public async IAsyncEnumerable<KafkaMessage> RunKafkaTesterServiceAsync(CancellationTokenSource cts, string groupId, string servers, string topic, Action<string> onError)
        {
            var conf = new ConsumerConfig
            { 
                GroupId = groupId,
                BootstrapServers = servers,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var c = new ConsumerBuilder<Ignore, string>(conf).SetErrorHandler((consumer, error) =>
            {
                onError(error.Reason);
            }).Build())
            {
                c.Subscribe(topic);
                while (!cts.IsCancellationRequested)
                {
                    KafkaMessage message = null;
                    try
                    {
                        await Task.Run(() =>
                        {
                            var cr = c.Consume(cts.Token);
                            message = new KafkaMessage
                            {
                                Message = cr.Message.Value,
                                Partition = cr.TopicPartitionOffset.Partition.Value,
                                Offset = cr.TopicPartitionOffset.Offset.Value
                            };
                        });
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError($"Error occured: {e.Error.Reason}");
                        onError(e.Error.Reason);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ensure the consumer leaves the group cleanly and final offsets are committed.
                        c.Close();
                    }
                    yield return message;
                }
            }
        }

        public async Task SendMessageAsync(string servers, string topic, string message)
        {
            _logger.LogInformation("Sending message...");
            var conf = new ConsumerConfig
            {
                BootstrapServers = servers
            };

            using (var p = new ProducerBuilder<Null, string>(conf).Build())
            {
                await p.ProduceAsync(topic, new Message<Null, string> { Value = message });
            }
            _logger.LogInformation("Message sended");
        }
    }
}
