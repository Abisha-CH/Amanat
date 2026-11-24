using Amanat.Models.Entities;
using Microsoft.Data.SqlClient;

namespace Amanat.Repositories.Interfaces;

public interface IExpenseRepository
{
    Task<IReadOnlyList<ExpenseEntity>> GetByCampaignAsync(int campaignId, string? status = null);
    Task<ExpenseEntity?> GetByIdAsync(long expenseId);
    Task<long> SubmitAsync(ExpenseEntity expense, SqlTransaction transaction);
    Task<IReadOnlyList<ExpenseEntity>> GetPendingApprovalsAsync(int? campaignId = null);
    Task<bool> ApproveAsync(long expenseId, string actorUserId, string? note, byte[] expectedRowVersion, SqlTransaction transaction);
    Task<bool> RejectAsync(long expenseId, string actorUserId, string? note, byte[] expectedRowVersion, SqlTransaction transaction);
    Task<IReadOnlyList<ExpenseApprovalEntity>> GetApprovalHistoryAsync(long expenseId);
    Task<IReadOnlyList<ReceiptMetadataEntity>> GetReceiptsAsync(long expenseId);
}
