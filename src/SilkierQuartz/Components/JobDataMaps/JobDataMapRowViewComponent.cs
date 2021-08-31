using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;
using SilkierQuartz.TypeHandlers;

namespace SilkierQuartz.Components.JobDataMaps
{
    public class JobDataMapRowViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(JobDataMapItem item, bool isTemplate)
        {
            return View("_JobDataMapRow", new JobDataMapRowViewModel { Item = item, IsTemplate = isTemplate });
        }
    }

    public class JobDataMapRowViewModel
    {
        public JobDataMapItem Item { get; set; }
        public bool IsTemplate { get; set; }
        public string Handler(TypeHandlerBase handler) =>
            TypeHandlerService.Serialize(handler);

    }
}
