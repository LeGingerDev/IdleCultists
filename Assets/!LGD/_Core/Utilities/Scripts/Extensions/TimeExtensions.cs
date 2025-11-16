using System;

namespace LGD.Utilities.Extensions
{
    public static class TimeExtensions
{
    public static string ToTimeString(this float time)
    {
        return TimeSpan.FromSeconds(time).ToString(@"mm\:ss");
    }
}
}