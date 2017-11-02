using System;

namespace Uploader.Models
{
    public class FileItem
    {
        public static FileItem NewSourceVideoFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, sourceFilePath, true);
            fileItem.VideoSize = VideoSize.Source;
            return fileItem;
        }

        public static FileItem NewEncodedVideoFileItem(FileContainer fileContainer, VideoSize videoSize)
        {
            if(videoSize == VideoSize.Undefined)
                throw new InvalidOperationException("VideoSize inconnu");

            FileItem fileItem = new FileItem(fileContainer, null, false);
            fileItem.VideoSize = videoSize;
            return fileItem;
        }

        public static FileItem NewSpriteVideoFileItem(FileContainer fileContainer)
        {
            FileItem fileItem = new FileItem(fileContainer, null, false);
            fileItem.ModeSprite = true;
            fileItem.VideoSize = VideoSize.Source;
            return fileItem;
        }

        public static FileItem NewSourceImageFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, sourceFilePath, true);
            return fileItem;
        }

        public static FileItem NewAttachedImageFileItem(FileContainer fileContainer, string filePath)
        {
            FileItem fileItem = new FileItem(fileContainer, filePath, false);
            return fileItem;
        }

        private FileItem(FileContainer fileContainer, string filePath, bool isSource)
        {
            IsSource = isSource;
            FileContainer = fileContainer;
            FilePath = filePath;
        }

        public bool IsSource { get; }

        public long? FileSize { get; set; }

        public string FilePath
        { 
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;

                if(System.IO.File.Exists(_filePath))
                    FileSize = new System.IO.FileInfo(_filePath).Length;
            }
        }
        private string _filePath;

        public FileContainer FileContainer { get; }

        public bool ModeSprite { get; private set; }

        public bool WorkInProgress()
        {
            if(!string.IsNullOrWhiteSpace(IpfsErrorMessage))
                return false;
            if(!string.IsNullOrWhiteSpace(EncodeErrorMessage))
                return false;

            return string.IsNullOrWhiteSpace(IpfsHash);
        }


        public int? IpfsPositionInQueue { get; set; }

        public string IpfsHash { get; set; }

        private string _ipfsProgress;

        public string IpfsProgress
        {
            get
            {
                return _ipfsProgress;
            }

            set
            {
                _ipfsProgress = value;
                IpfsLastTimeProgressChanged = DateTime.UtcNow;
            }
        }

        public DateTime? IpfsLastTimeProgressChanged { get; set; }

        public string IpfsErrorMessage { get; set; }




        public VideoSize VideoSize { get; private set; }

        public int? EncodePositionInQueue { get; set; }

        private string _encodeProgress;

        public string EncodeProgress
        {
            get
            {
                return _encodeProgress;
            }

            set
            {
                _encodeProgress = value;
                EncodeLastTimeProgressChanged = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// in seconds
        /// </summary>
        /// <returns></returns>
        public int? VideoDuration { get; set; }

        public DateTime? EncodeLastTimeProgressChanged { get; set; }

        public string EncodeErrorMessage { get; set; }
    }
}