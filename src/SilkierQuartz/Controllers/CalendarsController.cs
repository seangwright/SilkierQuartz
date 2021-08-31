using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using SilkierQuartz.Helpers;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
    public class CalendarsController : Controller
    {
        private readonly ISchedulerFactory factory;

        public CalendarsController(ISchedulerFactory factory)
        {
            this.factory = factory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var calendarNames = await scheduler.GetCalendarNames(token);

            var list = new List<CalendarListItem>();

            foreach (string name in calendarNames)
            {
                var cal = await scheduler.GetCalendar(name);

                list.Add(new CalendarListItem() { Name = name, Description = cal.Description, Type = cal.GetType() });
            }

            return View(list);
        }

        [HttpGet]
        public IActionResult New()
        {
            var calendars = new[] { new CalendarViewModel()
            {
                IsRoot = true,
                Type = "cron",
                TimeZone = TimeZoneInfo.Local.Id,
            }};

            return View("Edit", new CalendarsViewModel
            {
                IsNew = true,
                Calendars = calendars
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string name, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            var calendar = await scheduler.GetCalendar(name);

            var model = calendar.Flatten().Select(x => CalendarViewModel.FromCalendar(x)).ToArray();

            if (model.Any())
            {
                model[0].IsRoot = true;
                model[0].Name = name;
            }

            return View(new CalendarsViewModel
            {
                IsNew = false,
                Calendars = model
            });
        }

        private void RemoveLastEmpty(List<string> list)
        {
            if (list?.Count > 0 && string.IsNullOrEmpty(list.Last()))
                list.RemoveAt(list.Count - 1);
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Save([FromBody] CalendarViewModel[] chain, bool isNew, CancellationToken token)
        {
            var result = new ValidationResult();

            if (chain.Length == 0 || string.IsNullOrEmpty(chain[0].Name))
                result.Errors.Add(ValidationError.EmptyField(nameof(CalendarViewModel.Name)));

            for (int i = 0; i < chain.Length; i++)
            {
                RemoveLastEmpty(chain[i].Days);
                RemoveLastEmpty(chain[i].Dates);

                var errors = new List<ValidationError>();
                chain[i].Validate(errors);
                errors.ForEach(x => x.SegmentIndex = i);
                result.Errors.AddRange(errors);
            }

            if (result.Success)
            {
                string name = chain[0].Name;

                ICalendar existing = null;

                var scheduler = await factory.GetScheduler(token);

                if (isNew == false)
                    existing = await scheduler.GetCalendar(name);

                ICalendar root = null, current = null;
                for (int i = 0; i < chain.Length; i++)
                {
                    ICalendar newCal = chain[i].Type.Equals("custom") ? existing : chain[i].BuildCalendar();

                    if (newCal == null)
                        break;

                    if (i == 0)
                        root = newCal;
                    else
                        current.CalendarBase = newCal;

                    current = newCal;
                    existing = existing?.CalendarBase;
                }

                if (root == null)
                {
                    result.Errors.Add(new ValidationError() { Field = nameof(CalendarViewModel.Type), Reason = "Cannot create calendar.", SegmentIndex = 0 });
                }
                else
                {
                    await scheduler.AddCalendar(name, root, replace: true, updateTriggers: true);
                }
            }

            return Json(result);
        }

        public class DeleteArgs
        {
            public string Name { get; set; }
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Delete([FromBody] DeleteArgs args, CancellationToken token)
        {
            var scheduler = await factory.GetScheduler(token);

            if (!await scheduler.DeleteCalendar(args.Name))
                throw new InvalidOperationException("Cannot delete calendar " + args.Name);

            return NoContent();
        }
    }
}
