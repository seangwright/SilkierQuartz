using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory;
using SilkierQuartz.Helpers;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class SchedulerController : Controller
    {
        private readonly ISchedulerFactory factory;

        public SchedulerController(ISchedulerFactory factory)
        {
            this.factory = factory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);
            var histStore = scheduler.Context.GetExecutionHistoryStore();
            var metadata = await scheduler.GetMetaData(token);
            IReadOnlyCollection<JobKey> jobKeys = null;
            IReadOnlyCollection<TriggerKey> triggerKeys = null;
            if (!scheduler.IsShutdown)
            {
                try
                {
                    jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), token);
                    triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), token);
                }
                catch (NotImplementedException) { }
            }
            var currentlyExecutingJobs = await scheduler.GetCurrentlyExecutingJobs(token);
            IEnumerable<SchedulerGroupStateViewModel> pausedJobGroups = null;
            IEnumerable<SchedulerGroupStateViewModel> pausedTriggerGroups = null;
            IEnumerable<ExecutionHistoryEntry> execHistory = null;
            if (!scheduler.IsShutdown)
            {
                try
                {
                    pausedJobGroups = await GetGroupPauseState(await scheduler.GetJobGroupNames(token), async x => await scheduler.IsJobGroupPaused(x));
                }
                catch (NotImplementedException) { }

                try
                {
                    pausedTriggerGroups = await GetGroupPauseState(await scheduler.GetTriggerGroupNames(token), async x => await scheduler.IsTriggerGroupPaused(x));
                }
                catch (NotImplementedException) { }
            }

            int? failedJobs = null;
            int executedJobs = metadata.NumberOfJobsExecuted;

            if (histStore != null)
            {
                execHistory = await histStore?.FilterLast(10);
                executedJobs = await histStore?.GetTotalJobsExecuted();
                failedJobs = await histStore?.GetTotalJobsFailed();
            }

            var histogram = execHistory.ToHistogram(detailed: true) ?? HistogramData.CreateEmpty();

            histogram.BarWidth = 14;

            return View(new SchedulerViewModel
            {
                History = histogram,
                MetaData = metadata,
                RunningSince = metadata.RunningSince?.UtcDateTime.ToDefaultFormat() ?? "N / A",
                UtcLabel = DateTimeSettings.UseLocalTime ? string.Empty : "UTC",
                MachineName = Environment.MachineName,
                Application = Environment.CommandLine,
                JobsCount = jobKeys?.Count ?? 0,
                TriggerCount = triggerKeys?.Count ?? 0,
                ExecutingJobs = currentlyExecutingJobs.Count,
                ExecutedJobs = executedJobs,
                FailedJobs = failedJobs?.ToString(CultureInfo.InvariantCulture) ?? "N / A",
                JobGroups = pausedJobGroups,
                TriggerGroups = pausedTriggerGroups,
                HistoryEnabled = histStore != null,
            });
        }

        async Task<IEnumerable<SchedulerGroupStateViewModel>> GetGroupPauseState(IEnumerable<string> groups, Func<string, Task<bool>> func)
        {
            var result = new List<SchedulerGroupStateViewModel>();

            foreach (var name in groups.OrderBy(x => x, StringComparer.InvariantCultureIgnoreCase))
                result.Add(new SchedulerGroupStateViewModel { Name = name, IsPaused = await func(name) });

            return result;
        }

        public class ActionArgs
        {
            public string Action { get; set; }
            public string Name { get; set; }
            public string Groups { get; set; } // trigger-groups | job-groups
        }

        [HttpPost, JsonErrorResponse]
        public async Task Action([FromBody] ActionArgs args, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            switch (args.Action.ToLower())
            {
                case "shutdown":
                    await scheduler.Shutdown(token);
                    break;
                case "standby":
                    await scheduler.Standby(token);
                    break;
                case "start":
                    await scheduler.Start(token);
                    break;
                case "pause":
                    if (string.IsNullOrEmpty(args.Name))
                    {
                        await scheduler.PauseAll(token);
                    }
                    else
                    {
                        if (args.Groups == "trigger-groups")
                            await scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals(args.Name), token);
                        else if (args.Groups == "job-groups")
                            await scheduler.PauseJobs(GroupMatcher<JobKey>.GroupEquals(args.Name), token);
                        else
                            throw new InvalidOperationException("Invalid groups: " + args.Groups);
                    }
                    break;
                case "resume":
                    if (string.IsNullOrEmpty(args.Name))
                    {
                        await scheduler.ResumeAll(token);
                    }
                    else
                    {
                        if (args.Groups == "trigger-groups")
                            await scheduler.ResumeTriggers(GroupMatcher<TriggerKey>.GroupEquals(args.Name), token);
                        else if (args.Groups == "job-groups")
                            await scheduler.ResumeJobs(GroupMatcher<JobKey>.GroupEquals(args.Name), token);
                        else
                            throw new InvalidOperationException("Invalid groups: " + args.Groups);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid action: " + args.Action);
            }
        }
    }
}
