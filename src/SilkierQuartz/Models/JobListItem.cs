using System.Collections.Generic;

namespace SilkierQuartz.Models
{
    public class JobListViewModel
    {
        public IEnumerable<JobListItem> Jobs { get; set; }
        public IEnumerable<string> Groups { get; set; }
    }

    public class JobListItem
    {
        public string JobName { get; set; }

        public string Group { get; set; }

        public string Type { get; set; }

        public string Description { get; set; }


        public bool Recovery { get; set; }

        public bool Persist { get; set; } // Persist job data

        public bool Concurrent { get; set; }

        public HistogramData History { get; set; }

        public Dictionary<string, string> EditRouteData() =>
            new Dictionary<string, string>()
            {
                {"name", JobName },
                {"group", Group }
            };
    }

    public class JobAdditionalDataViewModel
    {
        public string JobName { get; set; }
        public string Group { get; set; }
        public HistogramData History { get; set; }
        public string NextFireTime { get; set; }
    }
}
