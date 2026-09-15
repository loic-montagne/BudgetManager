// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Text;
using BudgetManager.Infrastructure.Identity;
using BudgetManager.Application.Features.User.ConfirmEmailChange;
using BudgetManager.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;

namespace BudgetManager.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ISender _sender;

        public ConfirmEmailChangeModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IStringLocalizer<SharedResource> localizer,
            ISender sender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
            _sender = sender;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(
            string userId,
            string email,
            string code)
        {
            if (!Guid.TryParse(userId, out var id) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code))
            {
                StatusMessage = _localizer["Message.EmailChangeConfirmationError"].Value;
                return Page();
            }

            string token;

            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }
            catch (FormatException)
            {
                StatusMessage = _localizer["Message.EmailChangeConfirmationError"].Value;
                return Page();
            }

            try
            {
                await _sender.Send(
                    new ConfirmEmailChangeUserCommand(
                        id,
                        email,
                        token),
                    HttpContext.RequestAborted);
            }
            catch (BadRequestException)
            {
                StatusMessage = _localizer["Message.EmailChangeConfirmationError"].Value;
                return Page();
            }
            catch (UpdateException)
            {
                StatusMessage = _localizer["Message.EmailChangeConfirmationError"].Value;
                return Page();
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is not null)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            StatusMessage = _localizer["Message.EmailChangeConfirmed"].Value;
            return Page();
        }
    }
}
