// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BudgetManager.Application.Email;
using BudgetManager.Application.Features.User.Activate;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Web.Controllers;
using BudgetManager.Web.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BudgetManager.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ActivateAccountModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISender _sender;
        private readonly IStringLocalizer<SharedResource> _localizer;
        protected IBusinessErrorLocalizer _businessErrorLocalizer;

        public ActivateAccountModel(
            UserManager<ApplicationUser> userManager,
            ISender sender,
            IStringLocalizer<SharedResource> localizer,
            IBusinessErrorLocalizer businessErrorLocalizer)
        {
            _userManager = userManager;
            _sender = sender;
            _localizer = localizer;
            _businessErrorLocalizer = businessErrorLocalizer;
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

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Validation.Required")]
            [Display(Name = "Field.Password")]
            [StringLength(100, ErrorMessage = "Validation.StringLength", MinimumLength = 12)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Field.ConfirmPassword")]
            [Compare("Password", ErrorMessage = "Validation.PasswordMismatch")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Validation.Required")]
            public string ActivationCode { get; set; }
        }

        public IActionResult OnGet(string activationCode = null)
        {
            if (string.IsNullOrWhiteSpace(activationCode))
                return BadRequest(_localizer["Error.ActivationCodeRequired"].Value);
                        
            try
            {
                activationCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(activationCode));
            }
            catch (FormatException)
            {
                return BadRequest(_localizer["Error.ActivationCodeRequired"].Value);
            }

            Input = new InputModel
            {
                ActivationCode = activationCode
            };
            return Page();
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
                // Don't reveal that the user does not exist
                return RedirectToPage("./ActivateAccountConfirmation");
            }

            var result = await SenderController.Send(
                new ActivateUserCommand(
                    user.Id,
                    Input.ActivationCode,
                    Input.Password,
                    Input.ConfirmPassword),
                null,
                _sender,
                _localizer,
                _businessErrorLocalizer,
                HttpContext.RequestAborted);

            if (result.Success)
                return RedirectToPage("./ActivateAccountConfirmation");

            if (!string.IsNullOrWhiteSpace(result.Error))
                ModelState.AddModelError(string.Empty, result.Error);

            foreach (var error in result.InvalidControls)
                ModelState.AddModelError(error.Name, error.Text);

            return Page();
        }
    }
}
