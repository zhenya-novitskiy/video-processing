using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Itenso.TimePeriod;
using MediaInfo;
using test3.Extensions;
using test3.Models;

namespace test3.Services
{
    public class VideoFpsAligner
    {
        private readonly string _ffmpegPath;
        private readonly int _targetFps;
        private int _totalFilesToProcess;
        private int _filesProcessed;
        private float currentFps = 0;
        private DataManager _dataManager;

        public VideoFpsAligner()
        {
            _ffmpegPath = Program.Configuration.FfmpegLocation;
            _targetFps = Program.Configuration.TargetFpsCount;
            _dataManager = new DataManager();
        }

        public void AlignFps(DayData day)
        {
           

            foreach (var camera in day.CameraDayData)
            {
                _totalFilesToProcess = camera.VideoFragments.Count(x => x.IsValid());
                _filesProcessed = camera.VideoFragments.Count(x => x.IsValid() && x.IsFpsAligned);

                if (camera.VideoFragments.Where(x=>x.IsValid() && x.Type != VideoFragmentType.Black).All(x=>x.IsFpsAligned))
                {
                    OutputManager.DisplayChangeFPSSkip(camera.Name);
                }
                else
                {
                    DateTime prevEnd = DateTime.MinValue;

                    foreach (var fragment in camera.VideoFragments.Where(x => x.IsValid() && !x.IsFpsAligned))
                    {
                        ProcessFile(day.FilesPath, camera.Name, fragment.FilePath);

                        fragment.IsFpsAligned = true;
                        
                        if (fragment.Start < prevEnd)
                        {
                            fragment.Start = prevEnd;
                        }

                        var metadata = new MediaInfoWrapper(fragment.FilePath);

                        fragment.End = fragment.Start.AddMilliseconds(metadata.Duration);
                        prevEnd = fragment.End;

                        _filesProcessed++;

                        OutputManager.DisplayChangeFPS(camera.Name, _totalFilesToProcess, _filesProcessed, currentFps);

                        _dataManager.UpdateMetadata(day.FilesPath, camera.Name, camera.VideoFragments);
                    }

                    OutputManager.NextLine();

                    OutputManager.DisplayAdditionalInfo(camera.Name, $"Motion time: {camera.VideoFragments.ValidCameraVideos().Duration().ToString(@"hh\:mm\:ss")}");

                    var result = new TimePeriodIntersector<TimeRange>().IntersectPeriods(new TimePeriodCollection(camera.VideoFragments.ValidCameraVideos())).ToList();
                }
            }
        }
        
        private void ProcessFile(string path, string cameraName, string output)
        {
            var processedFileName = Path.GetFileName(output).Replace(".mp4", string.Empty) + "_processed.mp4";
            var processedPath = Path.Combine(Path.GetDirectoryName(output), "artifacts", "temp", processedFileName);

            processedPath.CheckDirectory();

            var args = $"-y -i {output} -filter:v fps=fps={_targetFps} -vf scale=2304:1296 -an -preset ultrafast -video_track_timescale 150000 -map_metadata 0 {processedPath}";

            var pci = new ProcessStartInfo(Path.Combine(_ffmpegPath, "ffmpeg.exe"), args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            var fileValidationProcess = new Process();

            fileValidationProcess.StartInfo = pci;
            fileValidationProcess.ErrorDataReceived += new DataReceivedEventHandler(NetErrorDataHandler);

            fileValidationProcess.Start();
            fileValidationProcess.BeginOutputReadLine();
            fileValidationProcess.BeginErrorReadLine();
            fileValidationProcess.WaitForExit();

            fileValidationProcess.ErrorDataReceived -= new DataReceivedEventHandler(NetErrorDataHandler);

            fileValidationProcess.Close();

            if (File.Exists(processedPath))
            {
                File.Delete(output);
                File.Move(processedPath, output);
            }
            else
            {
                var dir = Path.GetDirectoryName(output);

                if (!Directory.Exists(Path.Combine(dir, "artifacts", "corrupted"))) Directory.CreateDirectory(Path.Combine(dir, "artifacts", "corrupted"));

                File.Move(output, $"{ Path.Combine(path, "artifacts", "corrupted", Path.GetFileName(output).Replace(".mp4", string.Empty) + $"_fps.mp4")}");
            }
        }

        void NetErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            if (errLine.Data != null)
            {
                if (errLine.Data.Contains("frame="))
                {
                    var index = errLine.Data.IndexOf("q=");
                    var str = errLine.Data.Remove(index);

                    index = str.IndexOf("fps=");
                    str = str.Remove(0, index).Replace("fps=", string.Empty);
                    float.TryParse(str.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float fps);

                    if (fps != 0)
                    {
                        currentFps = fps;
                    }
                }
            }
        }
    }
}
