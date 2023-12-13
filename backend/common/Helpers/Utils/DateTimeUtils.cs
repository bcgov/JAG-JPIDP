namespace Common.Helpers.Utils;
using System;


public static class DateTimeUtils
{
    public static DateTime ChangeTime(this DateTime dateTime, int hours, int minutes, int seconds, int milliseconds)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            hours,
            minutes,
            seconds,
            milliseconds,
            dateTime.Kind);
    }
}
