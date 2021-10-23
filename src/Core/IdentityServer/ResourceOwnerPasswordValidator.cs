﻿using Bit.Core.Models.Table;
using Bit.Core.Repositories;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Bit.Core.Services;
using Bit.Core.Identity;
using Bit.Core.Context;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Bit.Core.IdentityServer
{
    public class ResourceOwnerPasswordValidator : BaseRequestValidator<ResourceOwnerPasswordValidationContext>,
        IResourceOwnerPasswordValidator
    {
        private UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly ICurrentContext _currentContext;
        private readonly ICaptchaValidationService _captchaValidationService;
        public ResourceOwnerPasswordValidator(
            UserManager<User> userManager,
            IDeviceRepository deviceRepository,
            IDeviceService deviceService,
            IUserService userService,
            IEventService eventService,
            IOrganizationDuoWebTokenProvider organizationDuoWebTokenProvider,
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository,
            IApplicationCacheService applicationCacheService,
            IMailService mailService,
            ILogger<ResourceOwnerPasswordValidator> logger,
            ICurrentContext currentContext,
            GlobalSettings globalSettings,
            IPolicyRepository policyRepository,
            ICaptchaValidationService captchaValidationService)
            : base(userManager, deviceRepository, deviceService, userService, eventService,
                  organizationDuoWebTokenProvider, organizationRepository, organizationUserRepository,
                  applicationCacheService, mailService, logger, currentContext, globalSettings, policyRepository)
        {
            _userManager = userManager;
            _userService = userService;
            _currentContext = currentContext;
            _captchaValidationService = captchaValidationService;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            if (!AuthEmailHeaderIsValid(context))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant,
                    "Auth-Email header invalid.");
                return;
            }

            string bypassToken = null;
            var user = await _userManager.FindByEmailAsync(context.UserName.ToLowerInvariant());
            var unknownDevice = !await KnownDeviceAsync(user, context.Request);
            if (unknownDevice && _captchaValidationService.RequireCaptchaValidation(_currentContext))
            {
                var captchaResponse = context.Request.Raw["captchaResponse"]?.ToString();

                if (string.IsNullOrWhiteSpace(captchaResponse))
                {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Captcha required.",
                        new Dictionary<string, object> {
                            { _captchaValidationService.SiteKeyResponseKeyName, _captchaValidationService.SiteKey },
                        });
                    return;
                }

                var captchaValid = _captchaValidationService.ValidateCaptchaBypassToken(captchaResponse, user) ||
                    await _captchaValidationService.ValidateCaptchaResponseAsync(captchaResponse, _currentContext.IpAddress);
                if (!captchaValid)
                {
                    await BuildErrorResultAsync("Captcha is invalid. Please refresh and try again", false, context, null);
                    return;
                }
                bypassToken = _captchaValidationService.GenerateCaptchaBypassToken(user);
            }

            await ValidateAsync(context, context.Request);
            if (context.Result.CustomResponse != null && bypassToken != null)
            {
                context.Result.CustomResponse["CaptchaBypassToken"] = bypassToken;
            }
        }

        protected async override Task<(User, bool)> ValidateContextAsync(ResourceOwnerPasswordValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(context.UserName))
            {
                return (null, false);
            }

            var user = await _userManager.FindByEmailAsync(context.UserName.ToLowerInvariant());
            if (user == null || !await _userService.CheckPasswordAsync(user, context.Password))
            {
                return (user, false);
            }

            return (user, true);
        }

        protected override void SetSuccessResult(ResourceOwnerPasswordValidationContext context, User user,
            List<Claim> claims, Dictionary<string, object> customResponse)
        {
            context.Result = new GrantValidationResult(user.Id.ToString(), "Application",
                identityProvider: "bitwarden",
                claims: claims.Count > 0 ? claims : null,
                customResponse: customResponse);
        }

        protected override void SetTwoFactorResult(ResourceOwnerPasswordValidationContext context,
            Dictionary<string, object> customResponse)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Two factor required.",
                customResponse);
        }

        protected override void SetSsoResult(ResourceOwnerPasswordValidationContext context,
            Dictionary<string, object> customResponse)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Sso authentication required.",
                customResponse);
        }

        protected override void SetErrorResult(ResourceOwnerPasswordValidationContext context,
            Dictionary<string, object> customResponse)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, customResponse: customResponse);
        }

        private bool AuthEmailHeaderIsValid(ResourceOwnerPasswordValidationContext context)
        {
            if (!_currentContext.HttpContext.Request.Headers.ContainsKey("Auth-Email"))
            {
                return false;
            }
            else
            {
                try
                {
                    var authEmailHeader = _currentContext.HttpContext.Request.Headers["Auth-Email"];
                    var authEmailDecoded = CoreHelpers.Base64UrlDecodeString(authEmailHeader);

                    if (authEmailDecoded != context.UserName)
                    {
                        return false;
                    }
                }
                catch (System.Exception e) when (e is System.InvalidOperationException || e is System.FormatException)
                {
                    // Invalid B64 encoding
                    return false;
                }
            }

            return true;
        }
    }
}
