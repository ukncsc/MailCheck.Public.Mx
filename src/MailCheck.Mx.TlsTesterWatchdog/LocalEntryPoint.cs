using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

namespace MailCheck.Mx.TlsTesterWatchdog
{
    public static class LocalEntryPoint
    {
        public static async Task Main(string[] args)
        {
            var lep = new LambdaEntryPoint();
            var ser = new DefaultLambdaJsonSerializer();
            var context = new TestLambdaContext();

            Console.WriteLine("Enter message JSON and hit Enter...");

            string n;
            while((n = Console.ReadLine()) != null)
            {
                var s = new MemoryStream(Encoding.UTF8.GetBytes(n));
                var item = ser.Deserialize<SQSEvent>(s);
                await lep.FunctionHandler(item, context);
            }
        }

    }
}
