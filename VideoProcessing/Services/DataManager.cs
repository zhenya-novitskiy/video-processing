using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using test3.Extensions;
using test3.Models;

namespace test3.Services
{
    public class DataManager
    {
        public void CleanupMetadataFile(string path, string cameraName)
        {
            var metadataPath = GetMetadataPath(path, cameraName);

            if (File.Exists(metadataPath))
            {
                File.Delete(metadataPath);
            }
        }

        public void UpdateMetadata(string path, string cameraName, IEnumerable<VideoFragment> fragments)
        {
            var dataForFile = new List<string>();

            dataForFile.Add(VideoFragment.GetHeaderString());
            dataForFile.AddRange(fragments.Select(x => x.ToString()));

            var summary = ReadSummary(path, cameraName);

            if (summary.Any())
            {
                dataForFile.AddRange(ReadSummary(path, cameraName));
            }
            
            CleanupMetadataFile(path, cameraName);

            var metadataPath = GetMetadataPath(path, cameraName);

            metadataPath.CheckDirectory();

            File.AppendAllLines(metadataPath, dataForFile);
        }
        
        public void AddMetadata(string path, string cameraName, VideoFragment fragment)
        {
            var metadataPath = GetMetadataPath(path, cameraName);

            metadataPath.CheckDirectory();

            if (!File.Exists(metadataPath))
            {
                File.AppendAllText(metadataPath, VideoFragment.GetHeaderString() + "\n");
            }

            File.AppendAllText(metadataPath, fragment + "\n");
        }

        public IList<VideoFragment> ReadMetadata(string path, string cameraName)
        {
            var result = new List<VideoFragment>();

            var metadataPath = GetMetadataPath(path, cameraName);


            if (File.Exists(metadataPath))
            {
                var rawData = File.ReadAllLines(metadataPath);

                foreach (string s in rawData.Skip(1))
                {
                    if (s.StartsWith("summary:")) continue;

                    var parts = s.Split("|");

                    var fragment = new VideoFragment();
                    
                    fragment.Type = Enum.Parse<VideoFragmentType>(parts[1]);
                    fragment.Start = new DateTime(long.Parse(parts[2].Replace("\t", string.Empty).Trim()));
                    fragment.End = new DateTime(long.Parse(parts[3].Replace("\t", string.Empty).Trim()));
                    fragment.FilePath = parts[4].Replace("\t", string.Empty).Trim();
                    fragment.FileName = parts[5].Replace("\t", string.Empty).Trim();
                    fragment.TotalFrames = int.Parse(parts[6]);
                    fragment.Width = int.Parse(parts[7]);
                    fragment.Height = int.Parse(parts[8]);
                    fragment.DurationMetadata = double.Parse(parts[9]);
                    fragment.DurationFfmpeg = double.Parse(parts[10]);
                    fragment.Tbn = parts[11].Replace("\t", string.Empty).Trim();
                    fragment.Fps = double.Parse(parts[12]);
                    fragment.IsFpsAligned = bool.Parse(parts[13]);

                    if (parts.Length > 17)
                    {
                        fragment.Error = new ErrorData();

                        fragment.Error.ErrorType = Enum.Parse<ErrorType>(parts[16]);
                        fragment.Error.Data = parts[17].Replace("\t", string.Empty).Trim();
                    }

                    result.Add(fragment);
                }
            }

            return result;
        }


        private string GetMetadataPath(string path, string cameraName)
        {
            return Path.Combine(path, "artifacts", $"{cameraName}_metadata.txt");
        }

        public IList<string> ReadSummary(string path, string cameraName)
        {
            var result = new List<string>();

            var metadataPath = GetMetadataPath(path, cameraName);

            if (File.Exists(metadataPath))
            {
                return File.ReadAllLines(metadataPath).Where(x => x.StartsWith("summary:")).ToList();
            }

            return result;
        }

        public void AddSummary(string path, string cameraName, string text)
        {
            var metadataPath = GetMetadataPath(path, cameraName);

            File.AppendAllText(metadataPath, "summary: " + text + "\n");
        }

        public void WriteFileForConcat(string root, IEnumerable<string> files)
        {
            if (File.Exists(Path.Combine(root, "files.txt")))
            {
                File.Delete(Path.Combine(root, "files.txt"));
            }

            var files2 = files.Select(x => $"file '{x}'");

            File.WriteAllLines(Path.Combine(root, "files.txt"), files2);
        }

        public void DeleteFileForConcat(string root)
        {
            if (File.Exists(Path.Combine(root, "files.txt")))
            {
                File.Delete(Path.Combine(root, "files.txt"));
            }
        }
    }
}
