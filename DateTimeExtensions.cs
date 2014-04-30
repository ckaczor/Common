using System;

namespace Common
{
    public static class DateTimeExtensions
    {
        public static DateTime FromUnixTime(double unixTime)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTime).ToLocalTime();
            return dtDateTime;
        }
    }
}
