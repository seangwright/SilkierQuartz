﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Quartz;
using Quartz.Impl;
using SilkierQuartz;
using SilkierQuartz.Configuration;
using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        ///  Returns a client-usable handle to a Quartz.IScheduler.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IScheduler GetScheduler(this IApplicationBuilder app)
        {
            return app.ApplicationServices.GetRequiredService<ISchedulerFactory>().GetScheduler().Result;
        }

        /// <summary>
        /// Use SilkierQuartz and automatically discover IJob subclasses with SilkierQuartzAttribute
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configure"></param>
        public static IApplicationBuilder UseSilkierQuartz(
            this IApplicationBuilder app,
            Action<Services> configure = null)
        {
            var options = app.ApplicationServices
                .GetService<SilkierQuartzOptions>() ?? throw new ArgumentNullException(nameof(SilkierQuartzOptions));
            var authenticationOptions = app.ApplicationServices
                .GetService<SilkierQuartzAuthenticationOptions>() ?? throw new ArgumentNullException(nameof(SilkierQuartzAuthenticationOptions));

            app.UseFileServer(options);
            if (options.Scheduler == null)
            {
                try
                {
                    options.Scheduler = app.ApplicationServices.GetRequiredService<ISchedulerFactory>()?.GetScheduler().Result;
                }
                catch (Exception)
                {
                    options.Scheduler = null;
                }
                if (options.Scheduler == null)
                {
                    options.Scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
                }
            }
            var services = Services.Create(options, authenticationOptions);
            configure?.Invoke(services);

            app.Use(async (context, next) =>
            {
                context.Items[typeof(Services)] = services;
                await next.Invoke();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(nameof(SilkierQuartz), $"{options.VirtualPathRoot}/{{controller=Scheduler}}/{{action=Index}}");
                endpoints.MapControllerRoute($"{nameof(SilkierQuartz)}Authenticate",
                    $"{options.VirtualPathRoot}/{{controller=Authenticate}}/{{action=Login}}");
            });

            var types = JobsListHelper.GetSilkierQuartzJobs();
            types.ForEach(t =>
            {
                var so = t.GetCustomAttribute<SilkierQuartzAttribute>();
                app.UseQuartzJob(t, () =>
                {
                    var tb = TriggerBuilder.Create();
                    tb.WithSimpleSchedule(x =>
                    {
                        x.WithInterval(so.WithInterval);
                        if (so.RepeatCount > 0)
                        {
                            x.WithRepeatCount(so.RepeatCount);

                        }
                        else
                        {
                            x.RepeatForever();
                        }
                    });
                    if (so.StartAt == DateTimeOffset.MinValue)
                    {
                        tb.StartNow();
                    }
                    else
                    {
                        tb.StartAt(so.StartAt);
                    }
                    var tk = new TriggerKey(!string.IsNullOrEmpty(so.TriggerName) ? so.TriggerName : $"{t.Name}'s Trigger");
                    if (!string.IsNullOrEmpty(so.TriggerGroup))
                    {
                        so.TriggerGroup = so.TriggerGroup;
                    }
                    tb.WithIdentity(tk);
                    tb.WithDescription(so.TriggerDescription ?? $"{t.Name}'s Trigger,full name is {t.FullName}");
                    if (so.Priority > 0) tb.WithPriority(so.Priority);
                    return tb;
                });

            });


            return app;
        }

        private static void UseFileServer(this IApplicationBuilder app, SilkierQuartzOptions options)
        {
            IFileProvider fs;
            if (string.IsNullOrEmpty(options.ContentRootDirectory))
                fs = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Content");
            else
                fs = new PhysicalFileProvider(options.ContentRootDirectory);

            var fsOptions = new FileServerOptions()
            {
                RequestPath = new PathString($"{options.VirtualPathRoot}/Content"),
                EnableDefaultFiles = false,
                EnableDirectoryBrowsing = false,
                FileProvider = fs
            };

            app.UseFileServer(fsOptions);
        }
    }
}

