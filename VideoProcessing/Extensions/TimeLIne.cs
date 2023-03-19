using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itenso.TimePeriod;
using test3.Models;

namespace test3.Extensions
{
    public static class Extensions
    {
        public static ITimePeriodCollection GetNoMotionAreas(ITimePeriodContainer periods, ITimePeriod limits = null)
        {
            var gaps = new TimeGapCalculator<TimeRange>(new TimeCalendar()).GetGaps(periods, limits);

            var periodCollection = new List<ITimePeriod>();

            foreach (ITimePeriod timePeriod in gaps)
            {
                periodCollection.Add(new TimeRange(timePeriod.Start.AddTicks(-1), timePeriod.End));
            }

            return new TimePeriodCollection(periodCollection);
        }

        public static void CheckDirectory(this string path)
        {
            var isDir = Path.GetExtension(path) == string.Empty;

            string dir = string.Empty;

            if (!isDir)
            {
                dir = Path.GetDirectoryName(path);
            }
            else
            {
                dir = path;
            }
            
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static List<VideoFragment> ValidCameraVideos(this List<VideoFragment> data)
        {
            return data.Where(x => x.IsValid() && x.Type == VideoFragmentType.OriginalFile).OrderBy(x=>x.Start).ToList();
        }

        public static List<VideoFragment> AllValidVideos(this List<VideoFragment> data)
        {
            return data.Where(x => x.IsValid()).OrderBy(x => x.Start).ToList();
        }

        public static TimeSpan Duration(this List<VideoFragment> data)
        {
            return TimeSpan.FromSeconds(data.Sum(x => x.Duration.TotalSeconds));
        }
    }
}
