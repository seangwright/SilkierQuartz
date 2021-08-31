using Quartz;
using Quartz.Impl.Matchers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilkierQuartz
{
    public class Cache
    {
        public Cache(ISchedulerFactory factory)
        {
            this.factory = factory;
        }

        private string[] jobTypes;
        private readonly ISchedulerFactory factory;

        public async Task<string[]> GetJobTypes(CancellationToken token = default)
        {
            if (jobTypes == null)
            {
                var scheduler = await factory.GetScheduler(token);
                var keys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), token);
                var knownTypes = new List<string>();

                foreach (var key in keys)
                {
                    var detail = await scheduler.GetJobDetail(key, token);
                    knownTypes.Add(detail.JobType.RemoveAssemblyDetails());
                }

                lock (this)
                {
                    if (jobTypes == null)
                    {
                        UpdateJobTypes(knownTypes);
                    }
                }
            }
            return jobTypes;
        }

        public void UpdateJobTypes(IEnumerable<string> list)
        {
            if (jobTypes != null)
                list = list.Concat(jobTypes); // append existing types
            jobTypes = list.Distinct().OrderBy(x => x).ToArray();
        }

    }
}
