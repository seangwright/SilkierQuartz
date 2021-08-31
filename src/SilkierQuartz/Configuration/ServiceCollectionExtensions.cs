﻿using Microsoft.AspNetCore.Authorization;
using SilkierQuartz;
using SilkierQuartz.Authorization;
using SilkierQuartz.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("We recommend AddSilkierQuartz")]
        public static IServiceCollection AddQuartzmin(this IServiceCollection services, Action<NameValueCollection> stdSchedulerFactoryOptions = null)
            => services.AddSilkierQuartz(stdSchedulerFactoryOptions: stdSchedulerFactoryOptions);


        public static IServiceCollection AddSilkierQuartz(
            this IServiceCollection services,
            Action<SilkierQuartzOptions> configureOptions = null,
            Action<SilkierQuartzAuthenticationOptions> configureAuthenticationOptions = null,
            Action<NameValueCollection> stdSchedulerFactoryOptions = null,
            Func<List<Assembly>> jobsasmlist = null)
        {
            var options = new SilkierQuartzOptions();
            configureOptions?.Invoke(options);

            var authenticationOptions = new SilkierQuartzAuthenticationOptions();
            configureAuthenticationOptions?.Invoke(authenticationOptions);

            services.AddSingleton(options);
            services.AddSingleton(authenticationOptions);
            services.AddControllersWithViews()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationExpanders.Add(new ComponentViewLocationsExpander());
                })
                .AddApplicationPart(typeof(SilkierQuartzOptions).Assembly);

            services
                .AddAuthentication(authenticationOptions.AuthScheme)
                .AddCookie(authenticationOptions.AuthScheme, cfg =>
                {
                    cfg.Cookie.Name = authenticationOptions.AuthScheme;
                    cfg.LoginPath = $"{options.VirtualPathRoot}/Authenticate/Login";
                    cfg.AccessDeniedPath = $"{options.VirtualPathRoot}/Authenticate/Login";
                    cfg.ExpireTimeSpan = TimeSpan.FromDays(7);
                    cfg.SlidingExpiration = true;
                });

            services.AddAuthorization(opts =>
            {
                opts.AddPolicy(SilkierQuartzAuthenticationOptions.AuthorizationPolicyName, builder =>
                {
                    builder.AddRequirements(new SilkierQuartzDefaultAuthorizationRequirement(authenticationOptions.AccessRequirement));
                });
            });

            services.AddScoped<IAuthorizationHandler, SilkierQuartzDefaultAuthorizationHandler>();

            services.UseQuartzHostedService(stdSchedulerFactoryOptions);
            services.AddSingleton<Cache>();

            var types = JobsListHelper.GetSilkierQuartzJobs(jobsasmlist?.Invoke());
            types.ForEach(t =>
            {
                var so = t.GetCustomAttribute<SilkierQuartzAttribute>();
                services.AddQuartzJob(t, so.Identity ?? t.Name, so.Desciption ?? t.FullName);
            });
            return services;
        }
    }
}
