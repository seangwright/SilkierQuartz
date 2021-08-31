using System.Collections.Generic;

namespace SilkierQuartz.Models
{
    public class ExecutionViewModel
    {
        public string Id { get; set; }
        public string JobGroup { get; set; }
        public string JobName { get; set; }
        public string TriggerGroup { get; set; }
        public string TriggerName { get; set; }
        public string ScheduledFireTime { get; set; }
        public string ActualFireTime { get; set; }
        public string RunTime { get; set; }

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
