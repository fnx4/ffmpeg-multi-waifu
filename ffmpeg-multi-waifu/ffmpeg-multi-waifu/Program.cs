using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ffmpeg_multi_waifu
{
    class Multi
    {
        static void Main(string[] args)
        {

            Console.Write("File:");
            string inputVideo = Console.ReadLine();

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

            System.IO.Directory.CreateDirectory("tmp_files");
            System.IO.Directory.CreateDirectory(pngPreResize);
            System.IO.Directory.CreateDirectory(pngPostResize);
            System.IO.Directory.CreateDirectory(outputAudio);

            if ((inputVideoExt != ".mkv") && (inputVideoExt != ".mp4") && (inputVideoExt != ".avi") && (inputVideoExt != ".webm"))
            {
                Console.Write("Should be mkv/mp4/avi/webm");
                Console.ReadKey();
                return;
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            //Step 1
            var ffmpegV = new Process();
            ffmpegV.StartInfo.FileName = "ffmpeg.exe";
            ffmpegV.StartInfo.Arguments = " -i " + inputVideo + " -r 23.976 -f image2 " + pngPreResize + "%06d.png";
            ffmpegV.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpegV.Start();
            ffmpegV.WaitForExit();
            ffmpegV.Close();
            Console.WriteLine("Step 1: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            //Step 2
            var ffmpegA = new Process();
            ffmpegA.StartInfo.FileName = "ffmpeg.exe";
            ffmpegA.StartInfo.Arguments = " -i " + inputVideo + " " + outputAudio + @"\audio.mp3";
            ffmpegA.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpegA.Start();
            ffmpegA.WaitForExit();
            ffmpegA.Close();
            Console.WriteLine("Step 2: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            DirectoryInfo dir = new DirectoryInfo(pngPreResize);
            FileInfo[] files = dir.GetFiles("*.png");

            Parallel.For(0, files.Length, thr, i =>
            {
                //Step 3
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
                        Console.WriteLine("File: {0}, Code: {1} (Error, retrying...)", files[i], waifuE);
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
            GC.WaitForPendingFinalizers();

            //Step 4
            var ffmpegO = new Process();
            ffmpegO.StartInfo.FileName = "ffmpeg.exe";
            ffmpegO.StartInfo.Arguments = " -f image2 -framerate 23.976 -i " + pngPostResize + "%06d.png -i " + outputAudio + @"\audio.mp3 -r 23.976 -vcodec libx264 -crf 18 " + inputVideoPath + @"\output" + inputVideoExt;
            ffmpegO.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpegO.Start();
            ffmpegO.WaitForExit();
            ffmpegO.Close();
            Console.WriteLine("Step 4: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);

            //Step 5
            Console.WriteLine("Removing \"tmp_files\\\"...");
            Directory.Delete("tmp_files", true);

            timer.Stop();
            Console.WriteLine("Output: " + inputVideoPath + @"\output" + inputVideoExt);
            Console.WriteLine("Finished. Time: " + timer.Elapsed.Minutes + "." + timer.Elapsed.Seconds);

            Console.ReadKey();
        }
    }
}
