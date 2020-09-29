using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

namespace ffmpeg_multi_waifu
{
    class Multi
    {
        static void Main(string[] args)
        {
            if (!File.Exists("ffmpeg.exe"))
            {
                Console.Write("Error: ffmpeg.exe not found");
                Console.ReadKey();
                return;
            }

            if (!File.Exists("waifu2x-caffe-cui.exe"))
            {
                Console.Write("Error: waifu2x-caffe-cui.exe not found");
                Console.ReadKey();
                return;
            }

            Console.Write("File (absolute path / Drag and Drop)\r\n>");
            string inputVideo = Console.ReadLine();

            if (!File.Exists(inputVideo))
            {
                Console.Write("Error: file " + inputVideo + " not found");
                Console.ReadKey();
                return;
            }

            Console.Write("Threads limit (2, 3, 4...): ");
            int threads = Convert.ToInt32(Console.ReadLine());
            ParallelOptions thr = new ParallelOptions
            {
                MaxDegreeOfParallelism = threads
            };

            ParallelOptions thrHash = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Console.Write("Multiplier (2, 2.5, 3, 3.5, 4...): ");
            string scaleRatio = Console.ReadLine();

            if (!Double.TryParse(scaleRatio, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) || scaleRatio.Contains(","))
            {
                Console.Write(scaleRatio + " is not a number");
                Console.ReadKey();
                return;
            }

            string inputVideoExt = Path.GetExtension(inputVideo);
            string inputVideoName = Path.GetFileNameWithoutExtension(inputVideo);
            string inputVideoPath = Path.GetDirectoryName(inputVideo);

            string pngPreResize = (@"tmp_files\png_frames_default\");
            string pngSha512 = (@"tmp_files\sha512\");
            string pngPostResize = (@"tmp_files\png_frames_upscaled\");
            string outputAudio = (@"tmp_files\output_audio");
            int errFilesCount = 0;

            Directory.CreateDirectory("tmp_files");
            Directory.CreateDirectory(pngPreResize);
            Directory.CreateDirectory(pngSha512);
            Directory.CreateDirectory(pngPostResize);
            Directory.CreateDirectory(outputAudio);

            if ((inputVideoExt != ".mkv") && (inputVideoExt != ".mp4") && (inputVideoExt != ".avi") && (inputVideoExt != ".webm"))
            {
                Console.Write("Should be mkv/mp4/avi/webm");
                Console.ReadKey();
                return;
            }

            if (inputVideoExt == ".webm") {
                Console.WriteLine("input webm -> output mp4");
                inputVideoExt = ".mp4";
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            Ffmpeg ffmpeg = new Ffmpeg();

            //Step 1
            if (!Directory.EnumerateFiles(pngPreResize).Any())
            {
                ffmpeg.FfmpegProcessSplit("video", inputVideo, pngPreResize);
                Console.WriteLine(string.Format("Step 1: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
            }
            else
            {
                Console.WriteLine(string.Format("Step 1: Skip (directory " + pngPreResize + " not empty) {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
                System.Threading.Thread.Sleep(1000);
            }


            //Step 2
            if (!Directory.EnumerateFiles(outputAudio).Any())
            {
                ffmpeg.FfmpegProcessSplit("audio", inputVideo, outputAudio);
                if (!File.Exists(outputAudio + @"\audio.mp3"))
                {
                    Console.WriteLine("no audio detected");
                }
                Console.WriteLine(string.Format("Step 2: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));

            }
            else
            {
                Console.WriteLine(string.Format("Step 2: Skip (directory " + outputAudio + " not empty) {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
                System.Threading.Thread.Sleep(1000);
            }
            DirectoryInfo dir = new DirectoryInfo(pngPreResize);
            FileInfo[] files = dir.GetFiles("*.png");

            int length = files.Length;
            int count = 0;

            //Step 3.1
            Parallel.For(0, length, thrHash, i =>
            {
                ffmpeg.FfmpegProcessHash(files[i].Name, pngPreResize, pngSha512);
            });
            DirectoryInfo dirHash = new DirectoryInfo(pngSha512);
            FileInfo[] filesHash = dirHash.GetFiles("*.sha512");

            Dictionary<string, List<string>> hashes = new Dictionary<string, List<string>>();
            foreach (FileInfo file in filesHash)
            {
                string val = File.ReadAllText(pngSha512 + @"\" + file.Name).Trim();
                List<string> cList = null;
                if (hashes.ContainsKey(val))
                {
                    cList = hashes[val];
                }
                else
                {
                    cList = new List<string>();
                }
                cList.Add(file.Name);
                hashes[val] = cList;
            }

           //Step 3.2
           Parallel.For(0, length, thr, i =>
            {
                if (File.Exists(pngPostResize + files[i]))
                {
                    Console.WriteLine("File: {0}, Already exist (SKIP)", files[i]);
                    return;
                }

                string hash = File.ReadAllText(pngSha512 + @"\" + files[i] + ".sha512").Trim();
                if (hashes[hash].Count > 1)
                {
                    foreach (string dupe in hashes[hash])
                    {
                        String dupeName = dupe.Replace(".sha512", "");
                        if (File.Exists(pngPostResize + dupeName))
                        {
                            File.Copy(pngPostResize + @"\" + dupeName, pngPostResize + @"\" + files[i].Name);
                            count++;
                            Console.WriteLine("Thread ID: {0:D2}, File: {1}, DUPLICATE [{2}/{3}] {4}", Task.CurrentId, files[i], count, length, hash);
                            return;
                        }
                    }
                }

                int waifuExitCode = 1;
                while (waifuExitCode != 0)
                {
                    GC.Collect();
                    Process waifu = new Process();
                    waifu.StartInfo.FileName = "waifu2x-caffe-cui.exe";
                    waifu.StartInfo.Arguments = @"-m noise_scale --noise_level 1 --scale_ratio " + scaleRatio + " -i " + pngPreResize + files[i] + " -o " + pngPostResize + files[i];
                    waifu.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    waifu.Start();
                    waifu.WaitForExit();
                    waifuExitCode = waifu.ExitCode;
                    waifu.Close();

                    if (waifuExitCode != 0)
                    {
                        errFilesCount++;
                        Console.WriteLine("Thread ID: {0:D2}, File: {1}, Code: {2} (Error, not enough memory? Retrying...)", Task.CurrentId, files[i], waifuExitCode);
                        Thread.Sleep(250);
                    }
                    else
                    {
                        count++;
                        Console.WriteLine("Thread ID: {0:D2}, File: {1}, Code: {2} (OK) [{3}/{4}] {5}", Task.CurrentId, files[i], waifuExitCode, count, length, hash);
                    }
                }
            });
            Console.WriteLine("Finished. Repeats: " + errFilesCount);
            Console.WriteLine(string.Format("Step 3: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
            GC.Collect();


            //Step 4
            ffmpeg.FfmpegProcessMerge(pngPostResize, outputAudio, inputVideoPath, inputVideoExt);
            Console.WriteLine(string.Format("Step 4: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
            Console.WriteLine("Output: " + inputVideoPath + @"\output" + inputVideoExt);
            timer.Stop();

            //Step 5
            while (true) { 
                Console.Write("Remove tmp folder? [y/n] ");
                string key = Console.ReadLine();
                if (key == "y")
                {
                    Console.WriteLine("Removing \"tmp_files\\\"...");
                    Directory.Delete("tmp_files", true);
                    break;
                }
                else if (key == "n")
                {
                    break;
                }
            }

            Console.WriteLine(string.Format("Finished. Time: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
            Console.WriteLine("Press any key to close ffmpeg-multi-waifu.exe");
            Console.ReadKey(); 
        }
    }
}
