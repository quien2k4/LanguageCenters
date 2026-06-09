using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCenter.Models;

namespace LanguageCenter.Helpers
{
    public class ClassProgressResult
    {
        public int CurrentSession { get; set; }
        public int? TotalSessions { get; set; }
        public string DisplayText { get; set; }
        public int Percent { get; set; }
        public bool IsCompleted { get; set; }
    }

    public static class ClassProgressHelper
    {
        public static ClassProgressResult GetClassProgress(DateTime? startDate, int? totalSessions, IEnumerable<CLASS_SCHEDULE> schedules)
        {
            var result = new ClassProgressResult
            {
                CurrentSession = 0,
                TotalSessions = totalSessions,
                DisplayText = "Chưa bắt đầu",
                Percent = 0,
                IsCompleted = false
            };

            if (!startDate.HasValue)
            {
                result.DisplayText = "Chưa có ngày bắt đầu";
                return result;
            }

            var scheduleList = schedules == null ? new List<CLASS_SCHEDULE>() : schedules.ToList();
            if (!scheduleList.Any())
            {
                result.DisplayText = "Chưa có lịch học";
                return result;
            }

            if (!totalSessions.HasValue || totalSessions.Value <= 0)
            {
                result.DisplayText = "Chưa cấu hình số buổi";
                return result;
            }

            var start = startDate.Value.Date;
            var today = DateTime.Today;
            if (today < start)
            {
                result.DisplayText = "Chưa bắt đầu";
                return result;
            }

            var studyDays = scheduleList
                .Select(x => ParseDayOfWeek(x.DayOfWeek))
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            if (!studyDays.Any())
            {
                result.DisplayText = "Chưa có lịch học";
                return result;
            }

            var currentSession = CountStudyDays(start, today, studyDays);
            if (currentSession <= 0)
            {
                result.DisplayText = "Chưa bắt đầu";
                return result;
            }

            var total = totalSessions.Value;
            if (currentSession >= total)
            {
                result.CurrentSession = total;
                result.DisplayText = "Hoàn thành " + total + "/" + total;
                result.Percent = 100;
                result.IsCompleted = true;
                return result;
            }

            result.CurrentSession = currentSession;
            result.DisplayText = "Buổi " + currentSession + "/" + total;
            result.Percent = Math.Max(0, Math.Min(100, (int)Math.Round(currentSession * 100.0 / total)));
            return result;
        }

        private static int CountStudyDays(DateTime start, DateTime today, List<DayOfWeek> studyDays)
        {
            var count = 0;
            for (var date = start; date <= today; date = date.AddDays(1))
            {
                if (studyDays.Contains(date.DayOfWeek))
                {
                    count++;
                }
            }

            return count;
        }

        private static DayOfWeek? ParseDayOfWeek(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "monday":
                    return DayOfWeek.Monday;
                case "tuesday":
                    return DayOfWeek.Tuesday;
                case "wednesday":
                    return DayOfWeek.Wednesday;
                case "thursday":
                    return DayOfWeek.Thursday;
                case "friday":
                    return DayOfWeek.Friday;
                case "saturday":
                    return DayOfWeek.Saturday;
                case "sunday":
                    return DayOfWeek.Sunday;
                default:
                    return null;
            }
        }
    }
}
