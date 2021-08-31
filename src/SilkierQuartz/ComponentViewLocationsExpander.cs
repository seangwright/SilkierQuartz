using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Generic;

namespace SilkierQuartz
{
    public class ComponentViewLocationsExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            foreach (var location in viewLocations)
            {
                yield return location;
            }

            yield return "/{0}.cshtml";
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            return;
        }
    }
}
