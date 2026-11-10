using FluentValidation;
using Hrms.Assets.Application.Abstractions;
using Hrms.Assets.Application.Models;
using Hrms.Assets.Application.Services;
using Hrms.Assets.Domain.Entities;
using Hrms.Assets.Domain.Enums;
using Hrms.Assets.Domain.Services;
using Hrms.Employees.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Assets.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly IAssetsDbContext _dbContext;
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetAssignmentRepository _assignmentRepository;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IValidator<CreateAssetCommand> _createValidator;
    private readonly IValidator<UpdateAssetCommand> _updateValidator;
    private readonly IValidator<AssignAssetCommand> _assignValidator;
    private readonly IValidator<ReturnAssetCommand> _returnValidator;

    public AssetService(
        IAssetsDbContext dbContext,
        IAssetRepository assetRepository,
        IAssetAssignmentRepository assignmentRepository,
        IEmployeeLookup employeeLookup,
        ICurrentUser currentUser,
        IClock clock,
        IValidator<CreateAssetCommand> createValidator,
        IValidator<UpdateAssetCommand> updateValidator,
        IValidator<AssignAssetCommand> assignValidator,
        IValidator<ReturnAssetCommand> returnValidator)
    {
        _dbContext = dbContext;
        _assetRepository = assetRepository;
        _assignmentRepository = assignmentRepository;
        _employeeLookup = employeeLookup;
        _currentUser = currentUser;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _assignValidator = assignValidator;
        _returnValidator = returnValidator;
    }

    public async Task<Result<Guid>> CreateAssetAsync(CreateAssetCommand command, CancellationToken ct = default)
    {
        var validationResult = await _createValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Result.Failure<Guid>(new Error("Validation.Failed", firstError));
        }

        if (await _assetRepository.ExistsTagAsync(command.AssetTag, cancellationToken: ct))
        {
            return Result.Failure<Guid>(new Error("Asset.DuplicateTag", $"Asset tag '{command.AssetTag}' is already in use."));
        }

        if (!string.IsNullOrWhiteSpace(command.SerialNumber) &&
            await _assetRepository.ExistsSerialNumberAsync(command.SerialNumber, cancellationToken: ct))
        {
            return Result.Failure<Guid>(new Error("Asset.DuplicateSerial", $"Serial number '{command.SerialNumber}' is already registered."));
        }

        var asset = Asset.Create(
            command.AssetTag,
            command.Name,
            command.Category,
            command.SerialNumber,
            command.PurchaseDate,
            command.Condition,
            command.Notes);

        await _assetRepository.AddAsync(asset, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(asset.AssetId);
    }

    public async Task<Result> UpdateAssetAsync(UpdateAssetCommand command, CancellationToken ct = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Result.Failure(new Error("Validation.Failed", firstError));
        }

        var asset = await _assetRepository.GetByIdAsync(command.AssetId, ct);
        if (asset == null)
        {
            return Result.Failure(new Error("Asset.NotFound", "Asset not found."));
        }

        if (!string.IsNullOrWhiteSpace(command.SerialNumber) &&
            await _assetRepository.ExistsSerialNumberAsync(command.SerialNumber, asset.AssetId, ct))
        {
            return Result.Failure(new Error("Asset.DuplicateSerial", $"Serial number '{command.SerialNumber}' is already in use by another asset."));
        }

        var activeAssignment = await _assignmentRepository.GetActiveAssignmentByAssetIdAsync(asset.AssetId, ct);
        var transitionValidation = AssetRuleValidator.ValidateStatusTransition(asset, activeAssignment, command.Status);
        if (transitionValidation.IsFailure)
        {
            return transitionValidation;
        }

        // Set concurrency token
        _dbContext.Assets.Entry(asset).Property(a => a.RowVersion).OriginalValue = command.RowVersion;

        try
        {
            asset.UpdateDetails(
                command.Name,
                command.Category,
                command.SerialNumber,
                command.PurchaseDate,
                command.Condition,
                command.Status,
                command.Notes);

            _assetRepository.Update(asset);
            await _dbContext.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(new Error("Concurrency.Conflict", "The asset was modified by another user. Please reload and try again."));
        }
    }

    public async Task<Result<Guid>> AssignAssetAsync(AssignAssetCommand command, CancellationToken ct = default)
    {
        var validationResult = await _assignValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Result.Failure<Guid>(new Error("Validation.Failed", firstError));
        }

        var asset = await _assetRepository.GetByIdAsync(command.AssetId, ct);
        if (asset == null)
        {
            return Result.Failure<Guid>(new Error("Asset.NotFound", "Asset not found."));
        }

        var activeAssignment = await _assignmentRepository.GetActiveAssignmentByAssetIdAsync(asset.AssetId, ct);
        var employee = await _employeeLookup.GetBasicInfoAsync(command.EmployeeId, ct);

        var ruleValidation = AssetRuleValidator.ValidateAssignment(asset, activeAssignment, employee?.Status);
        if (ruleValidation.IsFailure)
        {
            return Result.Failure<Guid>(ruleValidation.Error);
        }

        var assignment = AssetAssignment.Create(
            asset.AssetId,
            command.EmployeeId,
            command.AssignedAtUtc,
            _currentUser.UserId,
            command.Note);

        asset.MarkAssigned();

        await _assignmentRepository.AddAsync(assignment, ct);
        _assetRepository.Update(asset);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(assignment.AssetAssignmentId);
    }

    public async Task<Result> ReturnAssetAsync(ReturnAssetCommand command, CancellationToken ct = default)
    {
        var validationResult = await _returnValidator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Result.Failure(new Error("Validation.Failed", firstError));
        }

        var asset = await _assetRepository.GetByIdAsync(command.AssetId, ct);
        if (asset == null)
        {
            return Result.Failure(new Error("Asset.NotFound", "Asset not found."));
        }

        var activeAssignment = await _assignmentRepository.GetActiveAssignmentByAssetIdAsync(asset.AssetId, ct);
        var returnValidation = AssetRuleValidator.ValidateReturn(asset, activeAssignment, command.ReturnedAtUtc);
        if (returnValidation.IsFailure)
        {
            return returnValidation;
        }

        activeAssignment!.MarkReturned(command.ReturnedAtUtc, _currentUser.UserId, command.Note);
        asset.MarkReturned(command.ReturnedCondition, command.TargetStatus);

        _assetRepository.Update(asset);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<PagedResult<AssetListItemDto>> GetAssetsAsync(
        string? search,
        AssetCategory? category,
        AssetStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.Assets
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.AssetTag.ToLower().Contains(s) ||
                                     a.Name.ToLower().Contains(s) ||
                                     (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(s)));
        }

        if (category.HasValue)
        {
            query = query.Where(a => a.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var assets = await query
            .OrderBy(a => a.AssetTag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.AssetId,
                a.AssetTag,
                a.Name,
                a.Category,
                a.SerialNumber,
                a.Condition,
                a.Status
            })
            .ToListAsync(ct);

        var assetIds = assets.Select(a => a.AssetId).ToList();

        // Load open assignments
        var activeAssignments = await _dbContext.AssetAssignments
            .AsNoTracking()
            .Where(asg => assetIds.Contains(asg.AssetId) && asg.ReturnedAtUtc == null)
            .ToListAsync(ct);

        var employeeIds = activeAssignments.Select(asg => asg.EmployeeId).Distinct().ToList();
        var employeeMap = new Dictionary<Guid, EmployeeBasicDto>();
        foreach (var empId in employeeIds)
        {
            var info = await _employeeLookup.GetBasicInfoAsync(empId, ct);
            if (info != null)
            {
                employeeMap[empId] = info;
            }
        }

        var items = assets.Select(a =>
        {
            var asg = activeAssignments.FirstOrDefault(x => x.AssetId == a.AssetId);
            EmployeeBasicDto? emp = null;
            if (asg != null && employeeMap.TryGetValue(asg.EmployeeId, out var foundEmp))
            {
                emp = foundEmp;
            }

            return new AssetListItemDto
            {
                AssetId = a.AssetId,
                AssetTag = a.AssetTag,
                Name = a.Name,
                Category = a.Category,
                SerialNumber = a.SerialNumber,
                Condition = a.Condition,
                Status = a.Status,
                CurrentEmployeeId = asg?.EmployeeId,
                CurrentEmployeeName = emp?.FullName,
                CurrentEmployeeCode = emp?.EmployeeCode,
                AssignedAtUtc = asg?.AssignedAtUtc
            };
        }).ToList();

        return new PagedResult<AssetListItemDto>(items, totalCount, page, pageSize);
    }

    public async Task<AssetDetailsDto?> GetAssetDetailsAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await _dbContext.Assets
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssetId == assetId, ct);

        if (asset == null)
        {
            return null;
        }

        var dto = new AssetDetailsDto
        {
            AssetId = asset.AssetId,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Category = asset.Category,
            SerialNumber = asset.SerialNumber,
            PurchaseDate = asset.PurchaseDate,
            Condition = asset.Condition,
            Status = asset.Status,
            Notes = asset.Notes,
            RowVersion = asset.RowVersion,
            CreatedAtUtc = asset.CreatedAtUtc
        };

        var activeAssignment = await _dbContext.AssetAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(asg => asg.AssetId == assetId && asg.ReturnedAtUtc == null, ct);

        if (activeAssignment != null)
        {
            dto.CurrentAssignmentId = activeAssignment.AssetAssignmentId;
            dto.CurrentEmployeeId = activeAssignment.EmployeeId;
            dto.CurrentAssignedAtUtc = activeAssignment.AssignedAtUtc;
            dto.CurrentAssignmentNote = activeAssignment.Note;

            var emp = await _employeeLookup.GetBasicInfoAsync(activeAssignment.EmployeeId, ct);
            if (emp != null)
            {
                dto.CurrentEmployeeName = emp.FullName;
                dto.CurrentEmployeeCode = emp.EmployeeCode;
            }
        }

        return dto;
    }

    public async Task<IReadOnlyList<AssetAssignmentHistoryDto>> GetAssetHistoryAsync(Guid assetId, CancellationToken ct = default)
    {
        var assignments = await _dbContext.AssetAssignments
            .AsNoTracking()
            .Where(asg => asg.AssetId == assetId)
            .OrderByDescending(asg => asg.AssignedAtUtc)
            .ToListAsync(ct);

        var list = new List<AssetAssignmentHistoryDto>();
        foreach (var asg in assignments)
        {
            var emp = await _employeeLookup.GetBasicInfoAsync(asg.EmployeeId, ct);
            list.Add(new AssetAssignmentHistoryDto
            {
                AssetAssignmentId = asg.AssetAssignmentId,
                EmployeeId = asg.EmployeeId,
                EmployeeName = emp?.FullName ?? "Unknown Employee",
                EmployeeCode = emp?.EmployeeCode ?? "-",
                AssignedAtUtc = asg.AssignedAtUtc,
                ReturnedAtUtc = asg.ReturnedAtUtc,
                Note = asg.Note
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<MyAssetDto>> GetMyAssetsAsync(Guid employeeId, CancellationToken ct = default)
    {
        var assignments = await _dbContext.AssetAssignments
            .AsNoTracking()
            .Where(asg => asg.EmployeeId == employeeId && asg.ReturnedAtUtc == null)
            .ToListAsync(ct);

        if (assignments.Count == 0)
        {
            return Array.Empty<MyAssetDto>();
        }

        var assetIds = assignments.Select(a => a.AssetId).ToList();
        var assets = await _dbContext.Assets
            .AsNoTracking()
            .Where(a => assetIds.Contains(a.AssetId))
            .ToDictionaryAsync(a => a.AssetId, ct);

        var result = new List<MyAssetDto>();
        foreach (var asg in assignments)
        {
            if (assets.TryGetValue(asg.AssetId, out var asset))
            {
                result.Add(new MyAssetDto
                {
                    AssetId = asset.AssetId,
                    AssetTag = asset.AssetTag,
                    Name = asset.Name,
                    Category = asset.Category,
                    SerialNumber = asset.SerialNumber,
                    Condition = asset.Condition,
                    AssignedAtUtc = asg.AssignedAtUtc,
                    Note = asg.Note
                });
            }
        }

        return result.OrderBy(a => a.AssetTag).ToList();
    }

    public async Task<Result<Guid>> ReportAssetIssueAsync(CreateAssetRequestCommand command, CancellationToken ct = default)
    {
        if (command.AssetId == Guid.Empty)
        {
            return Result.Failure<Guid>(new Error("Asset.Required", "Asset ID is required."));
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            return Result.Failure<Guid>(new Error("Description.Required", "Please provide a description of the issue."));
        }

        var asset = await _assetRepository.GetByIdAsync(command.AssetId, ct);
        if (asset == null)
        {
            return Result.Failure<Guid>(new Error("Asset.NotFound", "The specified asset was not found."));
        }

        var request = AssetRequest.Create(
            command.AssetId,
            command.EmployeeId,
            string.IsNullOrWhiteSpace(command.IssueType) ? "Malfunction" : command.IssueType,
            command.Description,
            _clock.UtcNow);

        _dbContext.AssetRequests.Add(request);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success(request.RequestId);
    }

    public async Task<IReadOnlyList<AssetRequestDto>> GetMyAssetRequestsAsync(Guid employeeId, CancellationToken ct = default)
    {
        var requests = await _dbContext.AssetRequests
            .AsNoTracking()
            .Include(r => r.Asset)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return requests.Select(r => new AssetRequestDto
        {
            RequestId = r.RequestId,
            AssetId = r.AssetId,
            AssetTag = r.Asset?.AssetTag ?? "N/A",
            AssetName = r.Asset?.Name ?? "N/A",
            Category = r.Asset?.Category ?? AssetCategory.Other,
            EmployeeId = r.EmployeeId,
            IssueType = r.IssueType,
            Description = r.Description,
            Status = r.Status,
            AdminNotes = r.AdminNotes,
            CreatedAtUtc = r.CreatedAtUtc,
            ResolvedAtUtc = r.ResolvedAtUtc
        }).ToList();
    }

    public async Task<Result> CancelAssetRequestAsync(Guid requestId, Guid employeeId, CancellationToken ct = default)
    {
        var request = await _dbContext.AssetRequests
            .FirstOrDefaultAsync(r => r.RequestId == requestId && r.EmployeeId == employeeId, ct);

        if (request == null)
        {
            return Result.Failure(new Error("AssetRequest.NotFound", "Asset request not found."));
        }

        if (request.Status != AssetRequestStatus.Pending)
        {
            return Result.Failure(new Error("AssetRequest.NotPending", "Only pending requests can be cancelled. This request has already been reviewed or resolved."));
        }

        _dbContext.AssetRequests.Remove(request);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<PagedResult<AssetRequestDto>> GetAssetRequestsAsync(string? search, AssetRequestStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _dbContext.AssetRequests
            .AsNoTracking()
            .Include(r => r.Asset)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r =>
                (r.Asset != null && (r.Asset.AssetTag.ToLower().Contains(s) || r.Asset.Name.ToLower().Contains(s))) ||
                r.IssueType.ToLower().Contains(s) ||
                r.Description.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var pageNumber = page < 1 ? 1 : page;
        var size = pageSize < 1 ? 10 : pageSize;

        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var employeeDropdown = await _employeeLookup.GetActiveEmployeesDropdownAsync(ct);
        var empMap = employeeDropdown.ToDictionary(e => e.EmployeeId, e => e);

        var dtos = items.Select(r =>
        {
            empMap.TryGetValue(r.EmployeeId, out var emp);
            return new AssetRequestDto
            {
                RequestId = r.RequestId,
                AssetId = r.AssetId,
                AssetTag = r.Asset?.AssetTag ?? "N/A",
                AssetName = r.Asset?.Name ?? "N/A",
                Category = r.Asset?.Category ?? AssetCategory.Other,
                EmployeeId = r.EmployeeId,
                EmployeeName = emp?.FullName ?? "Staff Employee",
                EmployeeCode = emp?.EmployeeCode ?? "-",
                IssueType = r.IssueType,
                Description = r.Description,
                Status = r.Status,
                AdminNotes = r.AdminNotes,
                CreatedAtUtc = r.CreatedAtUtc,
                ResolvedAtUtc = r.ResolvedAtUtc
            };
        }).ToList();

        return new PagedResult<AssetRequestDto>(dtos, total, pageNumber, size);
    }

    public async Task<Result> ResolveAssetRequestAsync(ResolveAssetRequestCommand command, CancellationToken ct = default)
    {
        var request = await _dbContext.AssetRequests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, ct);

        if (request == null)
        {
            return Result.Failure(new Error("AssetRequest.NotFound", "The asset request was not found."));
        }

        request.Resolve(command.Status, command.AdminNotes, command.AdminUserId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<AssetDashboardMetricsDto> GetAssetDashboardMetricsAsync(CancellationToken ct = default)
    {
        var assets = await _dbContext.Assets.AsNoTracking().ToListAsync(ct);
        var pendingCount = await _dbContext.AssetRequests.CountAsync(r => r.Status == AssetRequestStatus.Pending, ct);

        var categoryCounts = Enum.GetValues<AssetCategory>()
            .ToDictionary(c => c, c => assets.Count(a => a.Category == c));

        var conditionCounts = Enum.GetValues<AssetCondition>()
            .ToDictionary(c => c, c => assets.Count(a => a.Condition == c));

        var recentRequestsPaged = await GetAssetRequestsAsync(null, null, 1, 5, ct);
        var recentAssignments = await _dbContext.AssetAssignments
            .AsNoTracking()
            .Include(a => a.Asset)
            .OrderByDescending(a => a.AssignedAtUtc)
            .Take(5)
            .ToListAsync(ct);

        var employeeDropdown = await _employeeLookup.GetActiveEmployeesDropdownAsync(ct);
        var empMap = employeeDropdown.ToDictionary(e => e.EmployeeId, e => e);

        var assignmentDtos = recentAssignments.Select(a =>
        {
            empMap.TryGetValue(a.EmployeeId, out var emp);
            return new AssetListItemDto
            {
                AssetId = a.AssetId,
                AssetTag = a.Asset?.AssetTag ?? "N/A",
                Name = a.Asset?.Name ?? "N/A",
                Category = a.Asset?.Category ?? AssetCategory.Other,
                Condition = a.Asset?.Condition ?? AssetCondition.Good,
                Status = a.Asset?.Status ?? AssetStatus.Assigned,
                CurrentEmployeeId = a.EmployeeId,
                CurrentEmployeeName = emp?.FullName,
                CurrentEmployeeCode = emp?.EmployeeCode,
                AssignedAtUtc = a.AssignedAtUtc
            };
        }).ToList();

        return new AssetDashboardMetricsDto
        {
            TotalAssets = assets.Count,
            AssignedAssets = assets.Count(a => a.Status == AssetStatus.Assigned),
            AvailableAssets = assets.Count(a => a.Status == AssetStatus.Available),
            InRepairAssets = assets.Count(a => a.Status == AssetStatus.InRepair),
            RetiredAssets = assets.Count(a => a.Status == AssetStatus.Retired),
            PendingRequestsCount = pendingCount,
            CategoryCounts = categoryCounts,
            ConditionCounts = conditionCounts,
            RecentRequests = recentRequestsPaged.Items.ToList(),
            RecentAssignments = assignmentDtos
        };
    }
}