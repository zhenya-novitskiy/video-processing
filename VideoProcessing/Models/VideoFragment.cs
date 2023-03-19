using System.Text;
using Itenso.TimePeriod;

namespace test3.Models
{
    public class VideoFragment : TimeRange
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public int TotalFrames { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public double DurationMetadata { get; set; }

        public double DurationFfmpeg { get; set; }

        public string Tbn { get; set; }

        public double Fps { get; set; }

        public bool IsFpsAligned { get; set; }

        public ErrorData Error { get; set; }

        public VideoFragmentType Type { get; set; }

        public bool IsValid()
        {
            return Error == null;
        }

        public override string ToString()
        {
            var isValid = IsValid() ? "Y" : "N";

            var fb = new StringBuilder();
            fb.Append($" {isValid} |");
            fb.Append($"{Type} \t|");
            fb.Append($"{Start.Ticks} \t|");
            fb.Append($"{End.Ticks} \t|");
            fb.Append($" {FilePath} |");
            fb.Append($" {FileName} |");
            fb.Append($"{TotalFrames} \t|");
            fb.Append($"{Width} \t|");
            fb.Append($"{Height} \t|");
            fb.Append($"{DurationMetadata} \t|");
            fb.Append($"{DurationFfmpeg} \t|");
            fb.Append($"{Tbn} \t|");
            fb.Append($" {Fps} \t|");
            fb.Append($" {IsFpsAligned} |");

            fb.Append($" {Start.ToString(@"HH\:mm\:ss")} |");
            fb.Append($" {End.ToString(@"HH\:mm\:ss")} |");


            if (Error != null)
            {
                fb.Append($"{Error.ErrorType} \t | ");
                fb.Append($"{Error.Data} \t|");
            }

            
            return fb.ToString();
        }

        public static string GetHeaderString()
        {
            var headerb = new StringBuilder();
            headerb.Append("IsValid \t|");
            headerb.Append("Type \t|");
            headerb.Append("Start \t|");
            headerb.Append("End \t|");
            headerb.Append("Path \t|");
            headerb.Append("Name \t|");
            headerb.Append("TotalFrames \t|");
            headerb.Append("Width \t|");
            headerb.Append("Height \t|");
            headerb.Append("DurationMetadata \t|");
            headerb.Append("DurationFfmpeg \t|");
            headerb.Append("Tbn \t|");
            headerb.Append("Fps \t|");
            headerb.Append("IsFpsAligned \t|");

            headerb.Append("Error Type \t|");
            headerb.Append("Error Data \t|");

            return headerb.ToString();
        }
    }
}
