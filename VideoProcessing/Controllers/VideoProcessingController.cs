﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Emgu.CV.OCR;
using Microsoft.Extensions.Options;
using test3;
using test3.Extensions;
using test3.Services;

namespace VideoProcessing.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoProcessingController : ControllerBase
    {
        private readonly BackgroundWorkerQueue _backgroundWorkerQueue;
        

        public VideoProcessingController(BackgroundWorkerQueue backgroundWorkerQueue)
        {
            _backgroundWorkerQueue = backgroundWorkerQueue;
            
        }

        [HttpGet]
        [Route("ping")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                var filePath = @"E:\app\pruhozha_01_20220803091301.mp4";

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.WriteAllText(@"E:\app\yes.txt", "metadata.Framerate.ToString()");
                }

                //var metadata = new MediaInfoWrapper(filePath);

                //if (System.IO.File.Exists(filePath))
                //{
                //    System.IO.File.WriteAllText(@"E:\app\111.txt", metadata.Framerate.ToString());
                //}
                //else
                //{

                //    System.IO.File.WriteAllText(@"E:\app\222.txt", metadata.Framerate.ToString());
                //}

            }
            catch (Exception e)
            {

                System.IO.File.WriteAllText(@"E:\app\333.txt", e.InnerException.Message);
            }
            
            


            return new JsonResult(Program.Configuration);
        }


        [HttpGet]
        [Route("create")]
        public async Task<IActionResult> Get()
        {
            Program.Configuration = ConfigurationData.Get();
            Program.CurrentJobTimer = new Stopwatch();
            Program.CurrentJobTimer.Start();


            if (Program.Configuration == null || Program.Configuration.FfmpegLocation == null)
            {
                return new ContentResult() { Content = "No Configuration", ContentType = "text/plain", StatusCode = (int)HttpStatusCode.InternalServerError };
            }
            
            _backgroundWorkerQueue.QueueBackgroundWorkItem(async token =>
            {
                Process();
            });

            return new RedirectResult("log");//{Content = "Job started" , ContentType = "text/plain", StatusCode = (int)HttpStatusCode.OK};
        }

        

        [HttpGet]
        [Route("log")]
        public async Task<IActionResult> LastLog(int index)
        {
            var dir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));

            var lastLogFile = dir.GetFiles().OrderByDescending(x => x.Name).ToList().Skip(index).First();

            var fileData = System.IO.File.ReadAllLines(lastLogFile.FullName).ToList();

            fileData.Insert(0, lastLogFile.Name);

            return new ContentResult() { Content = string.Join("\n", fileData), ContentType = "text/plain", StatusCode = (int)HttpStatusCode.OK };
        }


        [HttpGet]
        [Route("log/list")]
        public async Task<IActionResult> LastLogList()
        {
            var dir = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));

            var logs = dir.GetFiles().OrderByDescending(x => x.Name);

            var content = string.Empty;

            foreach (var logsName in logs)
            {
                content += $"{logsName.Name} \t {logsName.Length} \n";
            }

            return new ContentResult() { Content = content, ContentType = "text/plain", StatusCode = (int)HttpStatusCode.OK };
        }


        private void Process()
        {
            //Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);

            OutputManager.SetSessionName(DateTime.Now.ToString("MM_dd_yyyy_HH_mm"));

            var today = DateTime.Now;

            var daysToCheck = new List<DateTime>();

            for (int i = 1; i < Program.Configuration.CheckDaysCount; i++)
            {
                daysToCheck.Add(new DateTime(today.AddDays(-i).Year, today.AddDays(-i).Month, today.AddDays(-i).Day));
            }

            var root = new DirectoryInfo(Program.Configuration.StorageLocation);

            var completed = root.GetFiles().Where(x => x.Extension == ".mp4");

            daysToCheck = daysToCheck.Where(x => !completed.Any(t => t.FullName.Contains(x.ToString("dd_MM_yyyy")))).ToList();

            var daysToProcess = new List<DateTime>();

            foreach (var dayToCheck in daysToCheck)
            {
                var str = dayToCheck.ToString("yyyy_MM_dd").Split("_");

                var dayLocation = Path.Combine(Program.Configuration.StorageLocation, str[0], str[1], str[2]);

                if (Directory.Exists(dayLocation))
                {
                    daysToProcess.Add(dayToCheck);
                }
            }

            var ftpManager = new FTPManager();

            daysToProcess.Reverse();

            foreach (var day in daysToProcess)
            {
                try
                {
                    var stringDay = day.ToString("yyyy_MM_dd");

                    OutputManager.AddText($"{stringDay} Starting rendering ---------------------------------------------------------------------------------------------", false);

                    var str = day.ToString("yyyy_MM_dd").Split("_");

                    var artifactsLocation = Path.Combine(Program.Configuration.StorageLocation, str[0], str[1], str[2], "artifacts");

                    artifactsLocation.CheckDirectory();

                    OutputManager.SetDay(stringDay);

                    var data = new VideoPreprocessor().PreprocessDay(day, Program.Configuration.Cameras);

                    OutputManager.AddText($"{stringDay} Finished Preprocess", false);

                    var test = new FileDataParser();

                    data = test.CreateVideoScreens(data);

                    OutputManager.AddText($"{stringDay} Finished CreateVideoScreens", false);

                    data = test.UpdateDatesData(data);

                    OutputManager.AddText($"{stringDay} Finished UpdateDatesData", false);

                    new VideoFpsAligner().AlignFps(data);

                    OutputManager.AddText($"{stringDay} Finished AlignFps", false);

                    data = new VideoDummyDataGenerator().GenerateMissedBlackParts(data);

                    data = new VideoJoiner().Join(data);

                    OutputManager.AddText($"{stringDay} Finished Join", false);

                    data = new VideoGrouper().Group(data);

                    OutputManager.AddText($"{stringDay} Finish rendering ---------------------------------------------------------------------------------------------", false);

                    OutputManager.AddText($"{stringDay} Start Uploading ---------------------------------------------------------------------------------------------", false);
                    
                    ftpManager.SyncFiles();
                    
                    OutputManager.AddText($"{stringDay} Finish Uploading ---------------------------------------------------------------------------------------------", false);
                }
                catch (Exception e)
                {
                    OutputManager.AddText($"Failed to render day {day.ToString("yyyy_MM_dd")}: {e.Message} ---------------------------------------------------------------------------------------------" , false);
                }
            }

            Program.CurrentJobTimer.Stop();
        }
    }
}
