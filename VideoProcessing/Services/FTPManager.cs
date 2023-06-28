using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentFTP;

namespace test3.Services
{
    public class FTPManager
    {
        private readonly string _storagePath;
        private FtpClient client;
        public string currentCameraName;
        public string currentDayName;
        private readonly Stopwatch _timer;

        public FTPManager()
        {
            _storagePath = Program.Configuration.StorageLocation;
            client = new FtpClient(Program.Configuration.HaFtpHost, Program.Configuration.HaFtpUser, Program.Configuration.HaFtpPassword);
            _timer = new Stopwatch();
        }

        public void SyncFiles()
        {
            Console.WriteLine("Sync FTP data:");

            client.AutoConnect();


            var today = DateTime.Now;

            var daysToCheck = new List<DateTime>();

            for (int i = 1; i < Program.Configuration.UploadDaysCount + 1; i++)
            {
                daysToCheck.Add(new DateTime(today.AddDays(-i).Year, today.AddDays(-i).Month, today.AddDays(-i).Day));
            }

            var filesToCheck = daysToCheck.Select(x => x.ToString("dd_MM_yyyy") + ".mp4").ToList();

            var uploadedFiles = client.GetListing("/media").Where(x => x.Name.EndsWith(".mp4")).ToList();

            var filesToRemove = uploadedFiles.Where(x => !filesToCheck.Any(t => t.Contains(x.Name))).ToList();

            foreach (var ftpListItem in filesToRemove)
            {
                client.DeleteFile(ftpListItem.FullName);
            }

            var dir = new DirectoryInfo(Program.Configuration.StorageLocation);

            var allFiles = dir.GetFiles().Where(x => x.Extension == ".mp4");

            var filesToUpload = allFiles.Where(x => filesToCheck.Contains(x.Name));

            foreach (var fileInfo in filesToUpload)
            {
                client.UploadFile(fileInfo.FullName, Path.Combine("/media", fileInfo.Name), FtpRemoteExists.Skip);
            }
        }

        public IList<string> GetRecordedDays()
        {
            var result = new List<string>();

            var camera = client.GetListing("/media").First();

            var years = client.GetListing(camera.FullName);

            if (!Directory.Exists(Program.Configuration.StorageLocation))
            {
                Directory.CreateDirectory(Program.Configuration.StorageLocation);
            }

            foreach (var year in years)
            {
                int currentYear;
                if (!int.TryParse(year.Name, out currentYear)) continue;

                var months = client.GetListing(year.FullName);

                foreach (var month in months)
                {
                    int currentMonth;
                    if (!int.TryParse(month.Name, out currentMonth)) continue;

                    var days = client.GetListing(month.FullName);

                    foreach (var day in days)
                    {
                        int currentDay;
                        if (!int.TryParse(day.Name, out currentDay)) continue;

                        result.Add($"{currentDay:D2}_{currentMonth:D2}_{currentYear}");
                    }
                }
            }

            return result;
        }

        public void FetchDayData(string day)
        {
            var dayParts = day.Split("_");

            if (!Directory.Exists(Path.Combine(_storagePath, day)))
            {
                Directory.CreateDirectory(Path.Combine(_storagePath, day));

                currentDayName = day;

                foreach (var camera in client.GetListing("/media"))
                {
                    currentCameraName = camera.Name;

                    if (!Directory.Exists(Path.Combine(_storagePath, day, camera.Name)))
                    {
                        Directory.CreateDirectory(Path.Combine(_storagePath, day, camera.Name));
                    }

                    _timer.Reset();
                    _timer.Start();
                    client.DownloadDirectory(Path.Combine(_storagePath, day, camera.Name), $"/media/{camera.Name}/{dayParts[2]}/{dayParts[1]}/{dayParts[0]}", FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None, null, Progress);
                    _timer.Stop();
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"| {currentDayName}\t| {currentCameraName}\t| Download\t| Progress 100 %                    \t|");
                    OutputManager.NextLine();
                }
            }
        }

        private void Progress(FtpProgress obj)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"| {currentDayName}\t| {currentCameraName}\t| Download\t| {_timer.Elapsed.ToString(@"hh\:mm\:ss")} | Progress {(int)(obj.FileIndex / (obj.FileCount / 100.0))} %\t|");
        }
    }
}
