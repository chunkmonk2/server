using Bit.Core.Repositories.EntityFramework;
using Bit.Core.Test.AutoFixture;
using Bit.Core.Test.AutoFixture.Attributes;
using Bit.Core.Test.AutoFixture.PolicyFixtures;
using Bit.Core.Test.AutoFixture.OrganizationUserFixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TableModel = Bit.Core.Models.Table;
using System.Linq;
using System.Collections.Generic;
using EfRepo = Bit.Core.Repositories.EntityFramework;
using SqlRepo = Bit.Core.Repositories.SqlServer;
using Bit.Core.Test.Repositories.EntityFramework.EqualityComparers;
using Bit.Core.Models.Data;
using System.Text.Json;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using System.Threading.Tasks;

namespace Bit.Core.Test.Repositories.EntityFramework
{
    public class PolicyRepositoryTests
    {
        [CiSkippedTheory, EfPolicyAutoData]
        public async void CreateAsync_Works_DataMatches(
            TableModel.Policy policy,
            TableModel.Organization organization,
            PolicyCompare equalityComparer,
            List<EfRepo.PolicyRepository> suts,
            List<EfRepo.OrganizationRepository> efOrganizationRepos,
            SqlRepo.PolicyRepository sqlPolicyRepo,
            SqlRepo.OrganizationRepository sqlOrganizationRepo
            )
        {
            var savedPolicys = new List<TableModel.Policy>();
            foreach (var sut in suts)
            {
                var i = suts.IndexOf(sut);

                var efOrganization = await efOrganizationRepos[i].CreateAsync(organization);
                sut.ClearChangeTracking();

                policy.OrganizationId = efOrganization.Id;
                var postEfPolicy = await sut.CreateAsync(policy);
                sut.ClearChangeTracking();

                var savedPolicy = await sut.GetByIdAsync(postEfPolicy.Id);
                savedPolicys.Add(savedPolicy);
            }

            var sqlOrganization = await sqlOrganizationRepo.CreateAsync(organization);

            policy.OrganizationId = sqlOrganization.Id;
            var sqlPolicy = await sqlPolicyRepo.CreateAsync(policy);
            var savedSqlPolicy = await sqlPolicyRepo.GetByIdAsync(sqlPolicy.Id);
            savedPolicys.Add(savedSqlPolicy);

            var distinctItems = savedPolicys.Distinct(equalityComparer);
            Assert.True(!distinctItems.Skip(1).Any());
        }

        [CiSkippedTheory]
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.User, false, OrganizationUserStatusType.Confirmed, true, true, false)]       // Ordinary user
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.Owner, false, OrganizationUserStatusType.Confirmed, true, true, false)]     // Owner
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.Admin, false, OrganizationUserStatusType.Confirmed, true, true, false)]     // Admin
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.User, true, OrganizationUserStatusType.Confirmed, true, true, false)]       // canManagePolicies
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.User, false, OrganizationUserStatusType.Confirmed, true, true, true)]       // Provider
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.User, false, OrganizationUserStatusType.Confirmed, false, true, false)]     // Policy disabled
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.User, false, OrganizationUserStatusType.Confirmed, true, false, false)]     // No policy of Type
        [EfPolicyApplicableToUserInlineAutoData(OrganizationUserType.User, false, OrganizationUserStatusType.Invited, true, true, false)]        // User not minStatus
        public async void GetManyByTypeApplicableToUser_Works_DataMatches_Corre(
            // Inline data
            OrganizationUserType userType,
            bool canManagePolicies,
            OrganizationUserStatusType orgUserStatus,
            bool policyEnabled,
            bool policySameType,
            bool isProvider,

            // Auto data - models
            TableModel.Policy policy,
            TableModel.User user,
            TableModel.Organization organization,
            TableModel.OrganizationUser orgUser,
            TableModel.Provider.Provider provider,
            TableModel.Provider.ProviderOrganization providerOrganization,
            TableModel.Provider.ProviderUser providerUser,
            PolicyCompareIncludingOrganization equalityComparer,

            // Auto data - EF repos
            List<EfRepo.PolicyRepository> suts,
            List<EfRepo.UserRepository> efUserRepository,
            List<EfRepo.OrganizationRepository> efOrganizationRepository,
            List<EfRepo.OrganizationUserRepository> efOrganizationUserRepository,
            List<EfRepo.ProviderRepository> efProviderRepository,
            List<EfRepo.ProviderOrganizationRepository> efProviderOrganizationRepository,
            List<EfRepo.ProviderUserRepository> efProviderUserRepository,

            // Auto data - SQL repos
            SqlRepo.PolicyRepository sqlPolicyRepo,
            SqlRepo.UserRepository sqlUserRepo,
            SqlRepo.OrganizationRepository sqlOrganizationRepo,
            SqlRepo.ProviderRepository sqlProviderRepo,
            SqlRepo.OrganizationUserRepository sqlOrganizationUserRepo,
            SqlRepo.ProviderOrganizationRepository sqlProviderOrganizationRepo,
            SqlRepo.ProviderUserRepository sqlProviderUserRepo
            )
        {
            // Combine EF and SQL repos into one list per type
            var policyRepos = suts.ToList<IPolicyRepository>();
            policyRepos.Add(sqlPolicyRepo);
            var userRepos = efUserRepository.ToList<IUserRepository>();
            userRepos.Add(sqlUserRepo);
            var orgRepos = efOrganizationRepository.ToList<IOrganizationRepository>();
            orgRepos.Add(sqlOrganizationRepo);
            var orgUserRepos = efOrganizationUserRepository.ToList<IOrganizationUserRepository>();
            orgUserRepos.Add(sqlOrganizationUserRepo);
            var providerRepos = efProviderRepository.ToList<IProviderRepository>();
            providerRepos.Add(sqlProviderRepo);
            var providerOrgRepos = efProviderOrganizationRepository.ToList<IProviderOrganizationRepository>();
            providerOrgRepos.Add(sqlProviderOrganizationRepo);
            var providerUserRepos = efProviderUserRepository.ToList<IProviderUserRepository>();
            providerUserRepos.Add(sqlProviderUserRepo);

            // Arrange data
            var savedPolicyType = PolicyType.SingleOrg;
            var queriedPolicyType = policySameType ? savedPolicyType : PolicyType.DisableSend;

            orgUser.Type = userType;
            orgUser.Status = orgUserStatus;
            var permissionsData = new Permissions { ManagePolicies = canManagePolicies };
            orgUser.Permissions = JsonSerializer.Serialize(permissionsData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            policy.Enabled = policyEnabled;
            policy.Type = savedPolicyType;

            var results = new List<TableModel.Policy>();

            foreach (var policyRepo in policyRepos)
            {
                var i = policyRepos.IndexOf(policyRepo);

                // Seed database
                var savedUser = await userRepos[i].CreateAsync(user);
                var savedOrg = await orgRepos[i].CreateAsync(organization);

                orgUser.UserId = savedUser.Id;
                orgUser.OrganizationId = savedOrg.Id;
                await orgUserRepos[i].CreateAsync(orgUser);

                if (isProvider)
                {
                    var savedProvider = await providerRepos[i].CreateAsync(provider);

                    providerOrganization.OrganizationId = savedOrg.Id;
                    providerOrganization.ProviderId = savedProvider.Id;
                    await providerOrgRepos[i].CreateAsync(providerOrganization);

                    providerUser.UserId = savedUser.Id;
                    providerUser.ProviderId = savedProvider.Id;
                    await providerUserRepos[i].CreateAsync(providerUser);
                }

                policy.OrganizationId = savedOrg.Id;
                await policyRepo.CreateAsync(policy);
                if (suts.Contains(policyRepo))
                {
                    (policyRepo as BaseEntityFrameworkRepository).ClearChangeTracking();
                }

                // Act
                var result = await policyRepo.GetManyByTypeApplicableToUserIdAsync(savedUser.Id, queriedPolicyType, OrganizationUserStatusType.Accepted);
                results.Add(result.FirstOrDefault());
            }

            // Assert
            var distinctItems = results.Distinct(equalityComparer);
            
            Assert.True(results.All(r => r == null) ||
                !distinctItems.Skip(1).Any());
        }
    }
}
