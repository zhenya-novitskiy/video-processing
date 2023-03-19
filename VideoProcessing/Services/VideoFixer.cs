using System.Linq;
using test3.Models;

namespace VideoProcessing.Services
{
    public class VideoFixer
    {
        public void FixFiles(DayData data)
        {
            foreach (var cameraDayData in data.CameraDayData)
            {
                foreach (var fragment in cameraDayData.VideoFragments.Where(x=>x.Error is { ErrorType: ErrorType.InvalidFile }))
                {
                    
                }
            }
        }
    }
}
