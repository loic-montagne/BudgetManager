// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BudgetManager.Application.Email;
using BudgetManager.Application.Features.User.SendActivationEmail;
using BudgetManager.Infrastructure.Configuration;
using BudgetManager.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace BudgetManager.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly ISender _sender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IOptions<IdentityTokenOptions> _options;

        public ResendEmailConfirmationModel(
            ISender sender,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<SharedResource> localizer,
            IOptions<IdentityTokenOptions> options)
        {
            _sender = sender;
            _userManager = userManager;
            _localizer = localizer;
            _options = options;
        }

        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Validation.Required")]
            [Display(Name = "Field.Email")]
            [EmailAddress(ErrorMessage = "Validation.Email")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        [EnableRateLimiting("email-security")]
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
            {
                StatusMessage = _localizer["Message.AccountActivationEmailSent"].Value;
                return Page();
            }

            await _sender.Send(new SendUserActivationEmailCommand(user.Id, "/Account/ActivateAccount", new { area = "Identity" }, _options.Value.AccountActivationLifetime, _localizer["Email.ActivateAccount.Subject"].Value), HttpContext.RequestAborted);

            StatusMessage = _localizer["Message.AccountActivationEmailSent"].Value;
            return Page();
        }
    }
}
