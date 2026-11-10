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
    /// <summary>
    /// Calculates the attendance status for a given work date.
    /// </summary>
    /// <param name="isWorkdayComplete">
    /// True when the workday is in the past or the check-out has been recorded.
    /// False when it is today and the employee has not yet checked out — in this
    /// case the HalfDay penalty is NOT applied (we cannot penalise an ongoing shift).
    /// </param>
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
        TimeZoneInfo timeZone,
        bool isWorkdayComplete = true)
    {
        // Rule 1: Holiday or weekly-off day → Holiday or Weekend.
        // If both apply, attendance.holiday_priority decides which one wins.
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

        // Rule 2: An approved leave covers this day → OnLeave
        if (isOnLeave)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.OnLeave,
                LateMinutes: 0,
                WorkedMinutes: 0,
                ShiftVersionId: null
            );
        }

        // Rule 3: No check-in → Absent
        if (!checkInUtc.HasValue)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.Absent,
                LateMinutes: 0,
                WorkedMinutes: 0,
                ShiftVersionId: candidateShiftVersionId
            );
        }

        // Calculate WorkedMinutes = check-out minus check-in minus break minutes.
        // If the employee has NOT yet checked out and the workday is still in progress,
        // we leave workedMinutes at 0 but do NOT apply the HalfDay penalty below.
        int workedMinutes = 0;
        if (checkOutUtc.HasValue && checkOutUtc.Value >= checkInUtc.Value)
        {
            int totalElapsed = (int)(checkOutUtc.Value - checkInUtc.Value).TotalMinutes;
            workedMinutes = Math.Max(0, totalElapsed - breakMinutes);
        }

        // Calculate LateMinutes = minutes after (shift start + grace period)
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

        // Rule 4: HalfDay — only apply when the workday is complete (past date, or checkout recorded).
        // If today's shift is still in progress and no checkout yet, skip the HalfDay penalty:
        // the employee may still work the full remaining hours.
        if (isWorkdayComplete)
        {
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
        }

        // Rule 5: Check-in later than shift start + grace → Late
        if (lateMinutes > 0)
        {
            return new AttendanceCalculationResult(
                Status: AttendanceStatus.Late,
                LateMinutes: lateMinutes,
                WorkedMinutes: workedMinutes,
                ShiftVersionId: candidateShiftVersionId
            );
        }

        // Rule 6: Otherwise → Present
        return new AttendanceCalculationResult(
            Status: AttendanceStatus.Present,
            LateMinutes: lateMinutes,
            WorkedMinutes: workedMinutes,
            ShiftVersionId: candidateShiftVersionId
        );
    }
}
