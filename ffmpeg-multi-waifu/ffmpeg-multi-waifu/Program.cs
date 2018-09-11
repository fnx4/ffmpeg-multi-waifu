using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Linq;

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

            Console.Write("File (absolute path):");
            string inputVideo = Console.ReadLine();

            if (!File.Exists(inputVideo))
            {
                Console.Write("Error: file " + inputVideo + " not found");
                Console.ReadKey();
                return;
            }

            Console.Write("Threads (2,4):");
            int threads = Convert.ToInt32(Console.ReadLine());
            ParallelOptions thr = new ParallelOptions
            {
                //MaxDegreeOfParallelism = Environment.ProcessorCount
                MaxDegreeOfParallelism = threads
            };

            Console.Write("Multiplier (1,2,3):");
            string scaleRatio = Console.ReadLine();

            string inputVideoExt = Path.GetExtension(inputVideo);
            string inputVideoName = Path.GetFileNameWithoutExtension(inputVideo);
            string inputVideoPath = Path.GetDirectoryName(inputVideo);

            string pngPreResize = (@"tmp_files\png_frames_default\");
            string pngPostResize = (@"tmp_files\png_frames_upscaled\");
            string outputAudio = (@"tmp_files\output_audio");
            int errFiles = 0;

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

            //Step 1
            if (!Directory.EnumerateFiles(pngPreResize).Any())
            {
                var ffmpegV = new Process();
                ffmpegV.StartInfo.FileName = "ffmpeg.exe";
                ffmpegV.StartInfo.Arguments = " -i " + inputVideo + " -r 23.976 -f image2 " + pngPreResize + "%06d.png";
                ffmpegV.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                ffmpegV.Start();
                ffmpegV.WaitForExit();
                ffmpegV.Close();
                Console.WriteLine("Step 1: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
                GC.Collect();
            }
            else
            {
                Console.WriteLine("Step 1: Skip (directory " + pngPreResize + "  not empty) " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
                System.Threading.Thread.Sleep(2500);
            }

            //Step 2
            if (!Directory.EnumerateFiles(outputAudio).Any())
            {
                var ffmpegA = new Process();
                ffmpegA.StartInfo.FileName = "ffmpeg.exe";
                ffmpegA.StartInfo.Arguments = " -i " + inputVideo + " " + outputAudio + @"\audio.mp3";
                ffmpegA.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                ffmpegA.Start();
                ffmpegA.WaitForExit();
                ffmpegA.Close();
                Console.WriteLine("Step 2: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
                GC.Collect();
            }
            else
            {
                Console.WriteLine("Step 2: Skip (directory " + outputAudio + "  not empty) " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
                System.Threading.Thread.Sleep(2500);
            }
            DirectoryInfo dir = new DirectoryInfo(pngPreResize);
            FileInfo[] files = dir.GetFiles("*.png");

            Parallel.For(0, files.Length, thr, i =>
            {
                //Step 3
                if (File.Exists(pngPostResize + files[i]))
                {
                    Console.WriteLine("File: {0}, Already exist (SKIP)", files[i]);
                    return;
                }
                var waifuE = 1;
                while (waifuE != 0)
                {
                    var waifu = new Process();
                    waifu.StartInfo.FileName = "waifu2x-caffe-cui.exe";
                    waifu.StartInfo.Arguments = @"-m noise_scale --noise_level 1 --scale_ratio " + scaleRatio + " -i " + pngPreResize + files[i] + " -o " + pngPostResize + files[i];
                    waifu.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    waifu.Start();
                    waifu.WaitForExit();
                    waifuE = waifu.ExitCode;
                    waifu.Close();

                    if (waifuE != 0)
                    {
                        errFiles++;
                        Console.WriteLine("File: {0}, Code: {1} (Error, not enough memory? Retrying...)", files[i], waifuE);
                    }
                    else
                    {
                        Console.WriteLine("File: {0}, Code: {1} (OK)", files[i], waifuE);
                    }
                }
            });
            Console.WriteLine("Finished. Repeats: " + errFiles);
            Console.WriteLine("Step 3: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
            GC.Collect();

            //Step 4
            var ffmpegO = new Process();
            ffmpegO.StartInfo.FileName = "ffmpeg.exe";
            ffmpegO.StartInfo.Arguments = " -f image2 -framerate 23.976 -i " + pngPostResize + "%06d.png -i " + outputAudio + @"\audio.mp3 -r 23.976 -vcodec libx264 -crf 18 " + inputVideoPath + @"\output" + inputVideoExt;
            ffmpegO.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpegO.Start();
            ffmpegO.WaitForExit();
            ffmpegO.Close();
            Console.WriteLine("Step 4: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
            Console.WriteLine("Output: " + inputVideoPath + @"\output" + inputVideoExt);

            //Step 5
            Console.WriteLine("Press any key to remove tmp folder...");
            Console.ReadKey();
            Console.WriteLine("Removing \"tmp_files\\\"...");
            Directory.Delete("tmp_files", true);

            timer.Stop();
            Console.WriteLine("Finished. Time: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);

            Console.ReadKey(); 
        }
    }
}
