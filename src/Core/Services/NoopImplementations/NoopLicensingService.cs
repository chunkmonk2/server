using Bit.Core.Models.Table;
using Microsoft.AspNetCore.Hosting;
using System;
using Bit.Core.Models.Business;
using Bit.Core.Settings;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Bit.Core.Services
{
    public class NoopLicensingService : ILicensingService
    {
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
    }
}
