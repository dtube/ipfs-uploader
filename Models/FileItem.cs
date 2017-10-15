using System;

namespace IpfsUploader.Models
{
    public class FileItem
    {
        private FileItem(VideoFile videoFile)
        {
            VideoFile = videoFile;
            IpfsProgress = "waiting...";
            IpfsLastTimeProgressChanged = null;
        }

        public FileItem(VideoFile videoFile, string sourceFilePath) : this(videoFile)
        {
            FilePath = sourceFilePath;
            IpfsProgressToken = Guid.NewGuid();
    
            VideoSize = VideoSize.Source;
            EncodeProgress = "not available";
            EncodeLastTimeProgressChanged = null;
        }

        public FileItem(VideoFile videoFile, VideoSize videoSize) : this(videoFile)
        {
            VideoSize = videoSize;
            EncodeProgress = "waiting...";
            EncodeLastTimeProgressChanged = null;
        }


        public string FilePath { get; set; }

        public VideoFile VideoFile { get; private set; }

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