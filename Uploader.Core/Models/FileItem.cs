using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;

namespace Uploader.Core.Models
{
    internal class FileItem
    {
        public static FileItem NewSourceVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.OriginFilePath, TypeFile.SourceVideo);
            fileItem.VideoSize = VideoSize.Source;
            fileItem.InfoSourceProcess = new ProcessItem(fileItem);

            if(VideoSettings.Instance.GpuEncodeMode)
            {
                fileItem.AudioCpuEncodeProcess = new ProcessItem(fileItem);
                fileItem.VideoGpuEncodeProcess = new ProcessItem(fileItem);
            }

            if(IpfsSettings.Instance.AddVideoSource)
            {
                fileItem.AddIpfsProcess(fileItem.SourceFilePath);
            }

            return fileItem;
        }

        public static FileItem NewEncodedVideoFileItem(FileContainer fileContainer, VideoSize videoSize)
        {
            if (videoSize == VideoSize.Undefined)
                throw new InvalidOperationException("VideoSize inconnu");

            FileItem fileItem = new FileItem(fileContainer, fileContainer.SourceFileItem.SourceFilePath, TypeFile.EncodedVideo);
            fileItem.VideoSize = videoSize;
            if(!VideoSettings.Instance.GpuEncodeMode)
            {
                fileItem.AudioVideoCpuEncodeProcess = new ProcessItem(fileItem);                
            }
            fileItem.IpfsProcess = new ProcessItem(fileItem);
            return fileItem;
        }

        public static FileItem NewSpriteVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.OriginFilePath, TypeFile.SpriteVideo);
            fileItem.VideoSize = VideoSize.Source;
            fileItem.SpriteEncodeProcess = new ProcessItem(fileItem);
            fileItem.IpfsProcess = new ProcessItem(fileItem);
            return fileItem;
        }

        public static FileItem NewSourceImageFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.OriginFilePath, TypeFile.SourceImage);
            fileItem.IpfsProcess = new ProcessItem(fileItem);
            return fileItem;
        }

        public static FileItem NewOverlayImageFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.SourceFileItem.SourceFilePath, TypeFile.OverlayImage);
            fileItem.IpfsProcess = new ProcessItem(fileItem);
            return fileItem;
        }

        public static FileItem NewSubtitleFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.SubtitleText);
            fileItem.IpfsProcess = new ProcessItem(fileItem);
            return fileItem;
        }

        private FileItem(FileContainer fileContainer, string sourceFilePath, TypeFile typeFile)
        {
            FileContainer = fileContainer;
            FilesToDelete = new List<string>();
            CreationDate = DateTime.UtcNow;
            SetSourceFilePath(sourceFilePath);
            TypeFile = typeFile;
            switch(typeFile){
                case TypeFile.EncodedVideo:
                case TypeFile.SourceVideo:
                    SetTempFilePath(Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".mp4"));
                break;

                case TypeFile.SpriteVideo:
                    SetTempFilePath(Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg"));
                    break;

                case TypeFile.SourceImage:
                case TypeFile.OverlayImage:
                    SetTempFilePath(Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png"));
                break;
            }
        }

        public DateTime CreationDate
        {
            get;
        }

        public TypeFile TypeFile
        {
            get;
        }

        public bool IsSource => TypeFile == TypeFile.SourceVideo || TypeFile == TypeFile.SourceImage;

        public DateTime LastActivityDateTime => Tools.Max(CreationDate, GetAllProcess().Max(p => p.LastActivityDateTime));

        public long? FileSize { get; private set; }

        public string OutputFilePath { get { return _outputFilePath; } }
        private string _outputFilePath;

        public void SetOutputFilePath(string path)
        {
            _outputFilePath = path;
            FilesToDelete.Add(path);

            if (OutputFilePath != null && File.Exists(OutputFilePath))
                FileSize = new FileInfo(OutputFilePath).Length;
        }

        public string SourceFilePath { get { return _sourceFilePath; } }
        private string _sourceFilePath;

        public void SetSourceFilePath(string path)
        {
            _sourceFilePath = path;
            FilesToDelete.Add(path);
        }

        public string TempFilePath { get { return _tempFilePath; } }
        private string _tempFilePath;

        public void SetTempFilePath(string path)
        {
            _tempFilePath = path;
            FilesToDelete.Add(path);
        }

        public string VideoAacTempFilePath { get { return _videoAacTempFilePath; } }
        private string _videoAacTempFilePath;

        public void SetVideoAacTempFilePath(string path)
        {
            _videoAacTempFilePath = path;
            FilesToDelete.Add(path);
        }

        public FileContainer FileContainer
        {
            get;
        }

        public VideoSize VideoSize
        {
            get;
            private set;
        }

        public string VideoFrameRate
        {
            get;
            set;
        }

        public int? VideoBitRate
        {
            get;
            set;
        }

        public string VideoPixelFormat
        {
            get;
            set;
        }

        public int? VideoRotate
        {
            get;
            set;
        }

        public int? VideoNbFrame
        {
            get;
            set;
        }

        public string AudioCodec
        {
            get;
            set;
        }

        public string VideoCodec
        {
            get;
            set;
        }

        /// <summary>
        /// in seconds
        /// </summary>
        /// <returns></returns>
        public int? VideoDuration
        {
            get;
            set;
        }

        public int? VideoHeight
        {
            get;
            set;
        }

        public int? VideoWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Récupération durée et résolution de la vidéo source
        /// </summary>
        /// <returns></returns>
        public ProcessItem InfoSourceProcess
        {
            get;
            private set;
        }

        /// <summary>
        /// Encode audio de la source
        /// </summary>
        /// <returns></returns>
        public ProcessItem AudioCpuEncodeProcess
        {
            get;
            private set;
        }

        /// <summary>
        /// Encode video et audi des formats demandés par CPU
        /// </summary>
        /// <returns></returns>
        public ProcessItem AudioVideoCpuEncodeProcess
        {
            get;
            private set;
        }

        /// <summary>
        /// Encode video des formats demandés sans l'audio par GPU
        /// </summary>
        /// <returns></returns>
        public ProcessItem VideoGpuEncodeProcess
        {
            get;
            private set;
        }

        /// <summary>
        /// Sprite création d'une video
        /// </summary>
        /// <returns></returns>
        public ProcessItem SpriteEncodeProcess
        {
            get;
            private set;
        }

        public ProcessItem IpfsProcess
        {
            get;
            private set;
        }

        public string IpfsHash
        {
            get;
            set;
        }

        public IList<string> FilesToDelete { get; private set; }

        public bool SuccessGetSourceInfo()
        {
            return (VideoDuration??0) > 0 
            && (VideoWidth??0) > 0 
            && (VideoHeight??0) > 0 
            && (VideoBitRate??0) > 0
            && (VideoNbFrame??0) > 0
            && !string.IsNullOrWhiteSpace(VideoCodec)
            && !string.IsNullOrWhiteSpace(VideoPixelFormat)
            && !string.IsNullOrWhiteSpace(AudioCodec)
            && !string.IsNullOrWhiteSpace(VideoFrameRate);
        }

        public bool HasReachMaxVideoDurationForEncoding()
        {
            return VideoDuration.HasValue ? VideoDuration.Value > VideoSettings.Instance.MaxVideoDurationForEncoding : false;
        }

        public void AddIpfsProcess(string sourceFilePath)
        {
            if(IpfsProcess == null)
            {
                IpfsProcess = new ProcessItem(this);
                IpfsProcess.CantCascadeCancel = true;
                SetOutputFilePath(sourceFilePath);
            }
        }

        private IEnumerable<ProcessItem> GetAllProcess()
        {
            if(IpfsProcess != null)
                yield return IpfsProcess;

            if(AudioCpuEncodeProcess != null)
                yield return AudioCpuEncodeProcess;

            if(AudioVideoCpuEncodeProcess != null)
                yield return AudioVideoCpuEncodeProcess;

            if(VideoGpuEncodeProcess != null)
                yield return VideoGpuEncodeProcess;

            if(SpriteEncodeProcess != null)
                yield return SpriteEncodeProcess;
        }

        public void Cancel(string message)
        {
            foreach (ProcessItem item in GetAllProcess().Where(p => p.Unstarted() && !p.CantCascadeCancel))
            {
                item.CancelUnstarted(message);
            }
        }

        public bool Finished()
        {
            return GetAllProcess().All(p => p.Finished());
        }
    }
}