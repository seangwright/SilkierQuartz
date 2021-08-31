using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;

namespace SilkierQuartz.Components.JobDataMap
{
    public class JobDataMapViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(JobDataMapModel map)
        {
            return View("_JobDataMap", map);
        }
    }
}
