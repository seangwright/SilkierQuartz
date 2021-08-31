using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;
using System.Collections.Generic;

namespace SilkierQuartz.Components.GroupActions
{
    public class GroupActionsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<SchedulerGroupStateViewModel> items, string id, string header)
        {
            return View("_GroupActions", new GroupActionsViewModel { Items = items, Id = id, Header = header });
        }
    }

    public class GroupActionsViewModel
    {
        public IEnumerable<SchedulerGroupStateViewModel> Items { get; set; }
        public string Id { get; set; }
        public string Header { get; set; }
    }
}
