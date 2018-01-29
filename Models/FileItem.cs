using System;
using System.IO;

using Uploader.Managers.Common;
using Uploader.Managers.Video;

namespace Uploader.Models
{
    public class FileItem
    {
        public static FileItem NewSourceVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.OriginFilePath, TypeFile.SourceVideo);
            fileItem.VideoSize = VideoSize.Source;
            if(VideoSettings.GpuEncodeMode)
                fileItem.EncodeProcess = new ProcessItem();
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewEncodedVideoFileItem(FileContainer fileContainer, VideoSize videoSize)
        {
            if (videoSize == VideoSize.Undefined)
                throw new InvalidOperationException("VideoSize inconnu");

            FileItem fileItem = new FileItem(fileContainer, fileContainer.SourceFileItem.SourceFilePath, TypeFile.EncodedVideo);
            fileItem.VideoSize = videoSize;
            fileItem.EncodeProcess = new ProcessItem();
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewSpriteVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.OriginFilePath, TypeFile.SpriteVideo);
            fileItem.VideoSize = VideoSize.Source;
            fileItem.EncodeProcess = new ProcessItem();
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewSourceImageFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.OriginFilePath, TypeFile.SourceImage);
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewOverlayImageFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, fileContainer.SourceFileItem.SourceFilePath, TypeFile.OverlayImage);
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewSubtitleFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.SubtitleText);
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        private FileItem(FileContainer fileContainer, string sourceFilePath, TypeFile typeFile)
        {
            FileContainer = fileContainer;
            SourceFilePath = sourceFilePath;
            TypeFile = typeFile;
            switch(typeFile){
                case TypeFile.EncodedVideo:
                case TypeFile.SourceVideo:
                    TempFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".mp4");
                break;

                case TypeFile.SpriteVideo:
                    TempFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".jpeg");
                    break;

                case TypeFile.SourceImage:
                case TypeFile.OverlayImage:
                    TempFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".png");
                break;
            }
        }

        public TypeFile TypeFile
        {
            get;
        }

        public bool IsSource => TypeFile == TypeFile.SourceVideo || TypeFile == TypeFile.SourceImage;

        public DateTime LastActivityDateTime => Tools.Max(EncodeProcess?.LastActivityDateTime??DateTime.MinValue, IpfsProcess?.LastActivityDateTime??DateTime.MinValue);

        public long? FileSize
        {
            get;
            private set;
        }

        public string OutputFilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;

                if (_filePath != null && File.Exists(_filePath))
                    FileSize = new FileInfo(_filePath).Length;
            }
        }
        private string _filePath;

        public string SourceFilePath { get; set; }

        public string TempFilePath { get; set; }

        public string VideoAacTempFilePath { get; set; }

        public FileContainer FileContainer
        {
            get;
        }

        public VideoSize VideoSize
        {
            get;
            private set;
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
        /// Encode audio de la source GPUMode
        /// Encode video des formats demandés (et audio pour CPU mode)
        /// Sprite création d'une video
        /// </summary>
        /// <returns></returns>
        public ProcessItem EncodeProcess
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
        
        public bool WorkInProgress()
        {
            if (IpfsProcess != null)
                if (IpfsProcess.CurrentStep == ProcessStep.Waiting || IpfsProcess.CurrentStep == ProcessStep.Started)
                    return true;

            if (EncodeProcess != null)
                if(EncodeProcess.CurrentStep == ProcessStep.Waiting || EncodeProcess.CurrentStep == ProcessStep.Started)
                    return true;

            return false;
        }

        public void CleanFiles()
        {
            try
            {
                if(!IsSource)
                {
                    TempFileManager.SafeDeleteTempFile(TempFilePath);
                    TempFileManager.SafeDeleteTempFile(OutputFilePath);
                }

                // vérification si on peut supprimer le fichier source
                if (!FileContainer.WorkInProgress())
                {
                    TempFileManager.SafeDeleteTempFile(FileContainer.OriginFilePath);
                    TempFileManager.SafeDeleteTempFile(FileContainer.SourceFileItem.SourceFilePath);
                    TempFileManager.SafeDeleteTempFile(FileContainer.SourceFileItem.VideoAacTempFilePath);
                    TempFileManager.SafeDeleteTempFile(FileContainer.SourceFileItem.TempFilePath);
                    TempFileManager.SafeDeleteTempFile(FileContainer.SourceFileItem.OutputFilePath);
                }
            }
            catch{}
        }

        public void SetEncodeErrorMessage(string message)
        {
            EncodeProcess.SetErrorMessage(message);
            CancelIpfs();
        }

        public void SetIpfsErrorMessage(string message)
        {
            IpfsProcess.SetErrorMessage(message);
            CleanFiles();
        }

        public void CancelEncode()
        {
            EncodeProcess.Cancel();
            CancelIpfs();
        }

        public void CancelIpfs()
        {
            IpfsProcess.Cancel();
            CleanFiles();
        }        
    }
}