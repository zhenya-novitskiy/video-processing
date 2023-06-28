using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using test3.Models;

namespace test3.Services
{
    public class VideoJoiner
    {
        private readonly string _ffmpegPath;
        private readonly bool _displayProgress;
        private int totalFilesToJoin = 0;
        private int currentProgress = 0;
        private float currentFps = 0;
        private string _dayName;
        private string _cameraName;

        private DataManager _dataManager;

        public VideoJoiner(bool displayProgress = true)
        {
            _ffmpegPath = Program.Configuration.FfmpegLocation;
            _displayProgress = displayProgress;
            _dataManager = new DataManager();
        }

        public DayData Join(DayData day)
        {
            _dayName = day.Name;

            foreach (var camera in day.CameraDayData)
            {
                _cameraName = camera.Name;

                var resultFilePath = Path.Combine(day.FilesPath, "artifacts", $"{camera.Name}.mp4");

                if (!File.Exists(resultFilePath))
                {
                    var files = camera.VideoFragments.Where(x => x.Type != VideoFragmentType.Corrupted)
                        .OrderBy(x => x.Start)
                        .Select(x => x.FilePath)
                        .ToList();

                    Join(files, resultFilePath);
                    OutputManager.NextLine();
                }
                else
                {
                    OutputManager.DisplayJoinFileSkip(_cameraName);
                }

               
            }

            return day;
        }

        public void Join(IList<string> parts, string resultPath)
        {
            totalFilesToJoin = parts.Count();
            currentProgress = 0;

            var root = Path.GetDirectoryName(parts.First());

            _dataManager.WriteFileForConcat(root, parts);

            var args = @"-y -f concat -safe 0 -i " + Path.Combine(root, "files.txt") + " -c copy -an " + resultPath;
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
            fileValidationProcess.OutputDataReceived += new DataReceivedEventHandler(NetErrorDataHandler);
            fileValidationProcess.ErrorDataReceived += new DataReceivedEventHandler(NetErrorDataHandler);

            fileValidationProcess.Start();
            fileValidationProcess.BeginOutputReadLine();
            fileValidationProcess.BeginErrorReadLine();
            fileValidationProcess.WaitForExit();
            
            fileValidationProcess.OutputDataReceived -= new DataReceivedEventHandler(NetErrorDataHandler);

            fileValidationProcess.Close();
            
            _dataManager.DeleteFileForConcat(root);
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
                    float.TryParse(str.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out currentFps);

                    if (_displayProgress)
                    {
                        OutputManager.DisplayJoinFile(_cameraName, totalFilesToJoin, currentProgress);
                    }
                }

                if (errLine.Data.StartsWith("[mov,mp4,m4a,3gp,3g2,mj2"))
                {
                    currentProgress++;
                    if (currentProgress > totalFilesToJoin) currentProgress = totalFilesToJoin;
                }
            }
        }
    }
}
