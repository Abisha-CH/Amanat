using Amanat.Models.Entities;
using Microsoft.Data.SqlClient;

namespace Amanat.Repositories.Interfaces;

public interface IContributionRepository
{
    Task<IReadOnlyList<ContributionEntity>> GetByCampaignAsync(int campaignId, string? status = null);
    Task<IReadOnlyList<ContributionEntity>> GetByContributorAsync(string userId);
    Task<ContributionEntity?> GetByIdAsync(long contributionId);
    Task<IReadOnlyList<ContributionEntity>> GetPendingAsync(int? campaignId = null);
    Task<long> SubmitAsync(ContributionEntity contribution, SqlTransaction transaction);
    Task<bool> VerifyAsync(long contributionId, string actorUserId, string? note, byte[] expectedRowVersion, SqlTransaction transaction);
    Task<bool> RejectAsync(long contributionId, string actorUserId, string? note, byte[] expectedRowVersion, SqlTransaction transaction);
    Task<IReadOnlyList<ContributionVerificationEntity>> GetVerificationHistoryAsync(long contributionId);
}
