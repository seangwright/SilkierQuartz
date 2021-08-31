using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using SilkierQuartz.Helpers;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class ExecutionsController : Controller
    {
        private readonly ISchedulerFactory factory;

        public ExecutionsController(ISchedulerFactory factory)
        {
            this.factory = factory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var currentlyExecutingJobs = await scheduler.GetCurrentlyExecutingJobs();

            var list = new List<ExecutionViewModel>();

            foreach (var exec in currentlyExecutingJobs)
            {
                list.Add(new ExecutionViewModel
                {
                    Id = exec.FireInstanceId,
                    JobGroup = exec.JobDetail.Key.Group,
                    JobName = exec.JobDetail.Key.Name,
                    TriggerGroup = exec.Trigger.Key.Group,
                    TriggerName = exec.Trigger.Key.Name,
                    ScheduledFireTime = exec.ScheduledFireTimeUtc?.UtcDateTime.ToDefaultFormat(),
                    ActualFireTime = exec.FireTimeUtc.UtcDateTime.ToDefaultFormat(),
                    RunTime = exec.JobRunTime.ToString("hh\\:mm\\:ss")
                });
            }

            return View(list);
        }

        public class InterruptArgs
        {
            public string Id { get; set; }
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Interrupt([FromBody] InterruptArgs args, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            if (!await scheduler.Interrupt(args.Id))
                throw new InvalidOperationException("Cannot interrupt execution " + args.Id);

            return NoContent();
        }
    }
}
