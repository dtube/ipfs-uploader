using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Models;

namespace Uploader.Core.Managers.Video
{
    internal static class EncodeManager
    {
        public static bool AudioVideoCpuEncoding(FileItem fileItem)
        {
            try
            {
                FileItem sourceFile = fileItem.FileContainer.SourceFileItem;
                LogManager.AddEncodingMessage(LogLevel.Information, "SourceFilePath " + Path.GetFileName(sourceFile.SourceFilePath) + " -> " + fileItem.VideoSize, "Start AudioVideoCpuEncoding");
                fileItem.AudioVideoCpuEncodeProcess.StartProcessDateTime();

                string size = GetSize(fileItem.VideoSize, sourceFile.VideoWidth.Value, sourceFile.VideoHeight.Value);
                string arguments = $"-y -i {Path.GetFileName(sourceFile.SourceFilePath)}";
                if(sourceFile.VideoPixelFormat != "yuv420p")
                    arguments += " -pixel_format yuv420p";

                // si rotation 90 ou 270, inverser la largeur et la hauteur de la video
                if(sourceFile.VideoRotate.HasValue && (sourceFile.VideoRotate.Value == 90 || sourceFile.VideoRotate.Value == 270))
                {
                    string[] sizes = size.Split(':');
                    size = $"{sizes[1]}:{sizes[0]}";
                }

                arguments += $" -vf scale={size}";

                if(sourceFile.VideoCodec != "h264")
                    arguments += " -vcodec libx264";

                if(sourceFile.AudioCodec != "aac")
                    arguments += " -acodec aac -strict -2"; //-strict -2 pour forcer aac sur ubuntu
                else
                    arguments += " -acodec copy";

                arguments += $" {Path.GetFileName(fileItem.TempFilePath)}"; 

                var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.AudioVideoCpuEncodeProcess);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.Instance.EncodeTimeout);

                fileItem.ReplaceOutputPathWithTempPath();
                LogManager.AddEncodingMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileItem.OutputFilePath) + " / FileSize " + fileItem.FileSize + " / Format " + fileItem.VideoSize, "End AudioVideoCpuEncoding");
                fileItem.AudioVideoCpuEncodeProcess.EndProcessDateTime();

                return true;
            }
            catch (Exception ex)
            {
                string message = "Exception AudioVideoCpuEncoding : Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.AudioVideoCpuEncodeProcess.Progress;
                fileItem.AudioVideoCpuEncodeProcess.SetErrorMessage("Exception non gérée", message, ex);
                return false;
            }
        }

        public static bool AudioCpuEncoding(FileItem fileItem)
        {
            try
            {
                LogManager.AddEncodingMessage(LogLevel.Information, "SourceFilePath " + Path.GetFileName(fileItem.SourceFilePath), "Start AudioCpuEncoding");
                fileItem.AudioCpuEncodeProcess.StartProcessDateTime();

                if(fileItem.FileContainer.SourceFileItem.AudioCodec == "aac")
                {
                    fileItem.AudioCpuEncodeProcess.StartProcessDateTime();
                    fileItem.SetTempFilePath(fileItem.SourceFilePath);
                }
                else
                {
                    if(fileItem.VideoCodec.ToLower() == "vp8" || fileItem.VideoCodec.ToLower() == "vp9")
                        fileItem.SetTempFilePath(fileItem.TempFilePath.Replace(".mp4", ".mkv"));

                    // encoding audio de la source
                    string arguments = $"-y -i {Path.GetFileName(fileItem.SourceFilePath)} -vcodec copy -acodec aac -strict -2 {Path.GetFileName(fileItem.TempFilePath)}";
                    var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.AudioCpuEncodeProcess);
                    ffmpegProcessManager.StartProcess(arguments, VideoSettings.Instance.EncodeTimeout);
                }

                fileItem.SetVideoAacTempFilePath(fileItem.TempFilePath);
                LogManager.AddEncodingMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(fileItem.VideoAacTempFilePath) + " / FileSize " + fileItem.FileSize + " / Format " + fileItem.VideoSize, "End AudioCpuEncoding");
                fileItem.AudioCpuEncodeProcess.EndProcessDateTime();

                return true;
            }
            catch (Exception ex)
            {
                string message = "Exception AudioCpuEncoding : Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.AudioCpuEncodeProcess.Progress;
                fileItem.AudioCpuEncodeProcess.SetErrorMessage("Exception non gérée", message, ex);
                return false;
            }
        }

        public static bool VideoGpuEncoding(FileItem fileItem)
        {
            try
            {
                LogManager.AddEncodingMessage(LogLevel.Information, "SourceFilePath " + Path.GetFileName(fileItem.VideoAacTempFilePath) + " -> 1:N formats", "Start VideoGpuEncoding");
                fileItem.VideoGpuEncodeProcess.StartProcessDateTime();

                // encoding video 1:N formats
                string arguments = $"-y -hwaccel cuvid -vcodec h264_cuvid -vsync 0 -i {Path.GetFileName(fileItem.VideoAacTempFilePath)}";
                if(VideoSettings.Instance.NVidiaCard != "QuadroP5000")
                    arguments = arguments.Replace(" -hwaccel cuvid -vcodec h264_cuvid -vsync 0 ", " ");

                FileItem sourceFile = fileItem.FileContainer.SourceFileItem;
                foreach (FileItem item in fileItem.FileContainer.EncodedFileItems)
                {
                    string size = GetSize(item.VideoSize, fileItem.VideoWidth.Value, fileItem.VideoHeight.Value);
                    string maxRate = item.VideoSize.MaxRate;

                    if(sourceFile.VideoPixelFormat != "yuv420p")
                        arguments += " -pixel_format yuv420p";

                    // si rotation 90 ou 270, inverser la largeur et la hauteur de la video
                    if(sourceFile.VideoRotate.HasValue && (sourceFile.VideoRotate.Value == 90 || sourceFile.VideoRotate.Value == 270))
                    {
                        string[] sizes = size.Split(':');
                        size = $"{sizes[1]}:{sizes[0]}";
                    }

                    arguments += $" -vf scale_npp={size} -b:v {maxRate} -maxrate {maxRate} -bufsize {maxRate} -vcodec h264_nvenc -acodec copy {Path.GetFileName(item.TempFilePath)}";
                    if(VideoSettings.Instance.NVidiaCard != "QuadroP5000")
                        arguments = arguments.Replace("scale_npp=", "scale=");
                }

                var ffmpegProcessManager = new FfmpegProcessManager(fileItem, fileItem.VideoGpuEncodeProcess);
                ffmpegProcessManager.StartProcess(arguments, VideoSettings.Instance.EncodeTimeout);

                foreach (var item in fileItem.FileContainer.EncodedFileItems)
                {
                    item.ReplaceOutputPathWithTempPath();
                    LogManager.AddEncodingMessage(LogLevel.Information, "OutputFileName " + Path.GetFileName(item.OutputFilePath) + " / FileSize " + item.FileSize + " / Format " + item.VideoSize, "End VideoGpuEncoding");
                }
                fileItem.VideoGpuEncodeProcess.EndProcessDateTime();

                return true;
            }
            catch (Exception ex)
            {
                string message = "Exception VideoGpuEncoding : Video Duration " + fileItem.VideoDuration + " / FileSize " + fileItem.FileSize + " / Progress " + fileItem.VideoGpuEncodeProcess.Progress;
                fileItem.VideoGpuEncodeProcess.SetErrorMessage("Exception non gérée", message, ex);
                return false;
            }
        }

        private static string GetSize(VideoSize videoSize, int width, int height)
        {
            Tuple<int, int> finalSize = SizeHelper.GetSize(width, height, videoSize.Width, videoSize.Height);
            return $"{finalSize.Item1}:{finalSize.Item2}";
        }
    }
}
