using HandlebarsDotNet;
using SilkierQuartz.Models;
using SilkierQuartz.TypeHandlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace SilkierQuartz.Helpers
{
    internal class HandlebarsHelpers
    {
        Services _services;
        private readonly SilkierQuartzAuthenticationOptions authenticationOptions;

        public HandlebarsHelpers(Services services, SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            _services = services;
            this.authenticationOptions = authenticationOptions;
        }

        public static void Register(Services services, SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            new HandlebarsHelpers(services, authenticationOptions).RegisterInternal();
        }

        void RegisterInternal()
        {
            IHandlebars h = _services.Handlebars;

            h.RegisterHelper("Upper", (o, c, a) => o.Write(a[0].ToString().ToUpper()));
            h.RegisterHelper("Lower", (o, c, a) => o.Write(a[0].ToString().ToLower()));
            h.RegisterHelper("LocalTimeZoneInfoId", (o, c, a) => o.Write(TimeZoneInfo.Local.Id));
            h.RegisterHelper("SystemTimeZonesJson", (o, c, a) => Json(o, c, TimeZoneInfo.GetSystemTimeZones().ToDictionary()));
            h.RegisterHelper("DefaultDateFormat", (o, c, a) => o.Write(DateTimeSettings.DefaultDateFormat));
            h.RegisterHelper("DefaultTimeFormat", (o, c, a) => o.Write(DateTimeSettings.DefaultTimeFormat));
            h.RegisterHelper("DoLayout", (o, c, a) => c.Layout());
            h.RegisterHelper("SerializeTypeHandler", (o, c, a) => o.WriteSafeString(TypeHandlerService.Serialize((TypeHandlerBase)c)));
            h.RegisterHelper("Disabled", (o, c, a) => { if (IsTrue(a[0])) o.Write("disabled"); });
            h.RegisterHelper("Checked", (o, c, a) => { if (IsTrue(a[0])) o.Write("checked"); });
            h.RegisterHelper("nvl", (o, c, a) => o.Write(a[a[0] == null ? 1 : 0]));
            h.RegisterHelper("not", (o, c, a) => o.Write(IsTrue(a[0]) ? "False" : "True"));


            h.RegisterHelper(nameof(RenderJobDataMapValue), RenderJobDataMapValue);


            h.RegisterHelper(nameof(Json), Json);
            h.RegisterHelper(nameof(Selected), Selected);
            h.RegisterHelper(nameof(isType), isType);
            h.RegisterHelper(nameof(eachPair), eachPair);
            h.RegisterHelper(nameof(eachItems), eachItems);
            h.RegisterHelper(nameof(ToBase64), ToBase64);
        }

        static bool IsTrue(object value) => value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;


        string UrlEncode(string value) => HttpUtility.UrlEncode(value);


        void Selected(TextWriter output, dynamic context, params object[] arguments)
        {
            string selected;
            if (arguments.Length >= 2)
                selected = arguments[1]?.ToString();
            else
                selected = context["selected"].ToString();

            if (((string)arguments[0]).Equals(selected, StringComparison.InvariantCultureIgnoreCase))
                output.Write("selected");
        }

        void Json(TextWriter output, dynamic context, params object[] arguments)
        {
            output.WriteSafeString(Newtonsoft.Json.JsonConvert.SerializeObject(arguments[0]));
        }

        void RenderJobDataMapValue(TextWriter output, dynamic context, params object[] arguments)
        {
            var item = (JobDataMapItem)arguments[1];
            output.WriteSafeString(item.SelectedType.RenderView((Services)arguments[0], item.Value));
        }

        void isType(TextWriter writer, HelperOptions options, dynamic context, params object[] arguments)
        {
            Type[] expectedType;

            var strType = (string)arguments[1];

            switch (strType)
            {
                case "IEnumerable<string>":
                    expectedType = new[] { typeof(IEnumerable<string>) };
                    break;
                case "IEnumerable<KeyValuePair<string, string>>":
                    expectedType = new[] { typeof(IEnumerable<KeyValuePair<string, string>>) };
                    break;
                default:
                    throw new ArgumentException("Invalid type: " + strType);
            }

            var t = arguments[0]?.GetType();

            if (expectedType.Any(x => x.IsAssignableFrom(t)))
                options.Template(writer, (object)context);
            else
                options.Inverse(writer, (object)context);
        }

        void eachPair(TextWriter writer, HelperOptions options, dynamic context, params object[] arguments)
        {
            void OutputElements<T>()
            {
                if (arguments[0] is IEnumerable<T> pairs)
                {
                    foreach (var item in pairs)
                        options.Template(writer, item);
                }
            }

            OutputElements<KeyValuePair<string, string>>();
            OutputElements<KeyValuePair<string, object>>();
        }

        void eachItems(TextWriter writer, HelperOptions options, dynamic context, params object[] arguments)
        {
            eachPair(writer, options, context, ((dynamic)arguments[0]).GetItems());
        }

        void ToBase64(TextWriter output, dynamic context, params object[] arguments)
        {
            var bytes = (byte[])arguments[0];

            if (bytes != null)
                output.Write(Convert.ToBase64String(bytes));
        }
    }
}
