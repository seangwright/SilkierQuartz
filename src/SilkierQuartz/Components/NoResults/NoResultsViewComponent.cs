using Microsoft.AspNetCore.Mvc;

namespace SilkierQuartz.Components
{
    public class NoResultsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string name)
        {
            return View("_NoResults", name);
        }
    }
}
