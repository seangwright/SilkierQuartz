using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Plugins.RecentHistory;
using SilkierQuartz.Helpers;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class TriggersController : Controller
    {
        private readonly ISchedulerFactory factory;
        private readonly SilkierQuartzOptions options;

        public TriggersController(ISchedulerFactory factory, SilkierQuartzOptions options)
        {
            this.factory = factory;
            this.options = options;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);
            var keys = (await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), token)).OrderBy(x => x.ToString());
            var list = new List<TriggerListItem>();

            foreach (var key in keys)
            {
                var t = await GetTrigger(key, token);
                var state = await scheduler.GetTriggerState(key, token);

                list.Add(new TriggerListItem()
                {
                    Type = t.GetTriggerType(),
                    TriggerName = t.Key.Name,
                    TriggerGroup = t.Key.Group,
                    IsPaused = state == TriggerState.Paused,
                    JobKey = t.JobKey.ToString(),
                    JobGroup = t.JobKey.Group,
                    JobName = t.JobKey.Name,
                    ScheduleDescription = t.GetScheduleDescription(options),
                    History = HistogramData.Empty,
                    StartTime = t.StartTimeUtc.UtcDateTime.ToDefaultFormat(),
                    EndTime = t.FinalFireTimeUtc?.UtcDateTime.ToDefaultFormat(),
                    LastFireTime = t.GetPreviousFireTimeUtc()?.UtcDateTime.ToDefaultFormat(),
                    NextFireTime = t.GetNextFireTimeUtc()?.UtcDateTime.ToDefaultFormat(),
                    ClrType = t.GetType().Name,
                    Description = t.Description,
                });
            }

            var groups = (await scheduler.GetTriggerGroupNames(token)).GroupArray();

            list = list.OrderBy(x => x.NextFireTime).ToList();
            string prevKey = null;
            foreach (var item in list)
            {
                if (item.JobKey != prevKey)
                {
                    item.JobHeaderSeparator = true;
                    prevKey = item.JobKey;
                }
            }

            return View(new TriggersViewModel { Triggers = list, Groups = groups });
        }

        [HttpGet]
        public async Task<IActionResult> New(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);
            var model = await TriggerPropertiesViewModel.Create(scheduler);
            var jobDataMap = new JobDataMapModel() { Template = options.JobDataMapItemTemplate };

            model.IsNew = true;

            model.Type = TriggerType.Cron;
            model.Priority = 5;

            return View("Edit", new TriggerViewModel() { Trigger = model, DataMap = jobDataMap });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string name, string group, bool clone = false, CancellationToken token = default)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var scheduler = await factory.GetScheduler(token);
            var key = new TriggerKey(name, group);
            var trigger = await GetTrigger(key, token);

            var jobDataMap = new JobDataMapModel() { Template = options.JobDataMapItemTemplate };

            var model = await TriggerPropertiesViewModel.Create(scheduler);

            model.IsNew = clone;
            model.IsCopy = clone;
            model.Type = trigger.GetTriggerType();
            model.Job = trigger.JobKey.ToString();
            model.TriggerName = trigger.Key.Name;
            model.TriggerGroup = trigger.Key.Group;
            model.OldTriggerName = trigger.Key.Name;
            model.OldTriggerGroup = trigger.Key.Group;

            if (clone)
                model.TriggerName += " - Copy";

            // don't show start time in the past because rescheduling cause triggering missfire policies
            model.StartTimeUtc = trigger.StartTimeUtc > DateTimeOffset.UtcNow ? trigger.StartTimeUtc.UtcDateTime.ToDefaultFormat() : null;

            model.EndTimeUtc = trigger.EndTimeUtc?.UtcDateTime.ToDefaultFormat();

            model.CalendarName = trigger.CalendarName;
            model.Description = trigger.Description;
            model.Priority = trigger.Priority;

            model.MisfireInstruction = trigger.MisfireInstruction;

            switch (model.Type)
            {
                case TriggerType.Cron:
                    model.Cron = CronTriggerViewModel.FromTrigger((ICronTrigger)trigger);
                    break;
                case TriggerType.Simple:
                    model.Simple = SimpleTriggerViewModel.FromTrigger((ISimpleTrigger)trigger);
                    break;
                case TriggerType.Daily:
                    model.Daily = DailyTriggerViewModel.FromTrigger((IDailyTimeIntervalTrigger)trigger);
                    break;
                case TriggerType.Calendar:
                    model.Calendar = CalendarTriggerViewModel.FromTrigger((ICalendarIntervalTrigger)trigger);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported trigger type: " + trigger.GetType().AssemblyQualifiedName);
            }

            jobDataMap.Items.AddRange(trigger.GetJobDataMapModel(options));

            return View("Edit", new TriggerViewModel() { Trigger = model, DataMap = jobDataMap });
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Save([FromForm] TriggerViewModel model, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);
            var triggerModel = model.Trigger;
            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel();

            var result = new ValidationResult();

            model.Validate(result.Errors);
            ModelValidator.Validate(jobDataMap, result.Errors);

            if (result.Success)
            {
                var builder = TriggerBuilder.Create()
                    .WithIdentity(new TriggerKey(triggerModel.TriggerName, triggerModel.TriggerGroup))
                    .ForJob(jobKey: triggerModel.Job)
                    .UsingJobData(jobDataMap.GetQuartzJobDataMap())
                    .WithDescription(triggerModel.Description)
                    .WithPriority(triggerModel.PriorityOrDefault);

                builder.StartAt(triggerModel.GetStartTimeUtc() ?? DateTime.UtcNow);
                builder.EndAt(triggerModel.GetEndTimeUtc());

                if (!string.IsNullOrEmpty(triggerModel.CalendarName))
                    builder.ModifiedByCalendar(triggerModel.CalendarName);

                if (triggerModel.Type == TriggerType.Cron)
                    triggerModel.Cron.Apply(builder, triggerModel);
                if (triggerModel.Type == TriggerType.Simple)
                    triggerModel.Simple.Apply(builder, triggerModel);
                if (triggerModel.Type == TriggerType.Daily)
                    triggerModel.Daily.Apply(builder, triggerModel);
                if (triggerModel.Type == TriggerType.Calendar)
                    triggerModel.Calendar.Apply(builder, triggerModel);

                var trigger = builder.Build();

                if (triggerModel.IsNew)
                {
                    await scheduler.ScheduleJob(trigger, token);
                }
                else
                {
                    await scheduler.RescheduleJob(new TriggerKey(triggerModel.OldTriggerName, triggerModel.OldTriggerGroup), trigger, token);
                }
            }

            return Json(result);
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Delete([FromBody] KeyModel model, CancellationToken token)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var scheduler = await factory.GetScheduler(token);

            var key = model.ToTriggerKey();

            if (!await scheduler.UnscheduleJob(key, token))
                throw new InvalidOperationException("Cannot unschedule job " + key);

            return NoContent();
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Resume([FromBody] KeyModel model, CancellationToken token)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var scheduler = await factory.GetScheduler(token);
            await scheduler.ResumeTrigger(model.ToTriggerKey(), token);
            return NoContent();
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Pause([FromBody] KeyModel model, CancellationToken token)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var scheduler = await factory.GetScheduler(token);
            await scheduler.PauseTrigger(model.ToTriggerKey(), token);
            return NoContent();
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> PauseJob([FromBody] KeyModel model, CancellationToken token)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var scheduler = await factory.GetScheduler(token);
            await scheduler.PauseJob(model.ToJobKey(), token);
            return NoContent();
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> ResumeJob([FromBody] KeyModel model, CancellationToken token)
        {
            if (!EnsureValidKey(model)) return BadRequest();

            var scheduler = await factory.GetScheduler(token);
            await scheduler.ResumeJob(model.ToJobKey(), token);
            return NoContent();
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Cron()
        {
            var cron = (await Request.ReadAsStringAsync())?.Trim();
            if (string.IsNullOrEmpty(cron))
                return Json(new { Description = "", Next = new object[0] });

            string desc = "Invalid format.";

            try
            {
                desc = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(cron, options.CronExpressionOptions);
            }
            catch
            { }

            List<string> nextDates = new List<string>();

            try
            {
                var qce = new CronExpression(cron);
                DateTime dt = DateTime.Now;
                for (int i = 0; i < 10; i++)
                {
                    var next = qce.GetNextValidTimeAfter(dt);
                    if (next == null)
                        break;
                    nextDates.Add(next.Value.LocalDateTime.ToDefaultFormat());
                    dt = next.Value.LocalDateTime;
                }
            }
            catch
            { }

            return Json(new { Description = desc, Next = nextDates });
        }

        private async Task<ITrigger> GetTrigger(TriggerKey key, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);
            var trigger = await scheduler.GetTrigger(key, token);

            if (trigger == null)
                throw new InvalidOperationException("Trigger " + key + " not found.");

            return trigger;
        }

        [HttpGet, JsonErrorResponse]
        public async Task<IActionResult> AdditionalData(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);
            var keys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), token);
            var history = await scheduler.Context.GetExecutionHistoryStore().FilterLastOfEveryTrigger(10);
            var historyByTrigger = history.ToLookup(x => x.Trigger);

            var list = new List<TriggerAdditionalDataViewModel>();
            foreach (var key in keys)
            {
                list.Add(new TriggerAdditionalDataViewModel
                {
                    TriggerName = key.Name,
                    TriggerGroup = key.Group,
                    History = historyByTrigger.TryGet(key.ToString()).ToHistogram(),
                });
            }

            return View(list);
        }


        [HttpGet]
        public Task<IActionResult> Duplicate(string name, string group, CancellationToken token)
        {
            return Edit(name, group, clone: true, token: token);
        }

        bool EnsureValidKey(string name, string group) => !(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(group));
        bool EnsureValidKey(KeyModel model) => EnsureValidKey(model.Name, model.Group);
    }
}
