using System.Globalization;
using FluentValidation;
using Hrms.Settings.Application.Models;

namespace Hrms.Settings.Application.Validators;

public class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters.");

        RuleFor(x => x.CompanyTimezone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Company timezone is required.")
            .Must(BeAValidTimeZone).WithMessage("Company timezone must be a valid time zone ID.");

        RuleFor(x => x.AttendanceHalfDayHours)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Half day hours is required.")
            .Must(BeAValidHalfDayHours).WithMessage("Half day hours must be a number greater than 0 and at most 12.");

        RuleFor(x => x.AttendanceHolidayPriority)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Holiday priority is required.")
            .Must(v => v == "Holiday" || v == "Weekend")
            .WithMessage("Holiday priority must be either 'Holiday' or 'Weekend'.");

        RuleFor(x => x.LeaveYearStart)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Leave year start is required.")
            .Must(BeAValidLeaveYearStart)
            .WithMessage("Leave year start must be a valid date in MM-dd format (e.g. 01-01) and cannot be February 29.");

        RuleFor(x => x.PayrollPerDayDivisor)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Payroll per-day divisor is required.")
            .Must(BeAValidPerDayDivisor).WithMessage("Payroll per-day divisor must be a whole number from 1 to 31.");

        RuleFor(x => x.EmployeeCodeFormat)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Employee code format is required.")
            .Must(BeAValidEmployeeCodeFormat)
            .WithMessage("Employee code format must be a valid .NET format string with exactly one placeholder like {0:0000}, and formatted codes must not exceed 30 characters.");
    }

    private static bool BeAValidTimeZone(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone)) return false;
        var trimmed = timezone.Trim();

        // Explicitly accept Asia/Dhaka per specification
        if (string.Equals(trimmed, "Asia/Dhaka", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(trimmed);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _);
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool BeAValidHalfDayHours(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var hours))
        {
            return false;
        }
        return hours > 0m && hours <= 12m;
    }

    private static bool BeAValidLeaveYearStart(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (trimmed.Length != 5) return false;
        if (trimmed == "02-29") return false;

        return DateTime.TryParseExact(
            trimmed + "-2025",
            "MM-dd-yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    private static bool BeAValidPerDayDivisor(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var divisor))
        {
            return false;
        }
        return divisor >= 1 && divisor <= 31;
    }

    private static bool BeAValidEmployeeCodeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format)) return false;

        int openBrace = 0, closeBrace = 0;
        foreach (char c in format)
        {
            if (c == '{') openBrace++;
            if (c == '}') closeBrace++;
        }
        if (openBrace != 1 || closeBrace != 1) return false;
        if (!format.Contains("{0") || !format.Contains("}")) return false;

        try
        {
            var sample1 = string.Format(CultureInfo.InvariantCulture, format, 1);
            var sample999999 = string.Format(CultureInfo.InvariantCulture, format, 999999);

            if (sample1.Length == 0 || sample1.Length > 30) return false;
            if (sample999999.Length == 0 || sample999999.Length > 30) return false;

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
