using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itenso.TimePeriod;
using test3.Extensions;
using test3.Models;

//using Microsoft.WindowsAPICodePack.Shell;

namespace test3.Services
{
    public class VideoDummyDataGenerator
    {
        private VideoGenerator _videoGenerator;
        private DataManager _dataManager;

        private int _totalFilesToProcess;
        private int _filesProcessed;

        public VideoDummyDataGenerator()
        {
            _videoGenerator = new VideoGenerator();
            _dataManager = new DataManager();
        }

        public DayData GenerateMissedBlackParts(DayData day)
        {
            var allFragments = day.CameraDayData.SelectMany(x => x.VideoFragments.Where(x=>x.IsValid() && x.Type != VideoFragmentType.Black)).OrderBy(x=>x.Start).ToList();

            var limits = new TimeRange(allFragments.Min(x => x.Start), allFragments.Max(x => x.End));

            var groupNoMotionPeriods = new TimeGapCalculator<TimeRange>(new TimeCalendar()).GetGaps(new TimePeriodCollection(allFragments), limits).ToList();

            try
            {
                ConsoleManager.DisplayAdditionalInfo("All", $"Limits time: {limits.Duration.ToString(@"hh\:mm\:ss")}");
                ConsoleManager.DisplayAdditionalInfo("All", $"Group no motion: {TimeSpan.FromSeconds(groupNoMotionPeriods.Sum(x => x.Duration.TotalSeconds)).ToString(@"hh\:mm\:ss")}");
            }
            catch (Exception e)
            {
            }
            
            foreach (var camera in day.CameraDayData)
            {
                var cameraFoder = Path.Combine(day.FilesPath, "artifacts", "black", camera.Name);

                if (!_dataManager.ReadSummary(day.FilesPath, camera.Name).Any(x => x.Contains("Black files rendered")))
                {
                    camera.VideoFragments = camera.VideoFragments.Where(x => x.Type != VideoFragmentType.Black).OrderBy(x => x.Start).ToList();

                    cameraFoder.CheckDirectory();

                    var di = new DirectoryInfo(cameraFoder);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    
                    var cameraNoMotionPeriods = new TimeGapCalculator<TimeRange>(new TimeCalendar()).GetGaps(new TimePeriodCollection(camera.VideoFragments.Where(x => x.IsValid())), limits);

                    

                    ConsoleManager.DisplayAdditionalInfo(camera.Name, $"No motion time: {TimeSpan.FromMinutes(cameraNoMotionPeriods.Sum(x => x.Duration.TotalMinutes)).ToString(@"hh\:mm\:ss")}");

                    var dummyRanges = new TimePeriodSubtractor<TimeRange>().SubtractPeriods(cameraNoMotionPeriods, new TimePeriodCollection(groupNoMotionPeriods));

                    var dummyFragments = dummyRanges.Select(x => new VideoFragment { Type = VideoFragmentType.Black, Start = x.Start, End = x.End }).ToList();

                    camera.VideoFragments.AddRange(dummyFragments);

                    camera.VideoFragments = camera.VideoFragments.OrderBy(x => x.Start).ToList();

                    camera.VideoFragments = CombineBlackFragments(camera.VideoFragments).Where(x => x.Duration.TotalSeconds > 0.2).OrderBy(x=>x.Start).ToList();

                    camera.VideoFragments = RenderBlackFragments(camera.VideoFragments, cameraFoder, day.Name, camera.Name);

                    _dataManager.AddSummary(day.FilesPath, camera.Name, "Black files rendered");
                }
                else
                {
                    ConsoleManager.DisplayRenderBlackFragmentsSkip(camera.Name);
                }

                ConsoleManager.DisplayAdditionalInfo(camera.Name, $"Total planned: {camera.VideoFragments.AllValidVideos().Duration().ToString(@"hh\:mm\:ss")}");
            }

            return day;
        }

        private List<VideoFragment> RenderBlackFragments(List<VideoFragment> source, string folder, string day, string cameraName)
        {
            var prevFileName = $"{cameraName}_start.mp4";

            _totalFilesToProcess = source.Count(x => x.Type == VideoFragmentType.Black);
            _filesProcessed = 0;

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].Type == VideoFragmentType.Black)
                {
                    var filePath = Path.Combine(folder, prevFileName.Replace(".mp4", "_.mp4"));

                    prevFileName = _videoGenerator.GenerateNoMotionVideo(filePath, source[i].Duration);
                    source[i].FilePath = prevFileName;
                    source[i].FileName = Path.GetFileName(prevFileName);
                    _filesProcessed++;

                    ConsoleManager.DisplayRenderBlackFragments(cameraName, _totalFilesToProcess, _filesProcessed);
                    _dataManager.UpdateMetadata(day, cameraName, source);
                }

                if (source[i].Type == VideoFragmentType.OriginalFile)
                {
                    prevFileName = source[i].FileName;
                }
            }
            ConsoleManager.NextLine();

            return source;
        }

        private List<VideoFragment> CombineBlackFragments(List<VideoFragment> source)
        {
            var result = new List<VideoFragment>();

            var itemsToCombine = new List<VideoFragment>();

            for (int i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid())
                { 
                    result.Add(source[i]);
                    continue;
                }

                if (i == source.Count - 1)
                {
                    if (source[i].Type == VideoFragmentType.Black)
                    {
                        if (itemsToCombine.Any())
                        {
                            itemsToCombine.Add(source[i]);

                            var totalDuration = itemsToCombine.Sum(x => x.Duration.Ticks);

                            result.Add(new VideoFragment
                            {
                                Type = VideoFragmentType.Black,
                                Start = itemsToCombine.Min(x => x.Start),
                                End = itemsToCombine.Min(x => x.Start).AddTicks(totalDuration)
                            });
                        }
                        else
                        {
                            result.Add(source[i]);
                        }
                    }
                }

                if (source[i].Type == VideoFragmentType.OriginalFile)
                {
                    result.Add(source[i]);

                    if (itemsToCombine.Any())
                    {
                        var totalDuration = itemsToCombine.Sum(x => x.Duration.Ticks);

                        result.Add(new VideoFragment
                        {
                            Type = VideoFragmentType.Black,
                            Start = itemsToCombine.Min(x => x.Start),
                            End = itemsToCombine.Min(x => x.Start).AddTicks(totalDuration)
                        });
                    }

                    itemsToCombine = new List<VideoFragment>();
                    continue;
                }

                if (source[i].Type == VideoFragmentType.Black)
                {
                    itemsToCombine.Add(source[i]);
                    continue;
                }
            }

            return result.OrderBy(x=>x.Start).ToList();
        }
        
    }
}
