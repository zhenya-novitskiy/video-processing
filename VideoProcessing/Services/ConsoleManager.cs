using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace test3.Services
{
    public static class ConsoleManager
    {
        public static string CurrentLogFilePath;
        public static string DayName;
        public static bool OutputToConsole = false;

        public static void SetSessionName(string name)
        {
            CurrentLogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", name + ".txt");

            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
            }

            File.WriteAllLines(CurrentLogFilePath, new List<string>{ "Session started" });
        }

        public static void SetDay(string name)
        {
            DayName = name;
        }


        public static void DisplayPreProcessBasicSkip(string cameraName)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Preprocess Get Basic Info\t | Processed: 100%\t|", false);
        }

        public static void DisplayPreProcessBasic(string cameraName, int total, int processed)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Preprocess Get Basic Info\t | Processed: {(int)(processed / (total / 100.0))}%\t|");
        }

        public static void DisplayPreProcessValidationSkip(string cameraName, int valid, int corrupted)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Preprocess GetMediaInfo\t\t | Processed: 100%\t| Valid: {valid}\t| Corrupted: {corrupted}\t|", false);
        }

        public static void DisplayPreProcessValidation(string cameraName, int total, int processed, int corrupted)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Preprocess GetMediaInfo\t\t | Processed: {(int)(processed / (total / 100.0))}%\t| Valid: {processed - corrupted}\t| Corrupted: {corrupted}\t|");
        }

        public static void DisplayChangeFPSSkip(string cameraName)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Change FPS\t\t\t | Processed: 100%\t|", false);
        }

        public static void DisplayChangeFPS(string cameraName, int total, int processed, float currentFps)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Change FPS\t\t\t | Processed: {(int)(processed / (total / 100.0))}%\t| FPS: {currentFps}\t|");
        }

        public static void DisplayDateRecognitionSkip(string cameraName, int valid, int corrupted)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Date Recognition\t\t | Processed: 100%\t| Recognized: {valid}\t| Broken: {corrupted}\t|", false);
        }

        public static void DisplayDateRecognition(string cameraName, int total, int processed, int corrupted)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Date Recognition\t\t | Processed: {(int)(processed / (total / 100.0))}%\t| Recognized: {processed - corrupted}\t| Broken: {corrupted}\t|");
        }

        public static void DisplayCreatingScreenshotsSkip(string cameraName)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Creating Screenshots\t\t | Processed: 100%\t|", false);
        }

        public static void DisplayCreatingScreenshots(string cameraName, int total, int processed)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Creating Screenshots\t\t | Processed: {(int)(processed / (total / 100.0))}%\t|");
        }

        public static void DisplayRenderBlackFragmentsSkip(string cameraName)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Render black fragments\t | Processed: 100%\t|", false);
        }

        public static void DisplayRenderBlackFragments(string cameraName, int total, int processed)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Render black fragments\t | Processed: {(int)(processed / (total / 100.0))}%\t|");
        }


        public static void DisplayJoinFileSkip(string cameraName)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Concatenate\t\t\t | Processed: 100%\t|", false);
        }

        public static void DisplayJoinFile(string cameraName, int total, int processed)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| Concatenate\t\t\t | Processed: {(int)(processed / (total / 100.0))}%\t|");
        }


        public static void DisplayAdditionalInfo(string cameraName, string info)
        {
            AddText($"| {Program.CurrentJobTimer.Elapsed.ToString(@"hh\:mm\:ss")} | {cameraName}\t| {info} |", false);
        }


        public static void NextLine()
        {
            if (!OutputToConsole)
            {
                AddText($"new line", false);
            }
            else
            {
                Console.WriteLine();
            }
        }

        public static void AddText(string text, bool @override=true)
        {
            
            try
            {
                text = $"| {DayName} \t" + text;

                if (!OutputToConsole)
                {
                    if (@override)
                    {
                        var fileContent = File.ReadAllLines(CurrentLogFilePath).ToList();


                        var list = fileContent.SkipLast(1).ToList();

                        list.Add(text);
                        File.WriteAllLines(CurrentLogFilePath, list);
                    }
                    else
                    {
                        File.AppendAllLines(CurrentLogFilePath, new List<string>
                        {
                            text
                        });
                    }
                }
                else
                {

                    if (@override)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write(text);
                    }
                    else
                    {
                        Console.WriteLine(text);
                    }
                }

                
            }
            catch (Exception e)
            {
            }
        }
    }
}
