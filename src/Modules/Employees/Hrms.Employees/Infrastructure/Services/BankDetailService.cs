using FluentValidation;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Services;

public class BankDetailService : IBankDetailService
{
    private readonly IEmployeesDbContext _dbContext;
    private readonly IValidator<SubmitBankDetailModel> _validator;

    public BankDetailService(
        IEmployeesDbContext dbContext,
        IValidator<SubmitBankDetailModel> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<EmployeeBankProfileDto> GetEmployeeBankProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return new EmployeeBankProfileDto(null, null, null);
        }

        var details = await _dbContext.EmployeeBankDetails
            .AsNoTracking()
            .Include(b => b.Employee)
                .ThenInclude(e => e!.Designation)
                    .ThenInclude(d => d!.Department)
            .Where(b => b.EmployeeId == employeeId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var active = details.FirstOrDefault(b => b.IsActive && b.Status == BankDetailStatus.Approved);
        var pending = details.FirstOrDefault(b => b.Status == BankDetailStatus.Pending);
        var rejected = details.FirstOrDefault(b => b.Status == BankDetailStatus.Rejected && (active == null || b.CreatedAtUtc > active.CreatedAtUtc));

        return new EmployeeBankProfileDto(
            active != null ? MapToDto(active) : null,
            pending != null ? MapToDto(pending) : null,
            rejected != null ? MapToDto(rejected) : null
        );
    }

    public async Task<Result<Guid>> SubmitBankDetailAsync(SubmitBankDetailModel model, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == model.EmployeeId, cancellationToken);

        if (employee == null)
        {
            return Result<Guid>.Failure(new Error("Employee.NotFound", "Employee not found."));
        }

        var existingPending = await _dbContext.EmployeeBankDetails
            .FirstOrDefaultAsync(b => b.EmployeeId == model.EmployeeId && b.Status == BankDetailStatus.Pending, cancellationToken);

        DateTime now = DateTime.UtcNow;

        if (existingPending != null)
        {
            existingPending.BankName = model.BankName.Trim();
            existingPending.AccountHolderName = model.AccountHolderName.Trim();
            existingPending.AccountNumber = model.AccountNumber.Trim();
            existingPending.BranchName = model.BranchName.Trim();
            existingPending.RoutingNumber = model.RoutingNumber?.Trim();
            existingPending.SwiftCode = model.SwiftCode?.Trim();
            existingPending.ReviewRemarks = null;
            existingPending.ReviewedByUserId = null;
            existingPending.ReviewedAtUtc = null;
            existingPending.UpdatedAtUtc = now;
            existingPending.UpdatedByUserId = currentUserId;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(existingPending.EmployeeBankDetailId);
        }

        var entity = new EmployeeBankDetail
        {
            EmployeeBankDetailId = Guid.NewGuid(),
            EmployeeId = model.EmployeeId,
            BankName = model.BankName.Trim(),
            AccountHolderName = model.AccountHolderName.Trim(),
            AccountNumber = model.AccountNumber.Trim(),
            BranchName = model.BranchName.Trim(),
            RoutingNumber = model.RoutingNumber?.Trim(),
            SwiftCode = model.SwiftCode?.Trim(),
            Status = BankDetailStatus.Pending,
            IsActive = false,
            CreatedAtUtc = now,
            CreatedByUserId = currentUserId,
            UpdatedAtUtc = now,
            UpdatedByUserId = currentUserId
        };

        _dbContext.EmployeeBankDetails.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.EmployeeBankDetailId);
    }

    public async Task<List<BankDetailDto>> GetPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.EmployeeBankDetails
            .AsNoTracking()
            .Include(b => b.Employee)
                .ThenInclude(e => e!.Designation)
                    .ThenInclude(d => d!.Department)
            .Where(b => b.Status == BankDetailStatus.Pending)
            .OrderBy(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmployeeBankDetails
            .CountAsync(b => b.Status == BankDetailStatus.Pending, cancellationToken);
    }

    public async Task<Result> ReviewRequestAsync(ReviewBankDetailModel model, Guid reviewerUserId, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.EmployeeBankDetails
            .FirstOrDefaultAsync(b => b.EmployeeBankDetailId == model.EmployeeBankDetailId, cancellationToken);

        if (request == null)
        {
            return Result.Failure(new Error("BankDetail.NotFound", "Bank detail request not found."));
        }

        if (request.Status != BankDetailStatus.Pending)
        {
            return Result.Failure(new Error("BankDetail.NotPending", "This bank detail request has already been reviewed."));
        }

        DateTime now = DateTime.UtcNow;

        if (model.IsApproved)
        {
            var existingActives = await _dbContext.EmployeeBankDetails
                .Where(b => b.EmployeeId == request.EmployeeId && b.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var active in existingActives)
            {
                active.IsActive = false;
                active.UpdatedAtUtc = now;
                active.UpdatedByUserId = reviewerUserId;
            }

            request.Status = BankDetailStatus.Approved;
            request.IsActive = true;
        }
        else
        {
            request.Status = BankDetailStatus.Rejected;
            request.IsActive = false;
        }

        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = now;
        request.ReviewRemarks = model.ReviewRemarks?.Trim();
        request.UpdatedAtUtc = now;
        request.UpdatedByUserId = reviewerUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static BankDetailDto MapToDto(EmployeeBankDetail b)
    {
        var emp = b.Employee;
        return new BankDetailDto(
            b.EmployeeBankDetailId,
            b.EmployeeId,
            emp?.EmployeeCode ?? string.Empty,
            emp?.FullName ?? string.Empty,
            emp?.Department?.Name ?? string.Empty,
            emp?.Designation?.Title ?? string.Empty,
            b.BankName,
            b.AccountHolderName,
            b.AccountNumber,
            b.BranchName,
            b.RoutingNumber,
            b.SwiftCode,
            b.Status,
            b.IsActive,
            b.CreatedAtUtc,
            b.ReviewedAtUtc,
            b.ReviewRemarks
        );
    }
}
