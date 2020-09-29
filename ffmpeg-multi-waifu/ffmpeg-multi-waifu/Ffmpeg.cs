using System.Diagnostics;
using System.IO;

namespace ffmpeg_multi_waifu
{
    class Ffmpeg
    {
        private Process prepareProcess()
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg.exe";
            return ffmpeg;
        }

        private void startProcess(Process ffmpeg)
        {
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            ffmpeg.Close();
        }

        public void FfmpegProcessSplit(string mode, string input, string output)
        {
            Process ffmpeg = prepareProcess();
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
            startProcess(ffmpeg);
        }

        public void FfmpegProcessMerge(string pngPostResize, string outputAudio, string inputVideoPath, string inputVideoExt)
        {
            Process ffmpeg = prepareProcess();
            if (!File.Exists(outputAudio + @"\audio.mp3"))
            {
                ffmpeg.StartInfo.Arguments = " -f image2 -framerate 23.976 -i " + pngPostResize + "%06d.png -r 23.976 -vcodec libx264 -crf 18 " + inputVideoPath + @"\output" + inputVideoExt;
            }
            else
            {
                ffmpeg.StartInfo.Arguments = " -f image2 -framerate 23.976 -i " + pngPostResize + "%06d.png -i " + outputAudio + @"\audio.mp3 -r 23.976 -vcodec libx264 -crf 18 " + inputVideoPath + @"\output" + inputVideoExt;
            }
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            startProcess(ffmpeg);
        }

        public void FfmpegProcessHash(string frame, string pngPreResize, string pngSha512)
        {
            Process ffmpeg = prepareProcess();
            ffmpeg.StartInfo.Arguments = " -i " + pngPreResize + @"\" + frame + " -f hash -hash sha512 " + pngSha512 + @"\" + frame + ".sha512 -hide_banner -loglevel fatal -nostdin -n";
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startProcess(ffmpeg);
        }
    }
}
