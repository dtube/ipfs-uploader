using System;
using System.IO;
using Uploader.Managers.Common;

namespace Uploader.Models
{
    public class FileItem
    {
        public static FileItem NewSourceVideoFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, sourceFilePath, TypeFile.SourceVideo);
            fileItem.VideoSize = VideoSize.Source;
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewEncodedVideoFileItem(FileContainer fileContainer, VideoSize videoSize)
        {
            if (videoSize == VideoSize.Undefined)
                throw new InvalidOperationException("VideoSize inconnu");

            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.EncodedVideo);
            fileItem.VideoSize = videoSize;
            fileItem.EncodeProcess = new ProcessItem();
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewSpriteVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.SpriteVideo);
            fileItem.VideoSize = VideoSize.Source;
            fileItem.EncodeProcess = new ProcessItem();
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewSourceImageFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, sourceFilePath, TypeFile.SourceImage);
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        public static FileItem NewOverlayImageFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.OverlayImage);
            fileItem.IpfsProcess = new ProcessItem();
            return fileItem;
        }

        private FileItem(FileContainer fileContainer, string filePath, TypeFile typeFile)
        {
            FileContainer = fileContainer;
            FilePath = filePath;
            TypeFile = typeFile;
        }

        public TypeFile TypeFile
        {
            get;
        }

        public bool IsSource => TypeFile == TypeFile.SourceVideo || TypeFile == TypeFile.SourceImage;

        public long? FileSize
        {
            get;
            private set;
        }

        public string FilePath
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
            if (IpfsProcess.CurrentStep == ProcessStep.Canceled || IpfsProcess.CurrentStep == ProcessStep.Error || IpfsProcess.CurrentStep == ProcessStep.Success)
                return false;

            if (EncodeProcess == null || EncodeProcess.CurrentStep == ProcessStep.Canceled || EncodeProcess.CurrentStep == ProcessStep.Error || EncodeProcess.CurrentStep == ProcessStep.Success)
                return false;

            return true;
        }

        public void CleanFiles()
        {
            try
            {
                if(!IsSource)
                    TempFileManager.SafeDeleteTempFile(FilePath);

                // vérification si on peut supprimer le fichier source
                if (!FileContainer.WorkInProgress())
                {
                    TempFileManager.SafeDeleteTempFile(FileContainer.SourceFileItem.FilePath);
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