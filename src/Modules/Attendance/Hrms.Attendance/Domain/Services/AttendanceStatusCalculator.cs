using Hrms.Attendance.Domain.Enums;

namespace Hrms.Attendance.Domain.Services;

public record AttendanceCalculationResult(
    AttendanceStatus Status,
    int LateMinutes,
    int WorkedMinutes,
    Guid? ShiftVersionId
);

public static class AttendanceStatusCalculator
{
    public static AttendanceCalculationResult Calculate(
        DateOnly workDate,
        bool isHoliday,
        bool isWeeklyOff,
        string? holidayPriority,
        bool isOnLeave,
        DateTime? checkInUtc,
        DateTime? checkOutUtc,
        Guid? candidateShiftVersionId,
        TimeSpan? shiftStartTime,
        int graceMinutes,
        int breakMinutes,
        decimal halfDayHours,
        TimeZoneInfo timeZone)
    {
        // Rule 1: The date is a holiday or a weekly off day -> Holiday or Weekend.
        // If a holiday falls on a weekly off day, setting attendance.holiday_priority decides which one.
        if (isHoliday || isWeeklyOff)
        {
            AttendanceStatus holidayOrWeekendStatus;
            if (isHoliday && isWeeklyOff)
            {
                bool preferWeekend = string.Equals(holidayPriority, "Weekend", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(holidayPriority, "WeekendOverHoliday", StringComparison.OrdinalIgnoreCase);
                holidayOrWeekendStatus = preferWeekend ? AttendanceStatus.Weekend : AttendanceStatus.Holiday;
            }
            else if (isHoliday)
            {
                holidayOrWeekendStatus = AttendanceStatus.Holiday;
            }
            else
            {
                holidayOrWeekendStatus = AttendanceStatus.Weekend;
            }

            // ShiftVersionId is NULL on weekends, holidays and leave days
            return new AttendanceCalculationResult(
                Status: holidayOrWeekendStatus,
                LateMinutes: 0,
                WorkedMinutes: 0,
                ShiftVersionId: null
            );
        }

        // Rule 2: A Recorded leave covers the day -> OnLeave
        if (isOnLeave)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.OnLeave,
                LateMinutes: 0,
                WorkedMinutes: 0,
                ShiftVersionId: null
            );
        }

        // Rule 3: No check-in -> Absent
        if (!checkInUtc.HasValue)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.Absent,
                LateMinutes: 0,
                WorkedMinutes: 0,
                ShiftVersionId: candidateShiftVersionId
            );
        }

        // Calculate WorkedMinutes = check-out minus check-in minus break minutes
        int workedMinutes = 0;
        if (checkOutUtc.HasValue && checkOutUtc.Value >= checkInUtc.Value)
        {
            int totalElapsed = (int)(checkOutUtc.Value - checkInUtc.Value).TotalMinutes;
            workedMinutes = Math.Max(0, totalElapsed - breakMinutes);
        }

        // Calculate LateMinutes = minutes after (shift start + grace)
        int lateMinutes = 0;
        if (shiftStartTime.HasValue)
        {
            var checkInLocal = TimeZoneInfo.ConvertTimeFromUtc(checkInUtc.Value, timeZone);
            var shiftStartLocal = workDate.ToDateTime(TimeOnly.FromTimeSpan(shiftStartTime.Value));
            var graceCutoffLocal = shiftStartLocal.AddMinutes(graceMinutes);

            if (checkInLocal > graceCutoffLocal)
            {
                lateMinutes = (int)Math.Max(0, (checkInLocal - graceCutoffLocal).TotalMinutes);
            }
        }

        // Rule 4: Worked fewer hours than attendance.half_day_hours -> HalfDay
        int halfDayMinutesThreshold = (int)Math.Round(halfDayHours * 60m, MidpointRounding.AwayFromZero);
        if (workedMinutes < halfDayMinutesThreshold)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.HalfDay,
                LateMinutes: lateMinutes,
                WorkedMinutes: workedMinutes,
                ShiftVersionId: candidateShiftVersionId
            );
        }

        // Rule 5: Check-in later than shift start plus grace minutes -> Late
        if (lateMinutes > 0)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.Late,
                LateMinutes: lateMinutes,
                WorkedMinutes: workedMinutes,
                ShiftVersionId: candidateShiftVersionId
            );
        }

        // Rule 6: Otherwise -> Present
        return new AttendanceCalculationResult(
            Status: AttendanceStatus.Present,
            LateMinutes: lateMinutes,
            WorkedMinutes: workedMinutes,
            ShiftVersionId: candidateShiftVersionId
        );
    }
}
