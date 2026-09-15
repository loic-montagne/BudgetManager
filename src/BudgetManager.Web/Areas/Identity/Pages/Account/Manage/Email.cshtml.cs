// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BudgetManager.Application.Abstractions.Api;
using BudgetManager.Application.Abstractions.Email;
using BudgetManager.Application.Email;
using BudgetManager.Application.Email.Templates;
using BudgetManager.Application.Exceptions;
using BudgetManager.Application.Features.User.ChangeEmail;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace BudgetManager.Web.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITemplatedEmailSender _templatedEmailSender;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IApplicationUrlBuilder _applicationUrlBuilder;
        private readonly IBusinessErrorLocalizer _businessErrorLocalizer;
        private readonly ISender _sender;

        public EmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITemplatedEmailSender templatedEmailSender,
            IStringLocalizer<SharedResource> localizer,
            IApplicationUrlBuilder applicationUrlBuilder,
            IBusinessErrorLocalizer businessErrorLocalizer,
            ISender sender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _templatedEmailSender = templatedEmailSender;
            _localizer = localizer;
            _applicationUrlBuilder = applicationUrlBuilder;
            _businessErrorLocalizer = businessErrorLocalizer;
            _sender = sender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [Display(Name = "Field.Email")]
        public string Email { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
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
            [EmailAddress(ErrorMessage = "Validation.Email")]
            [Display(Name = "Field.NewEmail")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user, bool force)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            if (Input is null || force)
            {
                Input = new InputModel
                {
                    NewEmail = email,
                };
            }

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(_localizer["Error.UserNotFound", _userManager.GetUserId(User)].Value);
            }

            await LoadAsync(user, true);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(_localizer["Error.UserNotFound", _userManager.GetUserId(User)].Value);
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user, false);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);

            try
            { 
                var code = await _sender.Send(
                    new ChangeEmailUserCommand(
                        user.Id,
                        email,
                        Input.NewEmail),
                    HttpContext.RequestAborted);

                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = _applicationUrlBuilder.GetPageUrl(
                    "/Account/ConfirmEmailChange",
                    new { area = "Identity", userId = user.Id, email = Input.NewEmail, code });

                var message = new TemplatedEmailMessage
                {
                    Subject = _localizer["Email.ChangeEmail.Subject"].Value,
                    TemplateName = EmailTemplates.EmailChangeConfirmation
                };
                message.To.Add(new EmailAddress(Input.NewEmail));

                await _templatedEmailSender.SendAsync(
                    message,
                    new EmailChangeConfirmationEmailModel(
                        user.FirstName,
                        callbackUrl),
                    CultureInfo.CurrentUICulture,
                    HttpContext.RequestAborted);

                StatusMessage = _localizer["Message.EmailChangeVerificationSent"].Value;
                return RedirectToPage();
            }
            catch (BadRequestException exception)
            {
                foreach (var error in exception.ValidationErrors)
                {
                    ModelState.AddModelError(
                        error.PropertyName == nameof(ChangeEmailUserCommand.NormalizedNewEmail) ? nameof(Input.NewEmail) : string.Empty,
                        _businessErrorLocalizer.Localize(error));
                }

                await LoadAsync(user, false);

                return Page();
            }
        }
    }
}
