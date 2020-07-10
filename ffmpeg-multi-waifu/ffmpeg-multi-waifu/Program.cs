using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

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

            Console.Write("Threads (2,3,4):");
            int threads = Convert.ToInt32(Console.ReadLine());
            ParallelOptions thr = new ParallelOptions
            {
                //MaxDegreeOfParallelism = Environment.ProcessorCount
                MaxDegreeOfParallelism = threads
            };

            Console.Write("Multiplier (1,2,3,4):");
            string scaleRatio = Console.ReadLine();

            string inputVideoExt = Path.GetExtension(inputVideo);
            string inputVideoName = Path.GetFileNameWithoutExtension(inputVideo);
            string inputVideoPath = Path.GetDirectoryName(inputVideo);

            string pngPreResize = (@"tmp_files\png_frames_default\");
            string pngPostResize = (@"tmp_files\png_frames_upscaled\");
            string outputAudio = (@"tmp_files\output_audio");
            int errFilesCounter = 0;

            Directory.CreateDirectory("tmp_files");
            Directory.CreateDirectory(pngPreResize);
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

            Dictionary<int, bool> threadsInUse = new Dictionary<int, bool>();
            for (int i = 0; i < threads; i++)
            {
                threadsInUse.Add(i, false);
            }

            //Step 3
            Parallel.For(0, length, thr, i =>
            {
                
                if (File.Exists(pngPostResize + files[i]))
                {
                    Console.WriteLine("File: {0}, Already exist (SKIP)", files[i]);
                    count++;
                    return;
                }
                while (true)
                {
                    int waifuExitCode = 1;
                    int thread = -1;
                    for (int j = 0; j < threads; j++)
                    {
                        
                        if (threadsInUse[j] == false)
                        {
                            thread = j;
                            //Console.WriteLine("Current thread: " + thread);
                            threadsInUse[thread] = true;
                            break;
                        }
                    }
                    if (thread == -1)
                    {
                        Thread.Sleep(2000);
                        continue;
                    }

                    Process waifu = new Process();
                    waifu.StartInfo.FileName = "waifu2x-caffe-cui.exe";
                    waifu.StartInfo.Arguments = @"-m noise_scale --noise_level 1 --scale_ratio " + scaleRatio + " -i " + pngPreResize + files[i] + " -o " + pngPostResize + files[i];
                    waifu.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    waifu.Start();
                    //threads = 0x01 << threads;
                    waifu.ProcessorAffinity = (IntPtr)Math.Pow(2, thread);
                    waifu.WaitForExit();
                    waifuExitCode = waifu.ExitCode;
                    waifu.Close();

                    if (waifuExitCode != 0)
                    {
                        errFilesCounter++;
                        Console.WriteLine("Thread: {0:D2}, File: {1}, Code: {2} (Error, not enough video memory? Retrying...)", thread, files[i], waifuExitCode);
                        threadsInUse[thread] = false;
                        continue;
                    }
                    else
                    {
                        count++;
                        Console.WriteLine("Thread: {0:D2}, File: {1}, Code: {2} (OK) [{3}/{4}]", thread, files[i], waifuExitCode, count, length);
                        threadsInUse[thread] = false;
                        break;
                    }
                }
            });
            Console.WriteLine("Finished. Repeats: " + errFilesCounter);
            Console.WriteLine(string.Format("Step 3: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));

            //Step 4
            ffmpeg.FfmpegProcessMerge(pngPostResize, outputAudio, inputVideoPath, inputVideoExt);
            Console.WriteLine(string.Format("Step 4: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
            Console.WriteLine("Output: " + inputVideoPath + @"\output" + inputVideoExt);

            //Step 5
            while (true) { 
                Console.Write("Remove tmp folder? [y/n] ");
                string key = Console.ReadLine();
                if (key == "y")
                {
                    Console.WriteLine("Removing \"tmp_files\\\"...");
                    Directory.Delete("tmp_files", true);
                    break;
                } else if (key == "n")
                {
                    break;
                }
            }

            timer.Stop();
            Console.WriteLine(string.Format("Finished. Time: {0:D2}:{1:D2}:{2:D2}", timer.Elapsed.Hours, timer.Elapsed.Minutes, timer.Elapsed.Seconds));
            Console.WriteLine("Press any key to close ffmpeg-multi-waifu.exe");
            Console.ReadKey(); 
        }
    }
}
