using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
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

        public async IAsyncEnumerable<KafkaMessage> RunKafkaTesterServiceAsync(CancellationTokenSource cts, string groupId, KafkaSetting setting, Action<string> onError)
        {
            var conf = new ConsumerConfig
            { 
                GroupId = groupId,
                BootstrapServers = setting.Brokers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false
            };

            if (setting.IsSaslActivated)
            {
                conf.SaslMechanism = setting.SaslMechanism;
                conf.SecurityProtocol = setting.SecurityProtocol;
                conf.SaslUsername = setting.SaslUsername;
                conf.SaslPassword = setting.SaslPassword;
            }

            using (var c = new ConsumerBuilder<string, string>(conf).SetErrorHandler((consumer, error) =>
            {
                onError(error.Reason);
            }).Build())
            {
                c.Subscribe(setting.Topic);
                while (!cts.IsCancellationRequested)
                {
                    KafkaMessage message = null;
                    try
                    {
                        await Task.Run(() =>
                        {
                            var cr = c.Consume(cts.Token);
                            var messageValue = cr.Message.Value;
                            if (setting.IsGzipActivated && messageValue.StartsWith("H4sIAAAAAAAAA"))
                            {
                                messageValue = Decompress(Convert.FromBase64String(messageValue));
                            }
                            message = new KafkaMessage
                            {
                                Key = cr.Message.Key,
                                Message = messageValue,
                                MessageDateTime = cr.Message.Timestamp.UtcDateTime,
                                Partition = cr.TopicPartitionOffset.Partition.Value,
                                Offset = cr.TopicPartitionOffset.Offset.Value,
                                Headers = cr.Message.Headers.Select(h => new KafkaHeader { Key = h.Key, Value = System.Text.Encoding.Default.GetString(h.GetValueBytes()) }).ToList()
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

        public async Task<List<string>> GetTopicsAsync(KafkaSetting setting)
        {
            var conf = new AdminClientConfig
            {
                BootstrapServers = setting.Brokers
            };

            if (setting.IsSaslActivated)
            {
                conf.SaslMechanism = setting.SaslMechanism;
                conf.SecurityProtocol = setting.SecurityProtocol;
                conf.SaslUsername = setting.SaslUsername;
                conf.SaslPassword = setting.SaslPassword;
            }

            try
            {
                using (var c = new AdminClientBuilder(conf).Build())
                {
                    var metadata = await Task.Run(() => c.GetMetadata(TimeSpan.FromSeconds(10)));
                    return metadata.Topics.Select(t => t.Topic).ToList();
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occured: {e.Message}");
                return null;
            }
        }

        public async Task SendMessageAsync(string servers, string topic, KafkaMessage message)
        {
            _logger.LogInformation("Sending message...");
            var conf = new ConsumerConfig
            {
                BootstrapServers = servers
            };

            Headers headers = new Headers();
            foreach (var item in message.Headers)
            {
                if (item.Key == null || item.Value == null)
                    continue;

                headers.Add(item.Key, System.Text.Encoding.Default.GetBytes(item.Value));
            }

            using (var p = new ProducerBuilder<Null, string>(conf).Build())
            {
                await p.ProduceAsync(topic, new Message<Null, string> { Value = message.Message, Headers = headers });
            }
            _logger.LogInformation("Message sended");
        }

        private static string Decompress(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var memoryStreamOutput = new MemoryStream())
            {
                gZipStream.CopyTo(memoryStreamOutput);
                var outputBytes = memoryStreamOutput.ToArray();

                string decompressed = Encoding.UTF8.GetString(outputBytes);
                return decompressed;
            }
        }
    }
}
