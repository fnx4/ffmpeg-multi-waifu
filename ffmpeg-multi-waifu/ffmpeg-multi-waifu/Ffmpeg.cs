using System;
using System.Diagnostics;

namespace ffmpeg_multi_waifu
{
    class Ffmpeg
    {
        public void FfmpegProcessSplit(string mode, string input, string output)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";

            switch(mode)
            {
                case "video":
                    ffmpeg.StartInfo.Arguments = " -i " + input + " -r 23.976 -f image2 " + output + "%06d.png";
                    break;
                case "audio":
                    ffmpeg.StartInfo.Arguments = " -i " + input + " " + output + @"\audio.mp3";
                    break;
            }

            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();
        }

        public void FfmpegProcessMerge(string pngPostResize, string outputAudio, string inputVideoPath, string inputVideoExt)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            ffmpeg.StartInfo.Arguments = " -f image2 -framerate 23.976 -i " + pngPostResize + "%06d.png -i " + outputAudio + @"\audio.mp3 -r 23.976 -vcodec libx264 -crf 18 " + inputVideoPath + @"\output" + inputVideoExt;
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();
        }
    }
}
