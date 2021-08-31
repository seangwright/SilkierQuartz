using System.Collections.Generic;

namespace SilkierQuartz.Models
{
    public class TriggersViewModel
    {
        public IEnumerable<TriggerListItem> Triggers { get; set; }
        public IEnumerable<string> Groups { get; set; }
    }

    public class TriggerListItem
    {
        public string JobKey { get; set; }

        public string JobName { get; set; }

        public string JobGroup { get; set; }

        public string TriggerName { get; set; }

        public string TriggerGroup { get; set; }

        public bool IsPaused { get; set; }

        public TriggerType Type { get; set; }

        public string ClrType { get; set; }

        public string Description { get; set; }

        public string StartTime { get; set; }
        public string EndTime { get; set; }


        public string LastFireTime { get; set; }
        public string NextFireTime { get; set; }

        public string ScheduleDescription { get; set; }

        public HistogramData History { get; set; }

        public bool JobHeaderSeparator { get; set; }

        public string TypeIcon
        {
            get
            {
                switch (Type)
                {
                    case TriggerType.Cron:
                        return "clock outline";
                    case TriggerType.Simple:
                        return "redo";
                    case TriggerType.Daily:
                        return "tasks";
                    case TriggerType.Calendar:
                        return "calendar alternate outline";
                    default:
                        return "bug";
                }
            }
        }

        public string TypeString
        {
            get
            {
                if (Type == TriggerType.Unknown)
                    return ClrType;
                else
                    return Type.ToString();
            }
        }

        public Dictionary<string, string> EditRouteData() =>
            new Dictionary<string, string>()
            {
                {"group", TriggerGroup },
                {"name", TriggerName }
            };

        public Dictionary<string, string> EditJobRouteData() =>
            new Dictionary<string, string>()
            {
                { "group", JobGroup },
                { "name", JobName }
            };
    }
}