using System.Collections.Generic;

namespace test3.Models
{
    public class DayData
    {
        public string Name { get; set; }

        public string FilesPath { get; set; }

        public List<CameraDayData> CameraDayData { get; set; }
    }
}
