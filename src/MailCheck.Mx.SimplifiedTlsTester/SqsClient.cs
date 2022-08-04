using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using MailCheck.Common.Messaging.Common.Utils;
using MailCheck.Mx.Contracts.Simplified;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MailCheck.Mx.SimplifiedTlsTester
{
    public interface ISqsClient
    {
        Task<List<SimplifiedTlsTestPending>> GetTestsPending(CancellationToken cancellationToken);
        Task DeleteMessages(List<SimplifiedTlsTestPending> messages);
    }

    public class SqsClient : ISqsClient
    {
        private readonly IAmazonSQS _sqs;
        private readonly IProcessorConfig _config;
        private readonly ILogger<SqsClient> _log;

        public SqsClient(IAmazonSQS sqs, IProcessorConfig config, ILogger<SqsClient> log)
        {
            _sqs = sqs;
            _config = config;
            _log = log;
        }

        public async Task<List<SimplifiedTlsTestPending>> GetTestsPending(CancellationToken cancellationToken)
        {
            ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest(_config.SqsQueueUrl)
            {
                WaitTimeSeconds = 20,
                MaxNumberOfMessages = 10
            };

            _log.LogDebug("Starting long poll for SQS messages...");

            ReceiveMessageResponse receiveMessageResponse = await _sqs.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);

            _log.LogDebug($"Found {receiveMessageResponse.Messages.Count} SQS messages");

            List<SimplifiedTlsTestPending> list = new List<SimplifiedTlsTestPending>();

            foreach (Message message in receiveMessageResponse.Messages)
            {
                try
                {
                    _log.LogDebug($"Deserializing SQS message {message.MessageId}");
                    SimplifiedTlsTestPending pendingTest = JsonConvert.DeserializeObject<SimplifiedTlsTestPending>(message.Body);
                    pendingTest.MessageId = message.MessageId;
                    pendingTest.ReceiptHandle = message.ReceiptHandle;
                    if (message.Attributes.ContainsKey("SentTimestamp"))
                    {
                        pendingTest.Timestamp = message.Attributes["SentTimestamp"].MillisecondTimestampToDateTime();
                    }
                    list.Add(pendingTest);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"Deserializing SQS message failed for message {message.MessageId}");
                }
            }

            return list;
        }

        public async Task DeleteMessages(List<SimplifiedTlsTestPending> messages)
        {
            _log.LogDebug($"Deleting SQS messages {string.Join(",", messages.Select(x => x.MessageId))}");

            List<DeleteMessageBatchRequestEntry> entries = messages.Select(x => new DeleteMessageBatchRequestEntry(x.MessageId, x.ReceiptHandle)).ToList();
            DeleteMessageBatchRequest deleteMessageRequest = new DeleteMessageBatchRequest
            {
                QueueUrl = _config.SqsQueueUrl,
                Entries = entries
            };

            await _sqs.DeleteMessageBatchAsync(deleteMessageRequest);
        }
    }
}
