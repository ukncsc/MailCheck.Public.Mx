using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.ECS;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MailCheck.Mx.TlsTesterWatchdog
{
    public class LambdaEntryPoint
    {
        private static readonly ILogger _logger;
        private static readonly string _alarmName;
        private static readonly string _clusterName;
        private static readonly string _serviceName;
        private static readonly AmazonECSClient _amazonECSClient;

        static LambdaEntryPoint()
        {
            _alarmName = Environment.GetEnvironmentVariable("RestartTriggerAlarmName");
            _clusterName = Environment.GetEnvironmentVariable("EcsClusterName");
            _serviceName = Environment.GetEnvironmentVariable("EcsServiceName");

            ILoggerFactory factory = new LoggerFactory()
                .AddLambdaLogger(new LambdaLoggerOptions 
                { 
                    IncludeScopes = true,
                    IncludeEventId = true,
                    IncludeException = true,
                    IncludeCategory = true,
                    IncludeLogLevel = true,
                });

            _logger = factory
                .CreateLogger<LambdaEntryPoint>();

            _amazonECSClient = new AmazonECSClient();
        }

        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
        {
            if (evnt == null) 
            {
                _logger.LogWarning("Argument evnt was null. Possible misconfiguration.");
                return;
            }
            
            foreach (var message in evnt.Records)
            {
                using (_logger.BeginScope(new Dictionary<string, object> { ["MessageId"] = message.MessageId }))
                {
                    try
                    {
                        var alarm = JsonSerializer.Deserialize<Alarm>(message.Body);

                        if (string.IsNullOrEmpty(alarm.AlarmArn))
                        {
                            _logger.LogWarning("Message was not an alarm. Possible misconfiguration.");
                            return;
                        }

                        if (alarm.AlarmArn != _alarmName)
                        {
                            _logger.LogWarning("Not the alarm type we're looking for. Possible misconfiguration.");
                            return;
                        }

                        if (!(alarm.OldStateValue != "ALARM" && alarm.NewStateValue == "ALARM"))
                        {
                            _logger.LogInformation("Alarm is not changing from NON-ALARM --> ALARM. Doing nothing.");
                            return;
                        }

                        var taskList = await _amazonECSClient.ListTasksAsync(new Amazon.ECS.Model.ListTasksRequest { Cluster = _clusterName, ServiceName = _serviceName });
                        if (taskList.TaskArns.Count == 0)
                        {
                            _logger.LogInformation($"No running tasks found for cluster {_clusterName} and service {_serviceName}");
                            return;
                        }

                        if (taskList.TaskArns.Count > 1)
                        {
                            _logger.LogInformation($"More than one task found for cluster {_clusterName} and service {_serviceName}. Doing nothing.");
                            return;
                        }

                        var taskArn = taskList.TaskArns[0];

                        var taskInfos = await _amazonECSClient.DescribeTasksAsync(new Amazon.ECS.Model.DescribeTasksRequest { Cluster = _clusterName, Tasks = new List<string>{ taskArn }});
                        if (taskInfos.Tasks.Count != 1)
                        {
                            _logger.LogInformation($"Expected 1 task returned by DescribeTask but found {taskInfos.Tasks.Count} for {taskArn} cluster {_clusterName} and service {_serviceName}");
                            return;
                        }

                        var taskInfo = taskInfos.Tasks[0];

                        if (taskInfo.LastStatus != "RUNNING")
                        {
                            _logger.LogInformation($"Task {taskArn} has status {taskInfo.LastStatus} for cluster {_clusterName} and service {_serviceName}. Doing nothing.");
                            return;
                        }

                        if (taskInfo.CreatedAt > DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)))
                        {
                            _logger.LogInformation($"Task {taskArn} started less than an hour ago (may have been recently restarted) for cluster {_clusterName} and service {_serviceName}. Doing nothing.");
                            return;
                        }

                        _logger.LogInformation($"Attempting to stop task {taskArn} for cluster {_clusterName} and service {_serviceName}.");
                        await _amazonECSClient.StopTaskAsync(new Amazon.ECS.Model.StopTaskRequest { Cluster = _clusterName, Task = taskArn, Reason = "Suspected TLS Tester in stalled state." });
                        _logger.LogInformation($"Stop request sent for task {taskArn} for cluster {_clusterName} and service {_serviceName}.");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception occurred processing message.");
                    }
                }
            }
        }
    }

    public class Alarm
    {
        public string AlarmArn { get; set; }

        public string OldStateValue { get; set; }

        public string NewStateValue { get; set; }
    }
}
