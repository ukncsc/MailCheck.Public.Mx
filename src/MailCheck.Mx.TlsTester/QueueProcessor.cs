using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using MailCheck.Mx.TlsTester.Config;
using MailCheck.Mx.TlsTester.Domain;
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

        public MxQueueProcessor(IAmazonSQS sqs, IMxTesterConfig config)
        {
            _sqs = sqs;
            _config = config;
        }

        public async Task<List<TlsTestPending>> GetMxHosts()
        {
            Console.WriteLine("Starting processing SQS messages...");

            ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest(_config.SqsQueueUrl)
            {
                WaitTimeSeconds = 20, //Long polling
                MaxNumberOfMessages = 10
            };

            List<TlsTestPending> list = new List<TlsTestPending>();

            ReceiveMessageResponse receiveMessageResponse = await _sqs.ReceiveMessageAsync(receiveMessageRequest);
            
            Console.WriteLine($"Found {receiveMessageResponse.Messages.Count} messages");

            foreach (Message message in receiveMessageResponse.Messages)
            {
                TlsTestPending pendingTest = JsonConvert.DeserializeObject<TlsTestPending>(message.Body);
                pendingTest.MessageId = message.MessageId;
                pendingTest.ReceiptHandle = message.ReceiptHandle;
                list.Add(pendingTest);
            }
           
            return list;
        }

        public async Task DeleteMessage(string messageId, string receiptHandle)
        {
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
