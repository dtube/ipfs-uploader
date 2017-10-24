using System;

namespace IpfsUploader.Models
{
    public class FileItem
    {
        public FileItem(FileContainer fileContainer, string sourceFilePath) : this(fileContainer, true)
        {
            FilePath = sourceFilePath;
            IpfsProgressToken = Guid.NewGuid();

            VideoSize = VideoSize.Source;
            EncodeProgress = "not available";
            EncodeLastTimeProgressChanged = null;
        }

        public FileItem(FileContainer fileContainer, VideoSize videoSize) : this(fileContainer, false)
        {
            VideoSize = videoSize;
            EncodeProgress = "waiting...";
            EncodeLastTimeProgressChanged = null;
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

        public FileContainer FileContainer { get; private set; }

        public bool WorkInProgress()
        {
            if(!string.IsNullOrWhiteSpace(IpfsErrorMessage))
                return false;
            if(!string.IsNullOrWhiteSpace(EncodeErrorMessage))
                return false;

            return string.IsNullOrWhiteSpace(IpfsHash);
        }


        public int IpfsPositionInQueue { get; set; }

        public Guid IpfsProgressToken { get; private set; }

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

        public int EncodePositionInQueue { get; set; }

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