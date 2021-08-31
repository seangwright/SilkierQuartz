﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [AllowAnonymous]
    public class AuthenticateController : Controller
    {
        private readonly SilkierQuartzAuthenticationOptions authenticationOptions;

        public AuthenticateController(SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            this.authenticationOptions = authenticationOptions ?? throw new ArgumentNullException(nameof(authenticationOptions));
        }

        [HttpGet]
        public async Task<IActionResult> Login([FromServices] IAuthenticationSchemeProvider schemes)
        {
            if (authenticationOptions.AccessRequirement == SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous)
            {
                return RedirectToAction(nameof(SchedulerController.Index), "Scheduler");
            }

            var silkierScheme = await schemes.GetSchemeAsync(authenticationOptions.AuthScheme);

            if (string.IsNullOrEmpty(authenticationOptions.UserName) ||
                string.IsNullOrEmpty(authenticationOptions.UserPassword))
            {
                foreach (var userClaim in HttpContext.User.Claims)
                {
                    Debug.WriteLine($"{userClaim.Type} - {userClaim.Value}");
                }

                if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.HasClaim(authenticationOptions.SilkierQuartzClaim,
                        authenticationOptions.SilkierQuartzClaimValue))
                {
                    await SignIn(false);

                    return RedirectToScheduler();
                }
                else
                {
                    return RedirectToScheduler();
                }
            }
            else
            {
                if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.HasClaim(authenticationOptions.SilkierQuartzClaim, authenticationOptions.SilkierQuartzClaimValue))
                {
                    ViewBag.IsLoginError = false;
                    return View(new AuthenticateViewModel());
                }
                else
                {
                    return RedirectToScheduler();
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromForm] AuthenticateViewModel request)
        {
            var form = HttpContext.Request.Form;

            if (string.Compare(request.UserName, authenticationOptions.UserName,
                StringComparison.InvariantCulture) != 0 ||
                string.Compare(request.Password, authenticationOptions.UserPassword,
                    StringComparison.InvariantCulture) != 0)
            {
                ModelState.AddModelError("credentials", "Username or Password is incorrect");

                return View(request);
            }

            await SignIn(request.IsPersist);

            return RedirectToScheduler();
        }

        [HttpGet]
        [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(authenticationOptions.AuthScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignIn(bool isPersistentSignIn)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, string.IsNullOrEmpty(authenticationOptions.UserName)
                    ? "SilkierQuartzAdmin"
                    : authenticationOptions.UserName ),

                new Claim(ClaimTypes.Name, string.IsNullOrEmpty(authenticationOptions.UserPassword)
                    ? "SilkierQuartzPassword"
                    : authenticationOptions.UserPassword),

                new Claim(authenticationOptions.SilkierQuartzClaim, authenticationOptions.SilkierQuartzClaimValue)
            };

            var authProperties = new AuthenticationProperties()
            {
                IsPersistent = isPersistentSignIn
            };

            var userIdentity = new ClaimsIdentity(claims, authenticationOptions.AuthScheme);
            await HttpContext.SignInAsync(authenticationOptions.AuthScheme, new ClaimsPrincipal(userIdentity),
                authProperties);
        }

        private RedirectToActionResult RedirectToScheduler() =>
            RedirectToAction(nameof(SchedulerController.Index), "Scheduler");
    }
}
