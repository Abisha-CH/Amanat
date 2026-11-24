using Amanat.Data;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace Amanat.Repositories;

public sealed class CampaignRepository : ICampaignRepository
{
    private static readonly string[] Statuses = { "Draft", "Active", "Closed", "Archived" };
    private readonly IDbConnectionFactory _connectionFactory;

    public CampaignRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CampaignEntity>> GetAllAsync(bool activeOnly = true)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var sql = activeOnly
            ? "SELECT * FROM dbo.Campaigns WHERE Status = @Status ORDER BY Title;"
            : "SELECT * FROM dbo.Campaigns ORDER BY Title;";
        var rows = await connection.QueryAsync<CampaignEntity>(sql, new { Status = "Active" });
        return rows.AsList();
    }

    public async Task<CampaignEntity?> GetBySlugAsync(string slug)
    {
        RepositoryGuard.RequiredText(slug, nameof(slug), 100);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.Campaigns WHERE Slug = @Slug;";
        return await connection.QuerySingleOrDefaultAsync<CampaignEntity>(sql, new { Slug = slug });
    }

    public async Task<CampaignEntity?> GetByIdAsync(int campaignId)
    {
        RepositoryGuard.Positive(campaignId, nameof(campaignId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.Campaigns WHERE CampaignId = @CampaignId;";
        return await connection.QuerySingleOrDefaultAsync<CampaignEntity>(sql, new { CampaignId = campaignId });
    }

    public async Task<int> CreateAsync(CampaignEntity campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        NormalizeCampaign(campaign);
        ValidateCampaign(campaign);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        const string sql = @"
INSERT dbo.Campaigns
    (Slug, Title, Organization, Category, FocusKey, Thumbnail, ThumbnailAlt, ThumbnailLabel,
     HomeThumbnail, HomeThumbnailAlt, DetailsImage, DetailsImageAlt, Summary, HomeSummary,
     LedgerNote, Goal, StartedOn, Deadline, Status, CreatedByUserId, UpdatedByUserId,
     CreatedAtUtc, UpdatedAtUtc, ClosedAtUtc, FinalSummary)
VALUES
    (@Slug, @Title, @Organization, @Category, @FocusKey, @Thumbnail, @ThumbnailAlt, @ThumbnailLabel,
     @HomeThumbnail, @HomeThumbnailAlt, @DetailsImage, @DetailsImageAlt, @Summary, @HomeSummary,
     @LedgerNote, @Goal, @StartedOn, @Deadline, @Status, @CreatedByUserId, @UpdatedByUserId,
     @CreatedAtUtc, @UpdatedAtUtc, @ClosedAtUtc, @FinalSummary);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        try
        {
            var campaignId = await connection.QuerySingleAsync<int>(sql, campaign, transaction);
            await RepositoryAudit.WriteAsync(
                connection,
                transaction,
                "Campaign",
                campaignId.ToString(),
                "Created",
                campaign.CreatedByUserId,
                campaign.Title,
                Guid.NewGuid());
            await transaction.CommitAsync();
            return campaignId;
        }
        catch (SqlException exception) when (RepositoryGuard.IsDuplicateKey(exception))
        {
            throw new InvalidOperationException("A campaign with this slug already exists.", exception);
        }
    }

    public async Task<bool> UpdateAsync(CampaignEntity campaign, byte[] expectedRowVersion)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        RepositoryGuard.Positive(campaign.CampaignId, nameof(campaign.CampaignId));
        RepositoryGuard.RowVersion(expectedRowVersion, nameof(expectedRowVersion));
        NormalizeCampaign(campaign);
        ValidateCampaign(campaign);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        const string stateSql = "SELECT Status, RowVersion FROM dbo.Campaigns WHERE CampaignId = @CampaignId;";
        var current = await connection.QuerySingleOrDefaultAsync<CampaignDecisionState>(
            stateSql,
            new { campaign.CampaignId },
            transaction);

        if (current is null)
        {
            throw new InvalidOperationException($"Campaign {campaign.CampaignId} was not found.");
        }

        if (current.Status == "Archived" && campaign.Status != "Archived")
        {
            throw new InvalidOperationException("Archived campaigns cannot be reactivated.");
        }

        if (!current.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw RepositoryGuard.Concurrency($"Campaign {campaign.CampaignId} was modified by another operation.");
        }

        campaign.UpdatedAtUtc = DateTime.UtcNow;
        if (campaign.Status is "Closed" or "Archived")
        {
            campaign.ClosedAtUtc ??= DateTime.UtcNow;
        }

        const string updateSql = @"
UPDATE dbo.Campaigns
SET Slug = @Slug,
    Title = @Title,
    Organization = @Organization,
    Category = @Category,
    FocusKey = @FocusKey,
    Thumbnail = @Thumbnail,
    ThumbnailAlt = @ThumbnailAlt,
    ThumbnailLabel = @ThumbnailLabel,
    HomeThumbnail = @HomeThumbnail,
    HomeThumbnailAlt = @HomeThumbnailAlt,
    DetailsImage = @DetailsImage,
    DetailsImageAlt = @DetailsImageAlt,
    Summary = @Summary,
    HomeSummary = @HomeSummary,
    LedgerNote = @LedgerNote,
    Goal = @Goal,
    StartedOn = @StartedOn,
    Deadline = @Deadline,
    Status = @Status,
    UpdatedByUserId = @UpdatedByUserId,
    UpdatedAtUtc = @UpdatedAtUtc,
    ClosedAtUtc = @ClosedAtUtc,
    FinalSummary = @FinalSummary
WHERE CampaignId = @CampaignId AND RowVersion = @Expected;";

        var parameters = new
        {
            campaign.CampaignId,
            campaign.Slug,
            campaign.Title,
            campaign.Organization,
            campaign.Category,
            campaign.FocusKey,
            campaign.Thumbnail,
            campaign.ThumbnailAlt,
            campaign.ThumbnailLabel,
            campaign.HomeThumbnail,
            campaign.HomeThumbnailAlt,
            campaign.DetailsImage,
            campaign.DetailsImageAlt,
            campaign.Summary,
            campaign.HomeSummary,
            campaign.LedgerNote,
            campaign.Goal,
            campaign.StartedOn,
            campaign.Deadline,
            campaign.Status,
            campaign.UpdatedByUserId,
            campaign.UpdatedAtUtc,
            campaign.ClosedAtUtc,
            campaign.FinalSummary,
            Expected = expectedRowVersion
        };

        try
        {
            var affected = await connection.ExecuteAsync(updateSql, parameters, transaction);
            if (affected != 1)
            {
                throw RepositoryGuard.Concurrency($"Campaign {campaign.CampaignId} was modified by another operation.");
            }

            await RepositoryAudit.WriteAsync(
                connection,
                transaction,
                "Campaign",
                campaign.CampaignId.ToString(),
                "Updated",
                campaign.UpdatedByUserId ?? campaign.CreatedByUserId,
                campaign.Title,
                Guid.NewGuid());
            await transaction.CommitAsync();
            return true;
        }
        catch (SqlException exception) when (RepositoryGuard.IsDuplicateKey(exception))
        {
            throw new InvalidOperationException("A campaign with this slug already exists.", exception);
        }
    }

    public async Task<bool> ArchiveAsync(int campaignId, byte[] expectedRowVersion)
    {
        RepositoryGuard.Positive(campaignId, nameof(campaignId));
        RepositoryGuard.RowVersion(expectedRowVersion, nameof(expectedRowVersion));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        const string stateSql = "SELECT Status, RowVersion FROM dbo.Campaigns WHERE CampaignId = @CampaignId;";
        var current = await connection.QuerySingleOrDefaultAsync<CampaignDecisionState>(
            stateSql,
            new { CampaignId = campaignId },
            transaction);

        if (current is null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} was not found.");
        }

        if (current.Status == "Archived")
        {
            throw new InvalidOperationException($"Campaign {campaignId} is already archived.");
        }

        if (!current.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw RepositoryGuard.Concurrency($"Campaign {campaignId} was modified by another operation.");
        }

        const string updateSql = @"
UPDATE dbo.Campaigns
SET Status = N'Archived',
    ClosedAtUtc = ISNULL(ClosedAtUtc, SYSUTCDATETIME()),
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE CampaignId = @CampaignId AND Status <> N'Archived' AND RowVersion = @Expected;";

        var affected = await connection.ExecuteAsync(
            updateSql,
            new { CampaignId = campaignId, Expected = expectedRowVersion },
            transaction);

        if (affected != 1)
        {
            throw RepositoryGuard.Concurrency($"Campaign {campaignId} was modified by another operation.");
        }

        await RepositoryAudit.WriteAsync(
            connection,
            transaction,
            "Campaign",
            campaignId.ToString(),
            "Archived",
            null,
            null,
            Guid.NewGuid());
        await transaction.CommitAsync();
        return true;
    }

    public async Task<IReadOnlyList<CampaignUpdateEntity>> GetUpdatesAsync(int campaignId, bool publicOnly)
    {
        RepositoryGuard.Positive(campaignId, nameof(campaignId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var sql = publicOnly
            ? "SELECT * FROM dbo.CampaignUpdates WHERE CampaignId = @CampaignId AND IsPublic = 1 ORDER BY CreatedAtUtc DESC;"
            : "SELECT * FROM dbo.CampaignUpdates WHERE CampaignId = @CampaignId ORDER BY CreatedAtUtc DESC;";
        var rows = await connection.QueryAsync<CampaignUpdateEntity>(sql, new { CampaignId = campaignId });
        return rows.AsList();
    }

    public async Task<int> AddUpdateAsync(CampaignUpdateEntity update)
    {
        ArgumentNullException.ThrowIfNull(update);
        RepositoryGuard.Positive(update.CampaignId, nameof(update.CampaignId));
        RepositoryGuard.RequiredText(update.AuthorUserId, nameof(update.AuthorUserId), 450);
        RepositoryGuard.RequiredText(update.Title, nameof(update.Title), 200);
        RepositoryGuard.RequiredText(update.Body, nameof(update.Body), int.MaxValue);

        if (update.CreatedAtUtc == default)
        {
            update.CreatedAtUtc = DateTime.UtcNow;
        }

        if (update.IsPublic && update.PublishedAtUtc is null)
        {
            update.PublishedAtUtc = DateTime.UtcNow;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        const string sql = @"
INSERT dbo.CampaignUpdates
    (CampaignId, AuthorUserId, Title, Body, IsPublic, CreatedAtUtc, PublishedAtUtc, UpdatedAtUtc)
VALUES
    (@CampaignId, @AuthorUserId, @Title, @Body, @IsPublic, @CreatedAtUtc, @PublishedAtUtc, @UpdatedAtUtc);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        var updateId = await connection.QuerySingleAsync<int>(sql, update, transaction);
        await RepositoryAudit.WriteAsync(
            connection,
            transaction,
            "CampaignUpdate",
            updateId.ToString(),
            "Created",
            update.AuthorUserId,
            update.Title,
            Guid.NewGuid());
        await transaction.CommitAsync();
        return updateId;
    }

    private static void NormalizeCampaign(CampaignEntity campaign)
    {
        if (string.IsNullOrWhiteSpace(campaign.Status))
        {
            campaign.Status = "Draft";
        }

        if (campaign.CreatedAtUtc == default)
        {
            campaign.CreatedAtUtc = DateTime.UtcNow;
        }

        if (campaign.Status is "Closed" or "Archived")
        {
            campaign.ClosedAtUtc ??= DateTime.UtcNow;
        }
    }

    private static void ValidateCampaign(CampaignEntity campaign)
    {
        RepositoryGuard.RequiredText(campaign.Slug, nameof(campaign.Slug), 100);
        RepositoryGuard.RequiredText(campaign.Title, nameof(campaign.Title), 200);
        RepositoryGuard.RequiredText(campaign.Organization, nameof(campaign.Organization), 200);
        RepositoryGuard.RequiredText(campaign.Category, nameof(campaign.Category), 100);
        RepositoryGuard.RequiredText(campaign.FocusKey, nameof(campaign.FocusKey), 50);
        RepositoryGuard.RequiredText(campaign.CreatedByUserId, nameof(campaign.CreatedByUserId), 450);
        RepositoryGuard.Positive(campaign.Goal, nameof(campaign.Goal));
        RepositoryGuard.AllowedStatus(campaign.Status, nameof(campaign.Status), Statuses);

        if (campaign.UpdatedByUserId is not null && campaign.UpdatedByUserId.Length > 450)
        {
            throw new InvalidOperationException("UpdatedByUserId must be at most 450 characters.");
        }

        if (campaign.Thumbnail is { Length: > 300 })
        {
            throw new InvalidOperationException("Thumbnail must be at most 300 characters.");
        }

        if (campaign.ThumbnailAlt is { Length: > 300 })
        {
            throw new InvalidOperationException("ThumbnailAlt must be at most 300 characters.");
        }

        if (campaign.ThumbnailLabel is { Length: > 100 })
        {
            throw new InvalidOperationException("ThumbnailLabel must be at most 100 characters.");
        }

        if (campaign.HomeThumbnail is { Length: > 300 })
        {
            throw new InvalidOperationException("HomeThumbnail must be at most 300 characters.");
        }

        if (campaign.HomeThumbnailAlt is { Length: > 300 })
        {
            throw new InvalidOperationException("HomeThumbnailAlt must be at most 300 characters.");
        }

        if (campaign.DetailsImage is { Length: > 300 })
        {
            throw new InvalidOperationException("DetailsImage must be at most 300 characters.");
        }

        if (campaign.DetailsImageAlt is { Length: > 300 })
        {
            throw new InvalidOperationException("DetailsImageAlt must be at most 300 characters.");
        }

        if (campaign.StartedOn == default)
        {
            throw new InvalidOperationException("StartedOn is required.");
        }

        if (campaign.Deadline == default)
        {
            throw new InvalidOperationException("Deadline is required.");
        }

        if (campaign.Deadline < campaign.StartedOn)
        {
            throw new InvalidOperationException("Deadline cannot be earlier than StartedOn.");
        }
    }

    private sealed class CampaignDecisionState
    {
        public string Status { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
