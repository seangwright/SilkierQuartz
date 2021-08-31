﻿using HandlebarsDotNet;
using Quartz;
using SilkierQuartz.Helpers;

namespace SilkierQuartz
{
    public class Services
    {
        internal const string ContextKey = "SilkierQuartz.services";

        public SilkierQuartzOptions Options { get; set; }

        public ViewEngine ViewEngine { get; set; }

        public IHandlebars Handlebars { get; set; }

        public TypeHandlerService TypeHandlers { get; set; }

        public IScheduler Scheduler { get; set; }

        public static Services Create(SilkierQuartzOptions options, SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            var handlebarsConfiguration = new HandlebarsConfiguration()
            {
                FileSystem = ViewFileSystemFactory.Create(options),
                ThrowOnUnresolvedBindingExpression = true,
            };

            var services = new Services()
            {
                Options = options,
                Scheduler = options.Scheduler,
                Handlebars = HandlebarsDotNet.Handlebars.Create(handlebarsConfiguration),
            };

            HandlebarsHelpers.Register(services, authenticationOptions);

            services.ViewEngine = new ViewEngine(services);
            services.TypeHandlers = new TypeHandlerService(services);

            return services;
        }
    }
}
