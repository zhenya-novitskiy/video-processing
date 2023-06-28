using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using test3.Extensions;
using test3.Models;
using VideoProcessing.Models;

namespace test3.Services
{
    public class FileDataParser
    {
        private readonly string _ffmpegPath;
        private readonly Emgu.CV.OCR.Tesseract _engine;

        private int _totalFilesToProcess;
        private int _filesProcessed;
        private int _filesCorrupted;
        private int _highRecognitions = 0;
        private int _midRecognitions = 0;
        private int _lowRecognitions = 0;
        private int _fromFileRecognitions = 0;

        private DataManager _dataManager;

        public FileDataParser()
        {
            _ffmpegPath = Program.Configuration.FfmpegLocation;
            
            _engine = new Emgu.CV.OCR.Tesseract(Program.Configuration.TessdataLocation, Program.Configuration.TessdataLang, OcrEngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", "0123456789:");

            _dataManager = new DataManager();
        }

        public DayData CreateVideoScreens(DayData day)
        {
            foreach (var camera in day.CameraDayData)
            {
                var screensFolder = Path.Combine(day.FilesPath, "artifacts", "screens");
                screensFolder.CheckDirectory();

                var dir = new DirectoryInfo(screensFolder);

                var screens = dir.GetFiles().Where(x=>x.Name.StartsWith(camera.Name)).Select(x=>x.Name).ToList();

                _totalFilesToProcess = camera.VideoFragments.Count(x => x.IsValid() && x.Type != VideoFragmentType.Black);
                _filesProcessed = screens.Count;

                int failedToCreate = 0;

                if (_filesProcessed == _totalFilesToProcess)
                {
                    OutputManager.DisplayCreatingScreenshotsSkip(camera.Name);
                }
                else
                {
                    foreach (var videoFragment in camera.VideoFragments.Where(x => x.IsValid() && !screens.Contains(x.FileName.Replace(".mp4", ".png"))))
                    {
                        var screenUrl = CreateVideoScreen(videoFragment.FilePath);
                        if (!File.Exists(screenUrl))
                        {
                            failedToCreate++;
                        }

                        _filesProcessed++;
                        OutputManager.DisplayCreatingScreenshots(camera.Name, _totalFilesToProcess, _filesProcessed, failedToCreate);
                    }
                    OutputManager.NextLine();
                }

                
            }

            return day;
        }

        public DayData UpdateDatesData(DayData day)
        {

            var currentDate = new DateTime(int.Parse(day.Name.Split("_")[2]), int.Parse(day.Name.Split("_")[1]), int.Parse(day.Name.Split("_")[0]));

            foreach (var camera in day.CameraDayData)
            {
                DateTime prevEnd = DateTime.MinValue;


                _totalFilesToProcess = camera.VideoFragments.Count(x => x.IsValid());
                _filesProcessed = camera.VideoFragments.Count(x => x.IsValid() && x.Start != DateTime.MinValue);
                _filesCorrupted = camera.VideoFragments.Count(x => !x.IsValid() && x.Error.ErrorType == ErrorType.CreatedDate);
                _filesProcessed = 0;
                _filesCorrupted = 0;

                _highRecognitions = 0;
                _midRecognitions = 0;
                _lowRecognitions = 0;
                _fromFileRecognitions = 0;

                if (camera.VideoFragments.Where(x => x.IsValid()).All(x => x.Start != DateTime.MinValue))
                {
                    OutputManager.DisplayDateRecognitionSkip(camera.Name, camera.VideoFragments.Count(x => x.IsValid()), _filesCorrupted);
                }
                else
                {
                    foreach (var videoFragment in camera.VideoFragments.Where(x => x.IsValid() && x.Start == DateTime.MinValue))
                    {
                        var result = GetCreatedDate(currentDate, videoFragment);

                        if (result != null && result.Error != null)
                        {
                            videoFragment.Error = new ErrorData
                            {
                                ErrorType = ErrorType.CreatedDate,
                                Data = result.Error
                            };
                            videoFragment.Type = VideoFragmentType.Corrupted;
                            _filesCorrupted++;
                        }
                        else
                        {
                            videoFragment.Start = result.Time;
                            _filesProcessed++;
                        }

                        _dataManager.UpdateMetadata(day.FilesPath, camera.Name, camera.VideoFragments);

                        switch (result.Accuracy)
                        {
                            case Accuracy.High:
                                _highRecognitions++;
                                break;
                            case Accuracy.Mid:
                                _midRecognitions++;
                                break;
                            case Accuracy.Low:
                                _lowRecognitions++;
                                break;
                            case Accuracy.FromFile:
                                _fromFileRecognitions++;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        OutputManager.DisplayDateRecognition(camera.Name, _totalFilesToProcess, _filesProcessed, _filesCorrupted, _highRecognitions, _midRecognitions, _lowRecognitions, _fromFileRecognitions);
                    }
                    OutputManager.NextLine();
                }
            }

            return day;
        }

        public DateResult GetCreatedDate(DateTime currentDate, VideoFragment fragment)
        {
            try
            {
                //fragment.FilePath = Path.Combine(@"E:\storage\2022\08\10", "pruhozha_01_20220810081917.mp4");
                //fragment.Width = 896;

                switch (fragment.Width)
                {
                    case 2304:
                        var result = TryParse(currentDate, fragment.Width, @fragment.FilePath, new List<int> { 1120, 275, 31, 65, 127, 161, 221, 257, 80, 35, 5 }, false);
                        if (fragment.Width == 2304 && result == null || result.Error != null)
                        { 
                            fragment.Error = null;
                            fragment.Type = VideoFragmentType.OriginalFile;

                            var result2 = TryParse(currentDate, fragment.Width, @fragment.FilePath, new List<int> { 1120, 345, 25, 65, 148, 191, 271, 313, 100, 48, 5 }, true, true);

                            if (result2 == null || result2.Error != null)
                            {

                            }

                            return result2;
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    case 896:
                        return TryParse(currentDate, fragment.Width, fragment.FilePath, new List<int> { 422, 158, 14, 32, 62, 80, 110, 128, 46, 20, 3 });
                    default:
                        return new DateResult()
                        {
                            FileName = fragment.FilePath,
                            Error = "Unsupported resolution",
                        };
                }
            }
            catch (Exception e)
            {
                return new DateResult()
                {
                    FileName = fragment.FilePath,
                    Error = e.Message
                };
            }
        }

        private DateResult TryParse(DateTime currentDate, int width, string filePath, List<int> values, bool treshhold = false, bool copyErrors = false)
        {
            var root = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            bool isParsed = false;

            try
            {

                Image<Bgr, Byte> raw = new Image<Bgr, Byte>(Path.Combine(root, "artifacts", "screens", fileName.Replace(".mp4", ".png")));

                var original = raw.Convert<Gray, byte>();

                var copy = raw.Copy();

                raw.Dispose();

                original.ROI = new Rectangle(values[0], values[10], values[1], values[8]);

                var digits = original.Copy();

                var binDigits = new Image<Gray, byte>(digits.Width, digits.Height, new Gray(0));

                CvInvoke.Threshold(digits, binDigits, 200, 255, ThresholdType.Binary);
                CvInvoke.BitwiseNot(binDigits, binDigits);

                if (treshhold)
                {
                    CvInvoke.Erode(binDigits, binDigits, null, new System.Drawing.Point(1, 1), 1, BorderType.Default, CvInvoke.MorphologyDefaultBorderValue);
                    CvInvoke.GaussianBlur(binDigits, binDigits, new System.Drawing.Size(3, 3), 3);
                }

                //binDigits.Save(Path.Combine(root, "artifacts", "screens", "recognized", "1.png"));

                var text = ParseText(binDigits);

                //PrintTextToImage(600, $"{text}", copy, new Bgr(Color.Blue), root, fileName);

                //var imageToProcess = binDigits.Copy();

                ////if (fragment.Width == 896)
                ////{
                ////    Path.Combine(root, "not_recognized").CheckDirectory();

                ////    imageToProcess.Save(Path.Combine(root, "not_recognized", fileName.Replace(".mp4", ".png")));
                ////}

                //var allChars = new List<string>();

                //binDigits.ROI = new Rectangle(values[2], 0, values[9], values[8]);
                //allChars.Add(ParseDigit(binDigits.Copy()));

                //var binDigit1 = binDigits.Copy();

                //binDigits.ROI = new Rectangle(values[3], 0, values[9], values[8]);
                //allChars.Add(ParseDigit(binDigits.Copy()));

                //var binDigit2 = binDigits.Copy();

                //binDigits.ROI = new Rectangle(values[4], 0, values[9], values[8]);
                //allChars.Add(ParseDigit(binDigits.Copy()));

                //var binDigit3 = binDigits.Copy();

                //binDigits.ROI = new Rectangle(values[5], 0, values[9], values[8]);
                //allChars.Add(ParseDigit(binDigits.Copy()));

                //var binDigit4 = binDigits.Copy();

                //binDigits.ROI = new Rectangle(values[6], 0, values[9], values[8]);
                //allChars.Add(ParseDigit(binDigits.Copy()));

                //var binDigit5 = binDigits.Copy();

                //binDigits.ROI = new Rectangle(values[7], 0, values[9], values[8]);
                //allChars.Add(ParseDigit(binDigits.Copy()));

                //var binDigit6 = binDigits.Copy();

                try
                {

                    var result = ExtractDate(currentDate, text, fileName, width, copy, root);

                    isParsed = true;

                    return new DateResult()
                    {
                        FileName = filePath,
                        Time = result.DateTime,
                        Accuracy = result.Accuracy
                    };
                    
                }
                catch (Exception e)
                {
                    
                }

                if (!isParsed)
                {
                    Path.Combine(root, "artifacts", "not_recognized").CheckDirectory();

                    //if (copyErrors)
                    //{
                    //    imageToProcess.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", ".png")));
                    //    binDigit1.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", "_1.png")));
                    //    binDigit2.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", "_2.png")));
                    //    binDigit3.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", "_3.png")));
                    //    binDigit4.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", "_4.png")));
                    //    binDigit5.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", "_5.png")));
                    //    binDigit6.Save(Path.Combine(root, "artifacts", "not_recognized", fileName.Replace(".mp4", "_6.png")));

                    //}

                    //PrintTextToImage(200, $"{allChars[0]}{allChars[1]}:{allChars[2]}{allChars[3]}:{allChars[4]}{allChars[5]}", copy, new Bgr(Color.Red), root, fileName);

                    return new DateResult()
                    {
                        FileName = filePath,
                        Error = string.Join(" ", text)
                    };
                }

            }
            catch (Exception e)
            {
                return new DateResult()
                {
                    FileName = filePath,
                    Error = e.Message
                };
            }

            return null;
        }

        private RecognitionResult ExtractDate(DateTime currentDate, string recognizedText, string fileName, int width, Image<Bgr, Byte> image, string root)
        {
            int? rHours = null;
            int? rMinutes = null;
            int? rSeconds = null;

            Accuracy accuracy = Accuracy.High;

            var parts = recognizedText.Split(':');

            if (parts.Length >= 1)
            {
                rHours = TryParseNullable(parts[0]);
            }

            if (parts.Length >= 2)
            {
                rMinutes = TryParseNullable(parts[1]);
            }

            if (parts.Length >= 3)
            {
                rSeconds = TryParseNullable(parts[2]);
            }

            var filePathName = fileName.Replace(".mp4", string.Empty);
            var fHours = int.Parse(filePathName.Substring(filePathName.Length - 6, 2));
            var fMinutes = int.Parse(filePathName.Substring(filePathName.Length - 4, 2));
            var fSeconds = int.Parse(filePathName.Substring(filePathName.Length - 2, 2));

            if (rHours == null)
            {
                rHours = fHours;
                accuracy = Accuracy.FromFile;
            }

            if (rMinutes == null)
            {
                rMinutes = fMinutes;
                accuracy = Accuracy.FromFile;
            }

            if (rSeconds == null)
            {
                rSeconds = fSeconds;
                accuracy = Accuracy.FromFile;
            }

            double diff = (new TimeSpan((int)rHours, (int)rMinutes, (int)rSeconds) - new TimeSpan(fHours, fMinutes, fSeconds)).TotalSeconds;
            if (diff < 0) diff = -diff;

            if (diff < 10)
            {
                return new RecognitionResult
                {
                    Accuracy = accuracy != Accuracy.FromFile ? Accuracy.High : Accuracy.FromFile,
                    DateTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, (int)rHours, (int)rMinutes, (int)rSeconds)
                };
            }

            PrintTextToImage(353, width, $"1 {diff}", image, new Bgr(Color.Red), root, fileName);

            rHours = fHours;
            accuracy = Accuracy.Mid;

            diff = (new TimeSpan((int)rHours, (int)rMinutes, (int)rSeconds) - new TimeSpan(fHours, fMinutes, fSeconds)).TotalSeconds;
            if (diff < 0) diff = -diff;
            
            if (diff < 10)
            {
                return new RecognitionResult
                {
                    Accuracy = accuracy,
                    DateTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, (int)rHours, (int)rMinutes, (int)rSeconds)
                };
            }

            PrintTextToImage(453, width, $"2 {diff}", image, new Bgr(Color.Red), root, fileName);

            rMinutes = fMinutes;
            accuracy = Accuracy.Low;

            diff = (new TimeSpan((int)rHours, (int)rMinutes, (int)rSeconds) - new TimeSpan(fHours, fMinutes, fSeconds)).TotalSeconds;
            if (diff < 0) diff = -diff;

            if (diff < 10)
            {
                return new RecognitionResult
                {
                    Accuracy = accuracy,
                    DateTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, (int)rHours, (int)rMinutes, (int)rSeconds)
                };
            }

            accuracy = Accuracy.FromFile;

            PrintTextToImage(153, width, $"{recognizedText}", image, new Bgr(Color.Red), root, fileName);

            PrintTextToImage(453, width, $"3 {diff}", image, new Bgr(Color.Red), root, fileName);

            return new RecognitionResult
            {
                Accuracy = accuracy,
                DateTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, (int)rHours, (int)rMinutes, (int)rSeconds)
            };
        }

        private int? TryParseNullable(string val)
        {
            int outValue;
            return int.TryParse(val, out outValue) ? (int?)outValue : null;
        }

        private string CreateVideoScreen(string filePath)
        {
            var root = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            var screenUrl = Path.Combine(root, "artifacts", "screens", fileName.Replace(".mp4", ".png"));

            var args = $"-i {filePath} -y -vf" + " \"select = eq(n\\, 0)\"" + $" -q:v 1 -qmin 1 -vsync 0 -dpi 400 {screenUrl}";

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

            return screenUrl;
        }

        void NetErrorDataHandler(object sendingProcess, DataReceivedEventArgs errLine)
        {

        }

        private string ParseDigit(Image<Gray, byte> data)
        {
            string text = string.Empty;

            _engine.SetImage(data);
            
            text = _engine.GetUTF8Text();


            if (text.Length > 1)
            {
                text = text.Substring(0, 1);
            }

            text = text.Replace("Z", "2");
            text = text.Replace("p", "2");
            text = text.Replace("?", "7");
            text = text.Replace("I", "7");
            text = text.Replace("=", "7");
            text = text.Replace("b", "6");
            text = text.Replace("O", "0");
            text = text.Replace("(", "0");

            return text;
        }

        private string ParseText(Image<Gray, byte> data)
        {
            _engine.SetImage(data);

            var text = _engine.GetUTF8Text();

            text = text.Replace("?", string.Empty);
            text = text.Replace("\r", string.Empty);
            text = text.Replace("\n", string.Empty);

            

            //if (text.Length > 6 && text[1] == '7')
            //{
            //    text = text.First() + text.Substring(2);
            //}

            return text;
        }

        private void PrintTextToImage(int top, int width, string text, Image<Bgr, Byte> image, Bgr color, string root, string fileName)
        {
            CvInvoke.PutText(image, text, new Point(width/2, top), FontFace.HersheyDuplex, 1.87, color.MCvScalar, 2);
            var path = Path.Combine(root, "artifacts", "screens", "recognized", fileName.Replace(".mp4", ".png"));
            path.CheckDirectory();
            image.Save(path);
        }
    }

    

    public class DateResult
    {
        public string FileName { get; set; }
        public DateTime Time { get; set; }

        public Accuracy Accuracy { get; set; }

        public string Error { get; set; }
    }
}
