#region using

using System;

#endregion

namespace Dry.Common.Monorail.Helpers {
    public class DateHelper {
        public static int GetDaysFromNow(DateTime? date) {
            if (!date.HasValue) return 0;
            var now = new TimeSpan(DateTime.Now.Date.Ticks);
            var cur = new TimeSpan(date.Value.Date.Ticks);

            TimeSpan diff = cur.Subtract(now);
            return diff.Days;
        }

        public static int GetMonthsFromDate(DateTime? date, DateTime? from) {
            if (!date.HasValue) return 0;
            if (!from.HasValue) return 0;
            int months = 12 * (from.Value.Year - date.Value.Year) + from.Value.Month - date.Value.Month;
            return Math.Abs(months);
        }

        public static int GetDaysFromDate(DateTime? date, DateTime? from) {
            if (!date.HasValue) return 0;
            if (!from.HasValue) return 0;
            var now = new TimeSpan(from.Value.Date.Ticks);
            var cur = new TimeSpan(date.Value.Date.Ticks);

            TimeSpan diff = cur.Subtract(now);
            return diff.Days;
        }

        public static string FriendlyFormatFromNow(DateTime? date) {
            if (!date.HasValue) return string.Empty;

            var now = new TimeSpan(DateTime.Now.Date.Ticks);
            var cur = new TimeSpan(date.Value.Date.Ticks);

            TimeSpan diff = now.Subtract(cur);

            int days = diff.Days;
            if (days == 0) {
                return "Today";
            }
            else if (days > 0) {
                if (days > 365) {
                    int years = days / 365;
                    return String.Format("{0} year{1} ago", years, years > 1 ? "s" : String.Empty);
                }
                else if (days > 30) {
                    int months = days / 30;
                    return String.Format("{0} month{1} ago", months, months > 1 ? "s" : String.Empty);
                }
                else if (days == 1) {
                    return "Yesterday";
                }
                else {
                    return String.Format("{0} day{1} ago", days, days > 1 ? "s" : String.Empty);
                }
            }
            else {
                days = days * -1;
                if (days > 365) {
                    int years = days / 365;
                    return String.Format("{0} year{1} away", years, years > 1 ? "s" : String.Empty);
                }
                else if (days > 30) {
                    int months = days / 30;
                    return String.Format("{0} month{1} away", months, months > 1 ? "s" : String.Empty);
                }
                else if (days == 1) {
                    return "Tomorrow";
                }
                else {
                    return String.Format("{0} day{1} away", days, days > 1 ? "s" : String.Empty);
                }
            }
        }
    }
}
