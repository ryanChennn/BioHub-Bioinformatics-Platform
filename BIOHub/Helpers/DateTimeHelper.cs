namespace BIOHub.Helpers
{
    public class DateTimeHelper
    {
        public static DateTime ToTaiwanTime(DateTime utcTime)
        {
            var taiwanZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Taipei");

            var specifiedUtc = DateTime.SpecifyKind(
                utcTime,
                DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(
                specifiedUtc,
                taiwanZone);
        }

        public static DateTime? ToTaiwanTime(DateTime? utcTime)
        {
            return utcTime.HasValue
                ? ToTaiwanTime(utcTime.Value)
                : null;
        }
    }
}
