using System.Collections.Generic;

namespace test3.Models
{
    public class Configuration
    {
        public string FfmpegLocation { get; set; }
        public string StorageLocation { get; set; }
        public string TessdataLocation { get; set; }
        public string TessdataLang { get; set; }

        public List<string> Cameras { get; set; }

        public int TargetFpsCount { get; set; }

        public int CheckDaysCount { get; set; }

        public int UploadDaysCount { get; set; }

        public string HaFtpHost { get; set; }
        public string HaFtpUser { get; set; }
        public string HaFtpPassword { get; set; }
    }
}
