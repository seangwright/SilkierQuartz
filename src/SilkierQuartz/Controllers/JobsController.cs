using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class JobsController : Controller
    {
        private readonly ISchedulerFactory factory;
        private readonly Cache cache;
        private readonly SilkierQuartzOptions options;

        public JobsController(ISchedulerFactory factory, Cache cache, SilkierQuartzOptions options)
        {
            this.factory = factory;
            this.cache = cache;
            this.options = options;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var keys = (await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), token)).OrderBy(x => x.ToString());
            var list = new List<JobListItem>();
            var knownTypes = new List<string>();

            foreach (var key in keys)
            {
                var detail = await GetJobDetail(key, token);
                var item = new JobListItem()
                {
                    Concurrent = !detail.ConcurrentExecutionDisallowed,
                    Persist = detail.PersistJobDataAfterExecution,
                    Recovery = detail.RequestsRecovery,
                    JobName = key.Name,
                    Group = key.Group,
                    Type = detail.JobType.FullName,
                    History = HistogramData.Empty,
                    Description = detail.Description,
                };
                knownTypes.Add(detail.JobType.RemoveAssemblyDetails());
                list.Add(item);
            }

            cache.UpdateJobTypes(knownTypes);

            var groups = (await scheduler.GetJobGroupNames(token)).GroupArray();

            return View(new JobListViewModel { Jobs = list, Groups = groups });
        }

        [HttpGet]
        public async Task<IActionResult> New(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var job = new JobPropertiesViewModel() { IsNew = true };
            var jobDataMap = new JobDataMapModel() { Template = options.JobDataMapItemTemplate };

            job.Group = SchedulerConstants.DefaultGroup;
            job.GroupList = (await scheduler.GetJobGroupNames(token))
                .GroupArray()
                .Select(g => new SelectListItem(g, g, string.Equals(g, job.Group)))
                .Prepend(new SelectListItem("Group", "", string.IsNullOrWhiteSpace(job.Group)))
                .ToArray();
            job.TypeList = (await cache.GetJobTypes(token))
                .Select(t => new SelectListItem(t, t, string.Equals(t, job.Type)))
                .Prepend(new SelectListItem("Fully Qualified Type Name", "", string.IsNullOrWhiteSpace(job.Type)))
                .ToArray();

            return View("Edit", new JobViewModel() { Job = job, DataMap = jobDataMap });
        }

        [HttpGet]
        public async Task<IActionResult> Trigger(string name, string group, CancellationToken token)
        {
            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobKey = JobKey.Create(name, group);
            var job = await GetJobDetail(jobKey, token);
            var jobDataMap = new JobDataMapModel() { Template = options.JobDataMapItemTemplate };

            jobDataMap.Items.AddRange(job.GetJobDataMapModel(options));

            return View(new JobTriggerViewModel { DataMap = jobDataMap, JobName = name, Group = group });
        }

        [HttpPost, ActionName("Trigger"), JsonErrorResponse]
        public async Task<IActionResult> PostTrigger(string name, string group, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel();

            var result = new ValidationResult();

            ModelValidator.Validate(jobDataMap, result.Errors);

            if (result.Success)
            {
                await scheduler.TriggerJob(JobKey.Create(name, group), jobDataMap.GetQuartzJobDataMap(), token);
            }

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string name, string group, bool clone = false, CancellationToken token = default)
        {
            var scheduler = await factory.GetScheduler(token);

            if (!EnsureValidKey(name, group)) return BadRequest();

            var jobKey = JobKey.Create(name, group);
            var job = await GetJobDetail(jobKey, token);

            var jobModel = new JobPropertiesViewModel() { };
            var jobDataMap = new JobDataMapModel() { Template = options.JobDataMapItemTemplate };

            jobModel.IsNew = clone;
            jobModel.IsCopy = clone;
            jobModel.JobName = name;
            jobModel.Group = group;
            jobModel.GroupList = (await scheduler.GetJobGroupNames(token))
                .GroupArray()
                .Select(g => new SelectListItem(g, g, string.Equals(g, jobModel.Group)))
                .Prepend(new SelectListItem("Group", "", string.IsNullOrWhiteSpace(jobModel.Group)))
                .ToArray();

            jobModel.Type = job.JobType.RemoveAssemblyDetails();
            jobModel.TypeList = (await cache.GetJobTypes(token))
                .Select(t => new SelectListItem(t, t, string.Equals(t, jobModel.Type)))
                .Prepend(new SelectListItem("Fully Qualified Type Name", "", string.IsNullOrWhiteSpace(jobModel.Type)))
                .ToArray();

            jobModel.Description = job.Description;
            jobModel.Recovery = job.RequestsRecovery;

            if (clone)
                jobModel.JobName += " - Copy";

            jobDataMap.Items.AddRange(job.GetJobDataMapModel(options));

            return View("Edit", new JobViewModel() { Job = jobModel, DataMap = jobDataMap });
        }

        private async Task<IJobDetail> GetJobDetail(JobKey key, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var job = await scheduler.GetJobDetail(key, token);

            if (job == null)
                throw new InvalidOperationException("Job " + key + " not found.");

            return job;
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Save([FromForm] JobViewModel model, bool trigger, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var jobModel = model.Job;
            var jobDataMap = (await Request.GetJobDataMapForm()).GetModel();

            var result = new ValidationResult();

            model.Validate(result.Errors);
            ModelValidator.Validate(jobDataMap, result.Errors);

            if (result.Success)
            {
                IJobDetail BuildJob(JobBuilder builder)
                {
                    return builder
                        .OfType(Type.GetType(jobModel.Type, true))
                        .WithIdentity(jobModel.JobName, jobModel.Group)
                        .WithDescription(jobModel.Description)
                        .SetJobData(jobDataMap.GetQuartzJobDataMap())
                        .RequestRecovery(jobModel.Recovery)
                        .Build();
                }

                if (jobModel.IsNew)
                {
                    await scheduler.AddJob(BuildJob(JobBuilder.Create().StoreDurably()), replace: false, cancellationToken: token);
                }
                else
                {
                    var oldJob = await GetJobDetail(JobKey.Create(jobModel.OldJobName, jobModel.OldGroup), token);
                    await scheduler.UpdateJob(oldJob.Key, BuildJob(oldJob.GetJobBuilder()));
                }

                if (trigger)
                {
                    await scheduler.TriggerJob(JobKey.Create(jobModel.JobName, jobModel.Group), token);
                }
            }

            return Json(result);
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Delete([FromBody] KeyModel model, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            if (!EnsureValidKey(model)) return BadRequest();

            var key = model.ToJobKey();

            if (!await scheduler.DeleteJob(key, token))
                throw new InvalidOperationException("Cannot delete job " + key);

            return NoContent();
        }

        [HttpGet, JsonErrorResponse]
        public async Task<IActionResult> AdditionalData(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var keys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup(), token);
            var history = await scheduler.Context.GetExecutionHistoryStore().FilterLastOfEveryJob(10);
            var historyByJob = history.ToLookup(x => x.Job);

            var list = new List<object>();
            foreach (var key in keys)
            {
                var triggers = await scheduler.GetTriggersOfJob(key, token);

                var nextFires = triggers.Select(x => x.GetNextFireTimeUtc()?.UtcDateTime).ToArray();

                list.Add(new JobAdditionalDataViewModel
                {
                    JobName = key.Name,
                    Group = key.Group,
                    History = historyByJob.TryGet(key.ToString()).ToHistogram(),
                    NextFireTime = nextFires.Where(x => x != null).OrderBy(x => x).FirstOrDefault()?.ToDefaultFormat(),
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
