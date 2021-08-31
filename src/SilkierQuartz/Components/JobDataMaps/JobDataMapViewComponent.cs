using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;

namespace SilkierQuartz.Components.JobDataMaps
{
    public class JobDataMapViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(JobDataMapModel map)
        {
            return View("_JobDataMap", map);
        }
    }
}
