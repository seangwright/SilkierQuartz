using Quartz.Plugins.RecentHistory;
using System.Collections.Generic;

namespace SilkierQuartz.Models
{
    public class HistoryViewModel
    {
        public ExecutionHistoryEntry Entity { get; set; }
        public string JobGroup { get; set; }
        public string JobName { get; set; }
        public string TriggerGroup { get; set; }
        public string TriggerName { get; set; }
        public string ScheduledFireTimeUtc { get; set; }
        public string ActualFireTimeUtc { get; set; }
        public string FinishedTimeUtc { get; set; }
        public string Duration { get; set; }
        public string State { get; set; }
        public string StateIcon { get; set; }

        public Dictionary<string, string> JobsRouteData() =>
            new Dictionary<string, string>()
            {
                { "group", JobGroup },
                { "name", JobName }
            };

        public Dictionary<string, string> TriggersRouteData() =>
            new Dictionary<string, string>()
            {
                { "group", TriggerGroup },
                { "name", TriggerName }
            };
    }
}
