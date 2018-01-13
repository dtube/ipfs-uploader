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
            return fileItem;
        }

        public static FileItem NewEncodedVideoFileItem(FileContainer fileContainer, VideoSize videoSize)
        {
            if (videoSize == VideoSize.Undefined)
                throw new InvalidOperationException("VideoSize inconnu");

            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.EncodedVideo);
            fileItem.VideoSize = videoSize;
            return fileItem;
        }

        public static FileItem NewSpriteVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.SpriteVideo);
            fileItem.VideoSize = VideoSize.Source;
            return fileItem;
        }

        public static FileItem NewSourceImageFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, sourceFilePath, TypeFile.SourceImage);
            return fileItem;
        }

        public static FileItem NewOverlayImageFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, TypeFile.OverlayImage);
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
            set;
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

        public bool WorkInProgress()
        {
            if (!string.IsNullOrWhiteSpace(IpfsErrorMessage))
                return false;
            if (!string.IsNullOrWhiteSpace(EncodeErrorMessage))
                return false;

            return string.IsNullOrWhiteSpace(IpfsHash);
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

        public int? IpfsPositionInQueue
        {
            get;
            set;
        }

        public string IpfsHash
        {
            get;
            set;
        }

        private string _ipfsProgress;

        public string IpfsProgress
        {
            get
            {
                return _ipfsProgress;
            }

            set
            {
                bool changeLastTime = value == "0.00%" || _ipfsProgress != null;

                _ipfsProgress = value;

                if(value == null)
                    IpfsLastTimeProgressChanged = null;
                else if(changeLastTime)
                    IpfsLastTimeProgressChanged = DateTime.UtcNow;
            }
        }

        public DateTime? IpfsLastTimeProgressChanged
        {
            get;
            private set;
        }

        public string IpfsErrorMessage
        {
            get;
            set;
        }

        public VideoSize VideoSize
        {
            get;
            private set;
        }

        public int? EncodePositionInQueue
        {
            get;
            set;
        }

        private string _encodeProgress;

        public string EncodeProgress
        {
            get
            {
                return _encodeProgress;
            }

            set
            {
                bool changeLastTime = value == "0.00%" || _encodeProgress != null;

                _encodeProgress = value;

                if(value == null)
                    EncodeLastTimeProgressChanged = null;
                else if(changeLastTime)
                    EncodeLastTimeProgressChanged = DateTime.UtcNow;
            }
        }

        public DateTime? EncodeLastTimeProgressChanged
        {
            get;
            private set;
        }

        public string EncodeErrorMessage
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

        /// <summary>
        /// in seconds
        /// </summary>
        /// <returns></returns>
        public int? VideoHeight
        {
            get;
            set;
        }

        /// <summary>
        /// in seconds
        /// </summary>
        /// <returns></returns>
        public int? VideoWidth
        {
            get;
            set;
        }
    }
}