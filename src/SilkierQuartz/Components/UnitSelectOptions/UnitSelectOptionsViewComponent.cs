using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SilkierQuartz.Components.UnitSelect
{
    public class UnitSelectOptionsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(bool allowMillisecond, bool allowIrregular, string selectedValue)
        {
            var items = new List<SelectListItem>();

            if (allowMillisecond)
            {
                items.Add(new SelectListItem("Millisecond", "Millisecond"));
            }

            items.Add(new SelectListItem("Second", "Second"));
            items.Add(new SelectListItem("Minute", "Minute"));
            items.Add(new SelectListItem("Hour", "Hour"));

            if (allowIrregular)
            {
                items.Add(new SelectListItem("Day", "Day"));
                items.Add(new SelectListItem("Week", "Week"));
                items.Add(new SelectListItem("Month", "Month"));
                items.Add(new SelectListItem("Year", "Year"));
            }

            return View("_UnitSelectOptions", new UnitSelectViewModel(items.Select(i =>
            {
                i.Selected = string.Equals(i.Value, selectedValue, StringComparison.OrdinalIgnoreCase);

                return i;
            })));
        }
    }

    public class UnitSelectViewModel
    {
        public UnitSelectViewModel(IEnumerable<SelectListItem> items)
        {
            Items = items;
        }

        public IEnumerable<SelectListItem> Items { get; }
    }
}
