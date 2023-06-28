using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaInfo;
using test3.Models;

namespace test3.Services
{
    public class VideoGrouper
    {
        private readonly string _ffmpegPath;
        private readonly bool _displayProgress;
        private int totalFilesToJoin = 0;
        private int currentProgress = 0;
        private float currentFps = 0;
        private string _dayName;
        private string _cameraName;

        private DataManager _dataManager;

        public VideoGrouper(bool displayProgress = true)
        {
            _ffmpegPath = Program.Configuration.FfmpegLocation;
            _displayProgress = displayProgress;
            _dataManager = new DataManager();
        }

        public DayData Group(DayData day)
        {
            _dayName = day.Name;

            var inputFiles = day.CameraDayData.Select(x => Path.Combine(day.FilesPath, "artifacts", $"{x.Name}.mp4")).ToList();

            var outputFile = Path.Combine(Program.Configuration.StorageLocation, $"{day.Name}.mp4");

            if (inputFiles.Count == 3)
            {
                if (!File.Exists(outputFile))
                {
                    Group(inputFiles[0], inputFiles[2], inputFiles[1], outputFile);
                }
            }

            if (inputFiles.Count == 2)
            {
                if (!File.Exists(outputFile))
                {
                    Group(inputFiles[0], inputFiles[1], outputFile);
                }
            }


            return day;
        }
        
        public void Group(string filePath1, string filePath2, string filePath3, string output)
        {
            var metadata = new MediaInfoWrapper(filePath1);
            OutputManager.AddText($"Duration 1 {metadata.Duration} {filePath1}", false);
            metadata = new MediaInfoWrapper(filePath2);
            OutputManager.AddText($"Duration 2 {metadata.Duration} {filePath2}", false);
            metadata = new MediaInfoWrapper(filePath3);
            OutputManager.AddText($"Duration 3 {metadata.Duration} {filePath3}", false);

            var args = $"-y -i {filePath1} -i {filePath2} -i {filePath3}  -filter_complex \"[0:v]setpts=0.1 * PTS,  scale=960:540[a0]; [1:v] setpts=0.1 * PTS, scale=960:540[a1]; [2:v]setpts=0.1 * PTS,  scale=960:540[a2]; [a0][a1][a2] xstack=inputs=3:layout=0_0|0_h0|w0_h0:fill=black[v]\" -map \"[v]\" -an -preset ultrafast {output}";
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

            if (_displayProgress)
            {
                OutputManager.NextLine();
            }

            File.Delete(filePath1);
            File.Delete(filePath2);
            File.Delete(filePath3);
        }

        public void Group(string filePath1, string filePath2, string output)
        {
            var metadata = new MediaInfoWrapper(filePath1);
            OutputManager.AddText($"Duration 1 {metadata.Duration} {filePath1}", false);
            metadata = new MediaInfoWrapper(filePath2);
            OutputManager.AddText($"Duration 2 {metadata.Duration} {filePath2}", false);

            var args = $"-y -i {filePath1} -i {filePath2}   -filter_complex \"[0:v]setpts=0.1 * PTS,  scale=960:540[a0]; [1:v] setpts=0.1 * PTS, scale=960:540[a1]; [a0][a1] vstack=inputs=2\"  -an -preset ultrafast {output}";
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

            if (_displayProgress)
            {
                OutputManager.NextLine();
            }

            File.Delete(filePath1);
            File.Delete(filePath2);
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

                    if (currentFps != 0)
                    {

                        if (_displayProgress)
                        {
                            OutputManager.DisplayJoinFile(_cameraName, totalFilesToJoin, currentProgress);
                        }
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
