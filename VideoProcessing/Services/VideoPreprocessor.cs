using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MediaInfo;
using test3.Models;

namespace test3.Services
{
    internal class VideoPreprocessor
    {
        private readonly string _storagePath;
        private readonly string _ffmpegPath;
        private bool isNotValid = false;
        private int frames;
        private string reason;
        private TimeSpan duration;
        private string tbn;
        private int width;
        private int height;
        private double fps;

        private DataManager _dataManager;

        public VideoPreprocessor()
        {

            _storagePath = Program.Configuration.StorageLocation;
            _ffmpegPath = Program.Configuration.FfmpegLocation;
            _dataManager = new DataManager();
        }

        public DayData PreprocessDay(DateTime day, List<string> cameras)
        {
            var str = day.ToString("yyyy_MM_dd").Split("_");

            var dayLocation = Path.Combine(Program.Configuration.StorageLocation, str[0], str[1], str[2]);

            var result = new DayData()
            {
                Name = day.ToString("dd_MM_yyyy"),
                FilesPath = dayLocation
            };

            result.CameraDayData = new List<CameraDayData>();

            var dir = new DirectoryInfo(result.FilesPath);

            var allVideos = dir.GetFiles().Where(x => x.Extension.EndsWith(".mp4"));

            PrepareCameraDirectory(result.FilesPath);

            foreach (var camera in cameras)
            {
                var files = allVideos.Where(x => x.Name.StartsWith(camera));
                if (!files.Any()) continue;

                var cameraDayData = new CameraDayData();
                cameraDayData.Name = camera;
                cameraDayData.VideoFragments = new List<VideoFragment>();

                var processedCount = 0;

                var metadata = _dataManager.ReadMetadata(dayLocation, camera);

                if (metadata.Where(x => x.Type != VideoFragmentType.Black).Count() != files.Count())
                {
                    _dataManager.CleanupMetadataFile(dayLocation, camera);

                    foreach (FileInfo fileInfo in files)
                    {
                        var totalFilesToProcess = files.Count();


                        var basicInfo = ExtractMetadata(fileInfo);

                        cameraDayData.VideoFragments.Add(basicInfo);
                        _dataManager.AddMetadata(dayLocation, camera, basicInfo);

                        processedCount++;
                        if (processedCount > totalFilesToProcess) processedCount = totalFilesToProcess;

                        ConsoleManager.DisplayPreProcessBasic(camera, totalFilesToProcess, processedCount);
                    }
                    ConsoleManager.NextLine();
                }
                else
                {
                    cameraDayData.VideoFragments = metadata.ToList();
                    ConsoleManager.DisplayPreProcessBasicSkip(camera);
                }


                var corruptedCount = 0;
                processedCount = 0;

                if (cameraDayData.VideoFragments.Where(x => x.Type == VideoFragmentType.OriginalFile).Last().TotalFrames == 0 && cameraDayData.VideoFragments.Last().Error == null)
                {
                    for (int i = 0; i < cameraDayData.VideoFragments.Count; i++)
                    {
                        var totalFilesToProcess = files.Count();

                        cameraDayData.VideoFragments[i] = Validate(cameraDayData.VideoFragments[i]);

                        if (cameraDayData.VideoFragments[i].Error == null)
                        {
                            processedCount++;
                        }
                        else
                        {
                            corruptedCount++;
                        }

                        if (!cameraDayData.VideoFragments[i].IsValid())
                        {
                            var newFilePath = Path.Combine(Path.GetDirectoryName(cameraDayData.VideoFragments[i].FilePath), "artifacts", "corrupted", cameraDayData.VideoFragments[i].FileName);

                            if (!File.Exists(newFilePath))
                            {
                                File.Copy(cameraDayData.VideoFragments[i].FilePath, newFilePath);
                            }
                        }

                        ConsoleManager.DisplayPreProcessValidation(camera, totalFilesToProcess, processedCount + corruptedCount, corruptedCount);
                    }

                    _dataManager.UpdateMetadata(dayLocation, camera, cameraDayData.VideoFragments);

                    ConsoleManager.NextLine();
                }
                else
                {
                    ConsoleManager.DisplayPreProcessValidationSkip(camera, cameraDayData.VideoFragments.Count(x => x.IsValid()), cameraDayData.VideoFragments.Count(x => !x.IsValid()));
                }

                result.CameraDayData.Add(cameraDayData);

            }

            return result;
        }

        public VideoFragment ExtractMetadata(FileInfo fileInfo)
        {
            var result = new VideoFragment();

            var metadata = new MediaInfoWrapper(fileInfo.FullName);

            result.FilePath = fileInfo.FullName;
            result.FileName = fileInfo.Name;
            result.Width = metadata.Width;
            result.Height = metadata.Height;
            result.DurationMetadata = metadata.Duration;
            result.Fps = metadata.Framerate;

            return result;
        }


        public VideoFragment Validate(VideoFragment input)
        {
            isNotValid = false;

            //if (input.Height != 1296 || input.Width != 2304)
            //{
            //    input.Error = new ErrorData(ErrorType.Size, $"H: {input.Height} W: {input.Width}");
            //}

            if (input.DurationMetadata < 1000)
            {
                input.Error = new ErrorData(ErrorType.Duration, $"Duration: {input.DurationMetadata}");
                input.Type = VideoFragmentType.Corrupted;
            }

            if (input.Fps == 0 || input.Fps > 30)
            {
                input.Error = new ErrorData(ErrorType.Framerate, $"Fps: {input.Fps}");
                input.Type = VideoFragmentType.Corrupted;
            }

            if (input.Error == null)
            {

                var pci = new ProcessStartInfo(Path.Combine(_ffmpegPath, "ffmpeg.exe"), $"-i {input.FilePath} -c copy -f null -")
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
            }

            input.TotalFrames = frames;
            input.DurationFfmpeg = duration.TotalSeconds;
            input.Tbn = tbn;

            if (isNotValid)
            {
                input.Error = new ErrorData(ErrorType.InvalidFile, reason);
                input.Type = VideoFragmentType.Corrupted;
            }

            return input;
        }

        public void PrepareCameraDirectory(string path)
        {
            var dir = new DirectoryInfo(path);

            Directory.CreateDirectory(Path.Combine(path, "artifacts", "corrupted"));

            dir.GetFiles().Where(x => string.Equals(x.Extension, ".jpg", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => File.Delete(x.FullName));
        }

        public void PrepareCameraDirectory(string day, string camera)
        {
            var dir = new DirectoryInfo(Path.Combine(_storagePath, day, camera));

            Directory.CreateDirectory(Path.Combine(_storagePath, day, camera, "artifacts", "corrupted"));

            dir.GetFiles().Where(x => string.Equals(x.Extension, ".jpg", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => File.Delete(x.FullName));
        }

        void NetErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {

            var errors = new Dictionary<string, string>()
            {
                {"error reading header", "header"},
                //{"Invalid NAL unit size", "nal_size"},
            };

            if (errLine.Data != null)
            {
                if (errors.Any(x => errLine.Data.Contains(x.Key)))
                {
                    if (errLine.Data.Contains(errors.First().Key))
                    {
                        reason = errors.First().Value;
                    }

                    if (errLine.Data.Contains(errors.Skip(1).First().Key))
                    {
                        reason = errors.Skip(1).First().Value;
                    }

                    isNotValid = true;
                    frames = 0;
                    duration = TimeSpan.MinValue;
                }
                else
                {
                    if (errLine.Data.Contains("frame="))
                    {


                        try
                        {
                            var index = errLine.Data.IndexOf("fps");

                            var str = errLine.Data.Remove(index).Replace("frame=", string.Empty);

                            int.TryParse(str, out frames);

                            index = errLine.Data.IndexOf("bitrate");
                            str = errLine.Data.Remove(index);

                            index = str.IndexOf("time=");
                            str = str.Remove(0, index).Replace("time=", string.Empty);
                            TimeSpan.TryParse(str, out duration);
                        }
                        catch (Exception e)
                        {
                        }
                    }

                    if (errLine.Data.Contains("Stream #0:0(eng): Video: h264"))
                    {
                        try
                        {
                            var index = errLine.Data.IndexOf("tbn");
                            string str = string.Empty;
                                 
                            if (index > 0)
                            {
                                str = errLine.Data.Remove(index);

                                index = str.LastIndexOf(",");
                                str = str.Remove(0, index + 1);

                                tbn = str.Trim();
                            }

                            index = errLine.Data.IndexOf("fps");

                            if (index > 0)
                            {
                                str = errLine.Data.Remove(index);

                                index = str.LastIndexOf(",");
                                str = str.Remove(0, index + 1);

                                double.TryParse(str.Trim(), out fps);
                            }

                            index = errLine.Data.IndexOf(", q=");

                            if (index > 0)
                            {
                                str = errLine.Data.Remove(index);

                                index = str.LastIndexOf(",");
                                str = str.Remove(0, index + 1);

                                int.TryParse(str.Split("x").First().Trim(), out width);
                                int.TryParse(str.Split("x").Last().Trim(), out height);
                            }
                        }
                        catch (Exception e)
                        {
                           
                        }
                        

                    }
                }
            }
        }
    }
}
