using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;

namespace SilkierQuartz.Components.Histogram
{
    public class HistogramViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(HistogramData histogramData)
        {
            histogramData.Layout();

            return View("_Histogram", histogramData);
        }
    }
}
