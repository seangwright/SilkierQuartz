﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilkierQuartz.HostedService
{
    internal class QuartzHostedService : IHostedService
    {
        private IServiceProvider Services { get; }
        private IScheduler _scheduler;
        private ISchedulerFactory _schedulerFactory;
        private IJobFactory _jobFactory { get; }

        public QuartzHostedService(
            IServiceProvider services,
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory)
        {
            Services = services;
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var _scheduleJobs = Services.GetService<IEnumerable<IScheduleJob>>();

            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            _scheduler.JobFactory = _jobFactory;

            await _scheduler.Start(cancellationToken);

            if (_scheduleJobs == null || !_scheduleJobs.Any())
                return;

            foreach (var scheduleJob in _scheduleJobs)
            {
                bool isNewJob = true;
                foreach (var trigger in scheduleJob.Triggers)
                {
                    if (isNewJob)
                        await _scheduler.ScheduleJob(scheduleJob.JobDetail, trigger, cancellationToken);
                    else
                        await _scheduler.ScheduleJob(trigger, cancellationToken);
                    isNewJob = false;
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_scheduler.IsStarted)
                await _scheduler.Shutdown(cancellationToken);
        }
    }
}
