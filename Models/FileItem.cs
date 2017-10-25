using System;

namespace Uploader.Models
{
    public class FileItem
    {
        public static FileItem NewSourceVideoFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, true);
            fileItem.FilePath = sourceFilePath;

            fileItem.VideoSize = VideoSize.Source;
            fileItem.EncodeProgress = "not available";
            fileItem.EncodeLastTimeProgressChanged = null;

            return fileItem;
        }

        public static FileItem NewEncodedVideoFileItem(FileContainer fileContainer, VideoSize videoSize)
        {
            if(videoSize == VideoSize.Undefined)
                throw new InvalidOperationException("VideoSize inconnu");

            FileItem fileItem = new FileItem(fileContainer, false);

            fileItem.VideoSize = videoSize;
            fileItem.EncodeProgress = "waiting...";
            fileItem.EncodeLastTimeProgressChanged = null;

            return fileItem;
        }

        public static FileItem NewSourceImageFileItem(FileContainer fileContainer, string sourceFilePath)
        {
            FileItem fileItem = new FileItem(fileContainer, true);
            fileItem.FilePath = sourceFilePath;
            fileItem.IpfsErrorMessage = "ipfs not asked";

            return fileItem;
        }

        public static FileItem NewAttachedImageFileItem(FileContainer fileContainer, string filePath)
        {
            FileItem fileItem = new FileItem(fileContainer, false);
            fileItem.FilePath = filePath;

            return fileItem;
        }

        private FileItem(FileContainer fileContainer, bool isSource)
        {
            IsSource = isSource;
            FileContainer = fileContainer;
            IpfsProgress = "waiting...";
            IpfsLastTimeProgressChanged = null;
        }

        public bool IsSource { get; }

        public string FilePath { get; set; }

        public FileContainer FileContainer { get; }

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

        public DateTime? IpfsLastTimeProgressChanged { get; private set; }

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

        public DateTime? EncodeLastTimeProgressChanged { get; private set; }

        public string EncodeErrorMessage { get; set; }
    }
}