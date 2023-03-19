using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace test3.Services
{
    public class VideoGenerator
    {
        private readonly string _ffmpegPath;
        private readonly string _prerendersPath;
        private readonly List<double> _availableSteps = new List<double>
        {
        };

        private readonly VideoJoiner _videoJoiner;

        public VideoGenerator()
        {
            _ffmpegPath = Program.Configuration.FfmpegLocation;
            _videoJoiner = new VideoJoiner( false);

            _prerendersPath = Path.Combine(Program.Configuration.StorageLocation, "AppData", "Prerenders");

            if (!Directory.Exists(_prerendersPath))
            {
                Directory.CreateDirectory(_prerendersPath);
            }

            var targetSteps = new List<double>
            {
                3600,
                1800,
                900,
                600,
                300,
                60,
                30,
                10,
                5,
                1
            };

            foreach (double step in targetSteps.OrderBy(x => x))
            {
                var filePath = Path.Combine(_prerendersPath, $"{step}.mp4");

                if (!File.Exists(filePath))
                {
                    if (step == 1.0f)
                    {
                        GenerateNoMotionFile(filePath, TimeSpan.FromSeconds(step));
                    }
                    else
                    {
                        GenerateNoMotionVideo(filePath, TimeSpan.FromSeconds(step));
                    }
                }

                _availableSteps.Add(step);

                _availableSteps = _availableSteps.OrderByDescending(x => x).ToList();
            }
        }

        public string GenerateNoMotionVideo(string filePath, TimeSpan duration)
        {
            var originalTime = duration.TotalSeconds;
            var secondsLeft = duration.TotalSeconds;
            var parts = new List<string>();

            var index = 0;
            do
            {
                if (secondsLeft - _availableSteps[index] >= 0)
                {
                    parts.Add(Path.Combine(_prerendersPath, $"{_availableSteps[index]}.mp4"));
                    secondsLeft -= _availableSteps[index];
                }
                else
                {
                    index++;
                    if (index >= _availableSteps.Count)
                    {
                        break;
                    }
                }
            } while (true);

            if (secondsLeft > 0)
            {
                if (!parts.Any())
                {
                    return GenerateNoMotionFile(filePath, TimeSpan.FromSeconds(secondsLeft));
                }
                else
                {
                    var temp = GenerateNoMotionFile(Path.Combine(_prerendersPath, "temp.mp4"), TimeSpan.FromSeconds(secondsLeft));
                    if (temp != null) parts.Add(temp);
                }
            }

            _videoJoiner.Join(parts, filePath);

            return filePath;

        }

        private string GenerateNoMotionFile(string filePath, TimeSpan duration)
        {
            var seconds = duration.TotalSeconds - 0.15;

            if (seconds > 0.02)
            {
                var args = $"-t {seconds.ToString(CultureInfo.InvariantCulture)} -y -f lavfi -i color=c=black:s=2304x1296 -c:v libx264 -tune stillimage -pix_fmt yuv420p -r 12 -video_track_timescale 150000 -an {filePath}";
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
            }
            else
            {
                return null;
            }
            return filePath;
        }

        private string GenerateFile(string filePath, double duration)
        {
            var pci = new ProcessStartInfo(Path.Combine(_ffmpegPath, "ffmpeg.exe"), $"-t {duration.ToString(CultureInfo.InvariantCulture)} -f lavfi -i color=c=black:s=2304x1296 -c:v libx264 -tune stillimage -pix_fmt yuv420p -r 12 -video_track_timescale 150000 -an {filePath}")
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
            return filePath;
        }

        void NetErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {
            if (errLine.Data != null)
            {
                //if (errLine.Data.Contains("frame="))
                //{
                //    var index = errLine.Data.IndexOf("fps");

                //    var str = errLine.Data.Remove(index).Replace("frame=", string.Empty);

                //    int.TryParse(str, out currentFrameProgress);

                //    index = errLine.Data.IndexOf("q=");
                //    str = errLine.Data.Remove(index);

                //    index = str.IndexOf("fps=");
                //    str = str.Remove(0, index).Replace("fps=", string.Empty);
                //    float.TryParse(str.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out currentFps);

                //    if (currentFps != 0)
                //    {
                //        var dateEnd = DateTime.Now.AddSeconds((totalFrames - currentFrameProgress) / currentFps);

                //        Console.SetCursorPosition(0, Console.CursorTop);
                //        Console.Write($"| {_cameraName}\t| Concatenate\t| {_timer.Elapsed.ToString(@"hh\:mm\:ss")} | Processed: {(int)(currentFrameProgress / (totalFrames / 100.0))}%\t| FPS: {currentFps}\t| Ends at: {dateEnd.ToString(@"HH\:mm")}\t|");
                //    }
                //}
            }
        }
    }
}
