using Amanat.Data;
using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories;
using Amanat.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace Amanat.Services;

public sealed class SqlDomainMutationService : IDomainMutationService
{
    private const string UnavailableMessage = "The fund record is temporarily unavailable. Please try again.";
    private readonly IDbConnectionFactory _connections;
    private readonly ICampaignRepository _campaigns;
    private readonly IContributionRepository _contributions;
    private readonly IExpenseRepository _expenses;

    public SqlDomainMutationService(
        IDbConnectionFactory connections,
        ICampaignRepository campaigns,
        IContributionRepository contributions,
        IExpenseRepository expenses)
    {
        _connections = connections;
        _campaigns = campaigns;
        _contributions = contributions;
        _expenses = expenses;
    }

    public Task<int> CreateCampaignAsync(CampaignCreateInput input, ApplicationUser organizer) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            if (organizer is null || string.IsNullOrWhiteSpace(organizer.Id))
            {
                throw new DomainUnauthorizedException("You must be signed in to manage campaigns.");
            }

            var slug = (input.Slug ?? string.Empty).Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            {
                throw new DomainValidationException("Use a URL slug with lowercase letters, numbers, and hyphens.");
            }

            if (input.Deadline <= input.StartedOn)
            {
                throw new DomainValidationException("The deadline must be after the start date.");
            }

            var existing = await _campaigns.GetBySlugAsync(slug);
            if (existing is not null)
            {
                throw new DomainValidationException("A campaign with this slug already exists.");
            }

            var focus = (input.FocusKey ?? string.Empty).Trim().ToLowerInvariant();
            var imageKey = focus switch
            {
                "health" => "medical",
                "welfare" => "library",
                _ => focus
            };

            try
            {
                return await _campaigns.CreateAsync(new CampaignEntity
                {
                    Slug = slug,
                    Title = input.Title.Trim(),
                    Organization = input.Organization.Trim(),
                    Category = input.Category.Trim(),
                    FocusKey = focus,
                    Thumbnail = "assets/images/campaign-" + imageKey + ".jpg",
                    ThumbnailAlt = input.Title.Trim(),
                    ThumbnailLabel = input.Category.Trim(),
                    HomeThumbnail = "assets/images/campaign-" + imageKey + "-home.jpg",
                    HomeThumbnailAlt = input.Title.Trim(),
                    DetailsImage = "assets/images/campaign-" + imageKey + ".jpg",
                    DetailsImageAlt = input.Title.Trim(),
                    Summary = input.Summary.Trim(),
                    HomeSummary = input.Summary.Trim(),
                    LedgerNote = "Verified ledger entries",
                    Goal = input.Goal,
                    StartedOn = input.StartedOn,
                    Deadline = input.Deadline,
                    Status = "Active",
                    CreatedByUserId = organizer.Id
                });
            }
            catch (InvalidOperationException)
            {
                throw new DomainValidationException("The campaign could not be saved. Please check the details and try again.");
            }
        }, UnavailableMessage);

    public Task<int> AddCampaignUpdateAsync(CampaignUpdateInput input, ApplicationUser organizer) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            var campaign = await _campaigns.GetByIdAsync(input.CampaignId);
            if (campaign is null)
            {
                throw new DomainValidationException("The selected campaign was not found.");
            }

            if (campaign.CreatedByUserId != organizer.Id)
            {
                throw new DomainUnauthorizedException("You can only post updates for your own campaigns.");
            }

            if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Body))
            {
                throw new DomainValidationException("Enter a title and update before posting.");
            }

            return await _campaigns.AddUpdateAsync(new CampaignUpdateEntity
            {
                CampaignId = campaign.CampaignId,
                AuthorUserId = organizer.Id,
                Title = input.Title.Trim(),
                Body = input.Body.Trim(),
                IsPublic = true,
                PublishedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow
            });
        }, UnavailableMessage);

    public Task ArchiveCampaignAsync(int campaignId, string rowVersion, ApplicationUser organizer) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            var campaign = await _campaigns.GetByIdAsync(campaignId);
            if (campaign is null)
            {
                throw new DomainValidationException("The selected campaign was not found.");
            }

            if (campaign.CreatedByUserId != organizer.Id)
            {
                throw new DomainUnauthorizedException("You can only manage your own campaigns.");
            }

            var archived = await _campaigns.ArchiveAsync(campaignId, DecodeRowVersion(rowVersion));
            if (!archived)
            {
                throw new DomainValidationException("This campaign changed by another user. Reload the page and try again.");
            }
        }, UnavailableMessage);

    public Task<long> SubmitContributionAsync(ContributionEntity contribution, ApplicationUser? contributor) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            contribution.Status = "Pending";
            contribution.SubmittedAtUtc = DateTime.UtcNow;
            contribution.ContributorUserId = contributor?.Id;
            contribution.ExternalPaymentMethod = "External";
            contribution.DateSent ??= DateTime.UtcNow;

            try
            {
                return await WithTransactionAsync((connection, transaction) =>
                    _contributions.SubmitAsync(contribution, transaction));
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("transaction reference", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("That transaction reference has already been submitted.");
            }
            catch (SqlException exception) when (RepositoryGuard.IsDuplicateKey(exception))
            {
                throw new DomainValidationException("That transaction reference has already been submitted.");
            }
        }, UnavailableMessage);

    public Task DecideContributionAsync(
        long contributionId,
        string decision,
        string? note,
        string rowVersion,
        ApplicationUser reviewer) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            if (decision is not ("Verified" or "Rejected"))
            {
                throw new DomainValidationException("That contribution decision is not valid.");
            }

            var contribution = await _contributions.GetByIdAsync(contributionId);
            if (contribution is null)
            {
                throw new DomainValidationException("The contribution was not found.");
            }

            if (!string.IsNullOrWhiteSpace(contribution.ContributorUserId)
                && contribution.ContributorUserId == reviewer.Id)
            {
                throw new DomainUnauthorizedException("You cannot review your own contribution.");
            }

            if (!contribution.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("This contribution has already been reviewed.");
            }

            var updated = await WithTransactionAsync((connection, transaction) =>
                decision == "Verified"
                    ? _contributions.VerifyAsync(contributionId, reviewer.Id, note, DecodeRowVersion(rowVersion), transaction)
                    : _contributions.RejectAsync(contributionId, reviewer.Id, note, DecodeRowVersion(rowVersion), transaction));

            if (!updated)
            {
                throw new DomainValidationException("This contribution changed by another user. Reload the page and try again.");
            }
        }, UnavailableMessage);

    public Task<long> SubmitExpenseAsync(ExpenseEntity expense, ApplicationUser treasurer) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            var campaign = await _campaigns.GetByIdAsync(expense.CampaignId);
            if (campaign is null || !campaign.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("Select an active campaign for this expense.");
            }

            if (string.IsNullOrWhiteSpace(expense.Reference))
            {
                throw new DomainValidationException("An expense reference is required.");
            }

            expense.Status = "Pending";
            expense.SubmittedByUserId = treasurer.Id;
            expense.SubmittedAtUtc = DateTime.UtcNow;

            try
            {
                return await WithTransactionAsync((connection, transaction) =>
                    _expenses.SubmitAsync(expense, transaction));
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("reference", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("That expense reference already exists.");
            }
            catch (SqlException exception) when (RepositoryGuard.IsDuplicateKey(exception))
            {
                throw new DomainValidationException("That expense reference already exists.");
            }
        }, UnavailableMessage);

    public Task DecideExpenseAsync(
        long expenseId,
        string decision,
        string? note,
        string rowVersion,
        ApplicationUser organizer) =>
        SqlServiceGuard.RunAsync(async () =>
        {
            if (decision is not ("Approved" or "Rejected"))
            {
                throw new DomainValidationException("That expense decision is not valid.");
            }

            var expense = await _expenses.GetByIdAsync(expenseId);
            if (expense is null)
            {
                throw new DomainValidationException("The expense was not found.");
            }

            if (expense.SubmittedByUserId == organizer.Id)
            {
                throw new DomainUnauthorizedException("You cannot approve an expense you submitted.");
            }

            var campaign = await _campaigns.GetByIdAsync(expense.CampaignId);
            if (campaign is null || campaign.CreatedByUserId != organizer.Id)
            {
                throw new DomainUnauthorizedException("You can only approve expenses for your own campaigns.");
            }

            if (!expense.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("This expense has already been reviewed.");
            }

            var updated = await WithTransactionAsync((connection, transaction) =>
                decision == "Approved"
                    ? _expenses.ApproveAsync(expenseId, organizer.Id, note, DecodeRowVersion(rowVersion), transaction)
                    : _expenses.RejectAsync(expenseId, organizer.Id, note, DecodeRowVersion(rowVersion), transaction));

            if (!updated)
            {
                throw new DomainValidationException("This expense changed by another user. Reload the page and try again.");
            }
        }, UnavailableMessage);

    private async Task<T> WithTransactionAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> operation)
    {
        await using var connection = _connections.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static byte[] DecodeRowVersion(string value)
    {
        try
        {
            var bytes = Convert.FromBase64String(value);
            if (bytes.Length != 8)
            {
                throw new DomainValidationException("This record changed. Reload the page and try again.");
            }

            return bytes;
        }
        catch (FormatException)
        {
            throw new DomainValidationException("This record changed. Reload the page and try again.");
        }
    }
}
