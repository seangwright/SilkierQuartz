using System.Collections.Generic;

namespace SilkierQuartz.Models
{
    public class JobTriggerViewModel
    {
        public JobDataMapModel DataMap { get; set; }
        public string Group { get; set; }
        public string JobName { get; set; }

        public Dictionary<string, string> TriggerEditRouteData() =>
            new Dictionary<string, string>()
            {
                { "group", Group },
                { "name", JobName }
            };
    }

    public class JobDataMapModel
    {
        public List<JobDataMapItem> Items { get; } = new List<JobDataMapItem>();
        public JobDataMapItem Template { get; set; }
    }
}
