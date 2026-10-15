using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Employees.Domain.Services;

public static class EmployeeRuleValidator
{
    public const int MaxPhotoSizeBytes = 1024 * 1024; // 1 MB (1,048,576 bytes)
    public static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png" };

    private static readonly byte[] JpegHeader = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static Result ValidateExitDate(EmployeeStatus status, DateOnly joinDate, DateOnly? exitDate)
    {
        if (status is EmployeeStatus.Resigned or EmployeeStatus.Terminated)
        {
            if (!exitDate.HasValue)
            {
                return Result.Failure("Exit date is required when employee is Resigned or Terminated.");
            }

            if (exitDate.Value < joinDate)
            {
                return Result.Failure("Exit date cannot be before join date.");
            }
        }
        else
        {
            if (exitDate.HasValue)
            {
                return Result.Failure("Exit date must be empty when employee status is Active or OnNotice.");
            }
        }

        return Result.Success();
    }

    public static Result ValidateHierarchy(
        Department? department,
        Designation? designation,
        Grade? grade)
    {
        if (department == null)
        {
            return Result.Failure("Selected department does not exist.");
        }

        if (department.Status != EntityStatus.Active)
        {
            return Result.Failure("Selected department is inactive.");
        }

        if (designation == null)
        {
            return Result.Failure("Selected designation does not exist.");
        }

        if (designation.Status != EntityStatus.Active)
        {
            return Result.Failure("Selected designation is inactive.");
        }

        if (designation.DepartmentId != department.DepartmentId)
        {
            return Result.Failure("The selected designation does not belong to the selected department.");
        }

        if (grade == null)
        {
            return Result.Failure("Selected grade does not exist.");
        }

        if (grade.Status != EntityStatus.Active)
        {
            return Result.Failure("Selected grade is inactive.");
        }

        if (grade.DesignationId != designation.DesignationId)
        {
            return Result.Failure("The selected grade does not belong to the selected designation.");
        }

        return Result.Success();
    }

    public static Result ValidatePhoto(string fileName, long length, byte[] headerBytes)
    {
        if (length > MaxPhotoSizeBytes)
        {
            return Result.Failure($"Photo file size exceeds the maximum limit of 1 MB ({MaxPhotoSizeBytes} bytes).");
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(ext))
        {
            return Result.Failure("Invalid file type. Only JPG, JPEG, and PNG images are allowed.");
        }

        if (ext is ".jpg" or ".jpeg")
        {
            if (headerBytes.Length < JpegHeader.Length ||
                headerBytes[0] != JpegHeader[0] ||
                headerBytes[1] != JpegHeader[1] ||
                headerBytes[2] != JpegHeader[2])
            {
                return Result.Failure("Invalid file signature for JPEG image.");
            }
        }
        else if (ext is ".png")
        {
            if (headerBytes.Length < PngHeader.Length)
            {
                return Result.Failure("Invalid file signature for PNG image.");
            }

            for (int i = 0; i < PngHeader.Length; i++)
            {
                if (headerBytes[i] != PngHeader[i])
                {
                    return Result.Failure("Invalid file signature for PNG image.");
                }
            }
        }

        return Result.Success();
    }
}
