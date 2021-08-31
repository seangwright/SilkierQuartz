using Quartz;
using System.Collections.Generic;

namespace SilkierQuartz.Models
{
    public class SchedulerViewModel
    {
        public HistogramData History { get; set; }
        public SchedulerMetaData MetaData { get; set; }
        public string RunningSince { get; set; }
        public string UtcLabel { get; set; }
        public string MachineName { get; set; }
        public string Application { get; set; }
        public int JobsCount { get; set; }
        public int TriggerCount { get; set; }
        public int ExecutingJobs { get; set; }
        public int ExecutedJobs { get; set; }
        public string FailedJobs { get; set; }
        public IEnumerable<SchedulerGroupStateViewModel> JobGroups { get; set; }
        public IEnumerable<SchedulerGroupStateViewModel> TriggerGroups { get; set; }
        public bool HistoryEnabled { get; set; }
    }

    public class SchedulerGroupStateViewModel
    {
        public string Name { get; set; }
        public bool IsPaused { get; set; }
    }
}
