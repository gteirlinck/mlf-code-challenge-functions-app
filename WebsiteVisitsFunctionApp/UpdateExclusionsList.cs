using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace WebsiteVisitsFunctionApp
{
    public static class UpdateExclusionsList
    {
        [FunctionName("UpdateExclusionsList")]
        public static void Run([TimerTrigger("0 0 6 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
