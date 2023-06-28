//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using Emgu.CV;
//using Emgu.CV.CvEnum;
//using Emgu.CV.OCR;
//using Emgu.CV.Structure;
//using test3.Extensions;
//using test3.Models;

//namespace test3.Services
//{
//    public class FileDataParser
//    {
//        private readonly string _ffmpegPath;
//        private readonly Emgu.CV.OCR.Tesseract _engine;

//        private int _totalFilesToProcess;
//        private int _filesProcessed;
//        private int _filesCorrupted;
//        private DataManager _dataManager;

//        public FileDataParser()
//        {
//            _ffmpegPath = Program.Configuration.FfmpegLocation;

//            _engine = new Emgu.CV.OCR.Tesseract(Program.Configuration.TessdataLocation, Program.Configuration.TessdataLang, OcrEngineMode.Default);
//            _dataManager = new DataManager();
//        }

//        public DayData CreateVideoScreens(DayData day)
//        {
//            foreach (var camera in day.CameraDayData)
//            {
//                var screensFolder = Path.Combine(day.FilesPath, "artifacts", "screens");
//                screensFolder.CheckDirectory();

//                var dir = new DirectoryInfo(screensFolder);

//                var screens = dir.GetFiles().Where(x=>x.Name.StartsWith(camera.Name)).Select(x=>x.Name).ToList();

//                _totalFilesToProcess = camera.VideoFragments.Count(x => x.IsValid() && x.Type != VideoFragmentType.Black);
//                _filesProcessed = screens.Count;

//                if (_filesProcessed == _totalFilesToProcess)
//                {
//                    OutputManager.DisplayCreatingScreenshotsSkip(camera.Name);
//                }
//                else
//                {
//                    foreach (var videoFragment in camera.VideoFragments.Where(x => x.IsValid() && !screens.Contains(x.FileName.Replace(".mp4", ".png"))))
//                    {
//                        CreateVideoScreen(videoFragment.FilePath);
//                        _filesProcessed++;
//                        OutputManager.DisplayCreatingScreenshots(camera.Name, _totalFilesToProcess, _filesProcessed);
//                    }
//                    OutputManager.NextLine();
//                }


//            }

//            return day;
//        }

//        public DayData UpdateDatesData(DayData day)
//        {

//            var currentDate = new DateTime(int.Parse(day.Name.Split("_")[2]), int.Parse(day.Name.Split("_")[1]), int.Parse(day.Name.Split("_")[0]));

//            foreach (var camera in day.CameraDayData)
//            {
//                DateTime prevEnd = DateTime.MinValue;


//                _totalFilesToProcess = camera.VideoFragments.Count(x => x.IsValid());
//                _filesProcessed = camera.VideoFragments.Count(x => x.IsValid() && x.Start != DateTime.MinValue);
//                _filesCorrupted = camera.VideoFragments.Count(x => !x.IsValid() && x.Error.ErrorType == ErrorType.CreatedDate);

//                if (camera.VideoFragments.Where(x => x.IsValid()).All(x => x.Start != DateTime.MinValue))
//                {
//                    OutputManager.DisplayDateRecognitionSkip(camera.Name, camera.VideoFragments.Count(x => x.IsValid()), _filesCorrupted);
//                }
//                else
//                {
//                    foreach (var videoFragment in camera.VideoFragments.Where(x => x.IsValid() && x.Start == DateTime.MinValue))
//                    {
//                        var result = GetCreatedDate(currentDate, videoFragment);

//                        if (result != null && result.Error != null)
//                        {
//                            videoFragment.Error = new ErrorData
//                            {
//                                ErrorType = ErrorType.CreatedDate,
//                                Data = result.Error
//                            };
//                            videoFragment.Type = VideoFragmentType.Corrupted;
//                            _filesCorrupted++;
//                        }
//                        else
//                        {
//                            videoFragment.Start = result.Time;
//                            _filesProcessed++;
//                        }

//                        _dataManager.UpdateMetadata(day.FilesPath, camera.Name, camera.VideoFragments);

//                        OutputManager.DisplayDateRecognition(camera.Name, _totalFilesToProcess, _filesProcessed, _filesCorrupted);
//                    }
//                    OutputManager.NextLine();
//                }
//            }

//            return day;
//        }

//        public DateResult GetCreatedDate(DateTime currentDate, VideoFragment fragment)
//        {
//            var root = Path.GetDirectoryName(fragment.FilePath);
//            var fileName = Path.GetFileName(fragment.FilePath);

//            bool isParsed = false;

//            try
//            {
//                var positions = new Dictionary<int, List<int>>
//                {
//                    {2304, new List<int>{ 1100, 320, 31, 65, 127, 161, 221, 257, 80, 35, 5}},
//                    {896, new List<int>{ 422, 158, 14, 32, 62, 80, 110, 128, 46, 20, 3}},

//                };


//                if (!positions.ContainsKey(fragment.Width))
//                {
//                    return new DateResult()
//                    {
//                        FileName = fragment.FilePath,
//                        Error = "Unsupported resolution",
//                    };
//                }
//var result = TryParse(currentDate, @fragment.FilePath, new List<int> { 1100, 320, 31, 65, 127, 161, 221, 257, 80, 35, 5 }, false);
//if (fragment.Width == 2304 && result == null || result.Error != null)
//{
//    fragment.Error = null;
//    fragment.Type = VideoFragmentType.OriginalFile;

//    var result2 = TryParse(currentDate, @fragment.FilePath, new List<int> { 1100, 370, 25, 65, 148, 191, 271, 313, 100, 48, 5 }, true, true);

//    if (result2 == null || result2.Error != null)
//    {

//    }

//    return result2;
//}
//                var values = positions[fragment.Width];


//                Image<Bgr, Byte> raw = new Image<Bgr, Byte>(Path.Combine(root, "artifacts", "screens", fileName.Replace(".mp4", ".png")));

//                var original = raw.Convert<Gray, byte>();

//                raw.Dispose();

//                original.ROI = new Rectangle(values[0], values[10], values[1], values[8]);

//                var digits = original.Copy();

//                var binDigits = new Image<Gray, byte>(digits.Width, digits.Height, new Gray(0));

//                CvInvoke.Threshold(digits, binDigits, 200, 255, ThresholdType.Binary);
//                CvInvoke.BitwiseNot(binDigits, binDigits);
//                //CvInvoke.Erode(binDigits, binDigits, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
//                //CvInvoke.GaussianBlur(binDigits, binDigits, new System.Drawing.Size(3, 3), 3);

//                var imageToProcess = binDigits.Copy();

//                //if (fragment.Width == 896)
//                //{
//                //    Path.Combine(root, "not_recognized").CheckDirectory();

//                //    imageToProcess.Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", ".png")));
//                //}

//                var allChars = new List<string>();

//                binDigits.ROI = new Rectangle(values[2], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_1.png")));

//                binDigits.ROI = new Rectangle(values[3], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_2.png")));

//                binDigits.ROI = new Rectangle(values[4], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_3.png")));

//                binDigits.ROI = new Rectangle(values[5], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_4.png")));

//                binDigits.ROI = new Rectangle(values[6], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_5.png")));

//                binDigits.ROI = new Rectangle(values[7], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_6.png")));

//                try
//                {
//                    var hours = int.Parse(allChars[0] + allChars[1]);
//                    var minutes = int.Parse(allChars[2] + allChars[3]);
//                    var seconds = int.Parse(allChars[4] + allChars[5]);

//                    var date = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, hours, minutes, seconds);

//                    return new DateResult()
//                    {
//                        FileName = fragment.FilePath,
//                        Time = date,
//                    };
//                    isParsed = true;
//                }
//                catch (Exception e)
//                {

//                }

//                if (!isParsed)
//                {
//                    Path.Combine(root, "artifacts", "not_recognized").CheckDirectory();

//                    imageToProcess.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", ".png")));

//                    return new DateResult()
//                    {
//                        FileName = fragment.FilePath,
//                        Error = string.Join(" ", allChars)
//                    };
//                }

//            }
//            catch (Exception e)
//            {
//                return new DateResult()
//                {
//                    FileName = fragment.FilePath,
//                    Error = e.Message
//                };
//            }

//            return null;
//        }

//        private DateResult TryParse(DateTime currentDate, string filePath, List<int> values)
//        {
//            var root = Path.GetDirectoryName(filePath);
//            var fileName = Path.GetFileName(filePath);

//            bool isParsed = false;

//            try
//            {

//                Image<Bgr, Byte> raw = new Image<Bgr, Byte>(Path.Combine(root, "artifacts", "screens", fileName.Replace(".mp4", ".png")));

//                var original = raw.Convert<Gray, byte>();

//                raw.Dispose();

//                original.ROI = new Rectangle(values[0], values[10], values[1], values[8]);

//                var digits = original.Copy();

//                var binDigits = new Image<Gray, byte>(digits.Width, digits.Height, new Gray(0));

//                CvInvoke.Threshold(digits, binDigits, 200, 255, ThresholdType.Binary);
//                CvInvoke.BitwiseNot(binDigits, binDigits);
//                //CvInvoke.Erode(binDigits, binDigits, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
//                //CvInvoke.GaussianBlur(binDigits, binDigits, new System.Drawing.Size(3, 3), 3);

//                var imageToProcess = binDigits.Copy();

//                //if (fragment.Width == 896)
//                //{
//                //    Path.Combine(root, "not_recognized").CheckDirectory();

//                //    imageToProcess.Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", ".png")));
//                //}

//                var allChars = new List<string>();

//                binDigits.ROI = new Rectangle(values[2], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_1.png")));

//                binDigits.ROI = new Rectangle(values[3], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_2.png")));

//                binDigits.ROI = new Rectangle(values[4], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_3.png")));

//                binDigits.ROI = new Rectangle(values[5], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_4.png")));

//                binDigits.ROI = new Rectangle(values[6], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_5.png")));

//                binDigits.ROI = new Rectangle(values[7], 0, values[9], values[8]);
//                allChars.Add(ParseText(binDigits.Copy()));

//                //binDigits.Copy().Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", "_6.png")));

//                try
//                {
//                    var hours = int.Parse(allChars[0] + allChars[1]);
//                    var minutes = int.Parse(allChars[2] + allChars[3]);
//                    var seconds = int.Parse(allChars[4] + allChars[5]);

//                    var date = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, hours, minutes, seconds);

//                    return new DateResult()
//                    {
//                        FileName = filePath,
//                        Time = date,
//                    };
//                    isParsed = true;
//                }
//                catch (Exception e)
//                {

//                }

//                if (!isParsed)
//                {
//                    Path.Combine(root, "artifacts", "not_recognized").CheckDirectory();

//                    imageToProcess.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", ".png")));

//                    return new DateResult()
//                    {
//                        FileName = filePath,
//                        Error = string.Join(" ", allChars)
//                    };
//                }

//            }
//            catch (Exception e)
//            {
//                return new DateResult()
//                {
//                    FileName = filePath,
//                    Error = e.Message
//                };
//            }

//            return null;
//        }

//        private void CreateVideoScreen(string filePath)
//        {
//            var root = Path.GetDirectoryName(filePath);
//            var fileName = Path.GetFileName(filePath);

//            var args = $"-i {filePath} -y -vf" + " \"select = eq(n\\, 0)\"" + $" -q:v 1 -qmin 1 -vsync 0 -dpi 400 {Path.Combine(root, "artifacts", "screens", fileName.Replace(".mp4", ".png"))}";

//            var pci = new ProcessStartInfo(Path.Combine(_ffmpegPath, "ffmpeg.exe"), args)
//            {
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                RedirectStandardInput = true,
//                UseShellExecute = false,
//                CreateNoWindow = false
//            };

//            var fileValidationProcess = new Process();

//            fileValidationProcess.StartInfo = pci;
//            fileValidationProcess.ErrorDataReceived += new DataReceivedEventHandler(NetErrorDataHandler);

//            fileValidationProcess.Start();
//            fileValidationProcess.BeginOutputReadLine();
//            fileValidationProcess.BeginErrorReadLine();
//            fileValidationProcess.WaitForExit();

//            fileValidationProcess.ErrorDataReceived -= new DataReceivedEventHandler(NetErrorDataHandler);

//            fileValidationProcess.Close();
//        }

//        void NetErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
//        {

//        }

//        private string ParseText(Image<Gray, byte> data)
//        {
//            string text = string.Empty;

//            _engine.SetImage(data);

//            text = _engine.GetUTF8Text();

//            if (text.Length > 1)
//            {
//                text = text.Substring(0, 1);
//            }

//            text = text.Replace("Z", "2");
//            text = text.Replace("?", "7");
//            text = text.Replace("I", "7");
//            text = text.Replace("b", "6");
//            text = text.Replace("O", "0");

//            return text;
//        }
//    }



//    public class DateResult
//    {
//        public string FileName { get; set; }
//        public DateTime Time { get; set; }

//        public string Error { get; set; }
//    }
//}
