using System.Security.Claims;
using System.Text.Json;
using Bit.Core.AdminConsole.Entities;
using Bit.Core.Entities;
using Bit.Core.Models.Business;
using Bit.Core.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Bit.Core.Services;

public class NoopLicensingService : ILicensingService
{
    private readonly IGlobalSettings _globalSettings;
    public NoopLicensingService(
        IWebHostEnvironment environment,
        GlobalSettings globalSettings)
    {
    }

    public Task ValidateOrganizationsAsync()
    {
        return Task.FromResult(0);
    }

    public Task ValidateUsersAsync()
    {
        return Task.FromResult(0);
    }

    public Task<bool> ValidateUserPremiumAsync(User user)
    {
        return Task.FromResult(true);
    }

    public bool VerifyLicense(ILicense license)
    {
        return true;
    }

    public byte[] SignLicense(ILicense license)
    {
        return new byte[0];
    }


    public Task<OrganizationLicense> ReadOrganizationLicenseAsync(Organization organization) =>
        ReadOrganizationLicenseAsync(organization.Id);
    public async Task<OrganizationLicense> ReadOrganizationLicenseAsync(Guid organizationId)
    {
        var filePath = Path.Combine(_globalSettings.LicenseDirectory, "organization", $"{organizationId}.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var fs = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<OrganizationLicense>(fs);
    }

    public ClaimsPrincipal GetClaimsPrincipalFromLicense(ILicense license)
    {
        return null;
    }

    public Task<string> CreateOrganizationTokenAsync(Organization organization, Guid installationId, SubscriptionInfo subscriptionInfo)
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> CreateUserTokenAsync(User user, SubscriptionInfo subscriptionInfo)
    {
        return Task.FromResult<string>(null);
    }
}
