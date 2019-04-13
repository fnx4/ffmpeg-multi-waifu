using System;
using System.Diagnostics;

namespace ffmpeg_multi_waifu
{
    class Ffmpeg
    {
        public void FfmpegProcess(string mode, string input, string output)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            if (mode == "video")
            {
                ffmpeg.StartInfo.Arguments = " -i " + input + " -r 23.976 -f image2 " + output + "%06d.png";
            } else if (mode == "audio")
            {
                ffmpeg.StartInfo.Arguments = " -i " + input + " " + output + @"\audio.mp3";
            }
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();
            GC.Collect();
        }

        public void FfmpegProcess(string pngPostResize, string outputAudio, string inputVideoPath, string inputVideoExt)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            ffmpeg.StartInfo.Arguments = " -f image2 -framerate 23.976 -i " + pngPostResize + "%06d.png -i " + outputAudio + @"\audio.mp3 -r 23.976 -vcodec libx264 -crf 18 " + inputVideoPath + @"\output" + inputVideoExt;
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();
            GC.Collect();
        }
    }
}
