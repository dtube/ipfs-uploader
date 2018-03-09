using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal class FfProbeProcessManager
    {
        private FileItem _fileItem;

        public FfProbeProcessManager(FileItem fileItem)
        {
            if(fileItem == null)
                throw new ArgumentNullException(nameof(fileItem));

            _fileItem = fileItem;
        }

        public void FillInfo(int timeout)
        {
            // https://trac.ffmpeg.org/wiki/FFprobeTips
            string arguments = $"-v error -of default=nw=1 -show_entries stream_tags=rotate:format=size,duration:stream=index,codec_name,pix_fmt,height,width,duration,nb_frames,avg_frame_rate,bit_rate {Path.GetFileName(_fileItem.SourceFilePath)}";

            var process = new ProcessManager("ffprobe", arguments, LogManager.FfmpegLogger);
            process.Launch(timeout);
            
            foreach (string output in process.DataOutput.ToString().Split(Environment.NewLine))
            {
                try
                {
                    Fill(output);
                }
                catch{}
            }
        }

        private void Fill(string output)
        {
            if (string.IsNullOrWhiteSpace(output) || output.EndsWith("=N/A"))
                return;

            if(!_fileItem.VideoDuration.HasValue && output.StartsWith("duration="))
            {
                _fileItem.VideoDuration = Convert.ToInt32(output.Split('=')[1].Split('.')[0]);
            }
            else if(!_fileItem.VideoWidth.HasValue && output.StartsWith("width="))
            {
                _fileItem.VideoWidth = Convert.ToInt32(output.Split('=')[1]);
            }
            else if(!_fileItem.VideoHeight.HasValue && output.StartsWith("height="))
            {
                _fileItem.VideoHeight = Convert.ToInt32(output.Split('=')[1]);
            }
            else if(_fileItem.VideoCodec == null && output.StartsWith("codec_name="))
            {
                _fileItem.VideoCodec = output.Split('=')[1];
            }
            else if(_fileItem.VideoPixelFormat == null && output.StartsWith("pix_fmt="))
            {
                _fileItem.VideoPixelFormat = output.Split('=')[1];
            }
            else if(_fileItem.AudioCodec == null && output.StartsWith("codec_name="))
            {
                _fileItem.AudioCodec = output.Split('=')[1];
            }
            else if(!_fileItem.VideoBitRate.HasValue && output.StartsWith("bit_rate="))
            {
                _fileItem.VideoBitRate = Convert.ToInt32(output.Split('=')[1]);
            }
            else if(_fileItem.VideoFrameRate == null && output.StartsWith("avg_frame_rate="))
            {
                _fileItem.VideoFrameRate = output.Split('=')[1];
            }
            else if(!_fileItem.VideoNbFrame.HasValue && output.StartsWith("nb_frames="))
            {
                _fileItem.VideoNbFrame = Convert.ToInt32(output.Split('=')[1]);
            }
            else if(_fileItem.VideoRotate == null && output.StartsWith("TAG:rotate="))
            {
                _fileItem.VideoRotate = Convert.ToInt32(output.Split('=')[1]);
            }
        }
    }
}