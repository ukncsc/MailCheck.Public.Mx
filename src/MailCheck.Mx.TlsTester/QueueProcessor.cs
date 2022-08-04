using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Domain;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MailCheck.Mx.TlsTester
{
    public interface IMxQueueProcessor
    {
        Task<List<TlsTestPending>> GetMxHosts();
        Task DeleteMessage(string messageId, string receiptHandle);
    }
    public class MxQueueProcessor : IMxQueueProcessor
    {
        private readonly IAmazonSQS _sqs;
        private readonly IMxTesterConfig _config;
        private readonly ILogger<MxQueueProcessor> _log;

        public MxQueueProcessor(IAmazonSQS sqs, IMxTesterConfig config, ILogger<MxQueueProcessor> log)
        {
            _sqs = sqs;
            _config = config;
            _log = log;
        }

        public async Task<List<TlsTestPending>> GetMxHosts()
        {
            ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest(_config.SqsQueueUrl)
            {
                WaitTimeSeconds = 20, //Long polling
                MaxNumberOfMessages = 10 // 10 is the max
            };

            _log.LogDebug("Starting long poll for SQS messages...");

            ReceiveMessageResponse receiveMessageResponse = await _sqs.ReceiveMessageAsync(receiveMessageRequest);

            _log.LogDebug($"Found {receiveMessageResponse.Messages.Count} SQS messages");

            List<TlsTestPending> list = new List<TlsTestPending>();

            foreach (Message message in receiveMessageResponse.Messages)
            {
                try
                {
                    _log.LogInformation($"Deserializing SQS message {message.MessageId}");
                    TlsTestPending pendingTest = JsonConvert.DeserializeObject<TlsTestPending>(message.Body);
                    pendingTest.MessageId = message.MessageId;
                    pendingTest.ReceiptHandle = message.ReceiptHandle;
                    list.Add(pendingTest);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"Deserializing SQS message failed for message {message.MessageId}");
                }
            }
           
            return list;
        }

        public async Task DeleteMessage(string messageId, string receiptHandle)
        {
            _log.LogInformation($"Deleting SQS message {messageId}");

            DeleteMessageBatchRequest deleteMessageRequest = new DeleteMessageBatchRequest
            {
                QueueUrl = _config.SqsQueueUrl,
                Entries = new List<DeleteMessageBatchRequestEntry>
                    {new DeleteMessageBatchRequestEntry(messageId, receiptHandle)}
            };

            await _sqs.DeleteMessageBatchAsync(deleteMessageRequest);
        }
    }
}
