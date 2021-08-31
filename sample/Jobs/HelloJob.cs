using Quartz;
using System;
using System.Threading.Tasks;

namespace SilkierQuartz.Example.Jobs
{
    public class HelloJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Hello");
            return Task.CompletedTask;
        }
    }
}
