// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using BudgetManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace BudgetManager.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITemplatedEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IApplicationUrlBuilder _applicationUrlBuilder;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            ITemplatedEmailSender emailSender,
            IStringLocalizer<SharedResource> localizer,
            IApplicationUrlBuilder applicationUrlBuilder)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _localizer = localizer;
            _applicationUrlBuilder = applicationUrlBuilder;
        }

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

        [EnableRateLimiting("email-security")]
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = _applicationUrlBuilder.GetPageUrl(
                    "/Account/ResetPassword",
                    new { area = "Identity", code });
                
                var message =
                    new TemplatedEmailMessage
                    {
                        Subject = _localizer["Email.PasswordReset.Subject"].Value,
                        TemplateName = EmailTemplates.PasswordReset
                    };

                message.To.Add(
                    new EmailAddress(Input.Email));

                await _emailSender.SendAsync(
                    message,
                    new PasswordResetEmailModel(
                        user.FirstName,
                        callbackUrl),
                    CultureInfo.CurrentUICulture,
                    HttpContext.RequestAborted);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
