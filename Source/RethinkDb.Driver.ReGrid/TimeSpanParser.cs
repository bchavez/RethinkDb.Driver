using System;
using System.Globalization;

namespace RethinkDb.Driver.ReGrid
{
    internal static class TimeSpanParser
    {
        // methods
        public static string ToString(TimeSpan value)
        {
            const int msInOneSecond = 1000;
            const int msInOneMinute = 60 * msInOneSecond;
            const int msInOneHour = 60 * msInOneMinute;

            var ms = (long)value.TotalMilliseconds;
            if( (ms % msInOneHour) == 0 )
            {
                return string.Format("{0}h", ms / msInOneHour);
            }
            else if( (ms % msInOneMinute) == 0 && ms < msInOneHour )
            {
                return string.Format("{0}m", ms / msInOneMinute);
            }
            else if( (ms % msInOneSecond) == 0 && ms < msInOneMinute )
            {
                return string.Format("{0}s", ms / msInOneSecond);
            }
            else if( ms < 1000 )
            {
                return string.Format("{0}ms", ms);
            }
            else
            {
                return value.ToString();
            }
        }

        public static bool TryParse(string value, out TimeSpan result)
        {
            if( !string.IsNullOrEmpty(value) )
            {
                value = value.ToLowerInvariant();
                var end = value.Length - 1;

                var multiplier = 1000; // default units are seconds
                if( value[end] == 's' )
                {
                    if( value[end - 1] == 'm' )
                    {
                        value = value.Substring(0, value.Length - 2);
                        multiplier = 1;
                    }
                    else
                    {
                        value = value.Substring(0, value.Length - 1);
                        multiplier = 1000;
                    }
                }
                else if( value[end] == 'm' )
                {
                    value = value.Substring(0, value.Length - 1);
                    multiplier = 60 * 1000;
                }
                else if( value[end] == 'h' )
                {
                    value = value.Substring(0, value.Length - 1);
                    multiplier = 60 * 60 * 1000;
                }
                else if( value.IndexOf(':') != -1 )
                {
                    return TimeSpan.TryParse(value, out result);
                }

                double multiplicand;
                var numberStyles = NumberStyles.None;
                if( double.TryParse(value, numberStyles, CultureInfo.InvariantCulture, out multiplicand) )
                {
                    result = TimeSpan.FromMilliseconds(multiplicand * multiplier);
                    return true;
                }
            }

            result = default(TimeSpan);
            return false;
        }
    }
}