using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Reflection;

namespace SilkierQuartz.Components.Footer
{
    public class FooterViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var version = GetType().Assembly
                .GetCustomAttributes<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()
                ?.InformationalVersion ?? "";

            return View("_Footer", new FooterViewModel { Version = version });
        }
    }

    public class FooterViewModel
    {
        public string Version { get; set; }
    }
}
