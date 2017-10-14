using System;

namespace IpfsUploader.Models
{
    public class FileItem
    {
        public FileItem(string sourceFilePath, VideoFile videoFile)
        {
            FilePath = sourceFilePath;
            IpfsAddProgressToken = Guid.NewGuid();
            VideoFile = videoFile;            
            VideoFormat = VideoFormat.Source;
        }

        public FileItem(VideoFile videoFile, VideoFormat videoFormat)
        {
            VideoFile = videoFile;
            VideoFormat = videoFormat;
        }

        public string FilePath { get; set; }

        public VideoFile VideoFile { get; private set; }

        public bool WorkInProgress()
        {
            if(!string.IsNullOrWhiteSpace(IpfsAddErrorMessage))
                return false;
            if(!string.IsNullOrWhiteSpace(FFmpegErrorMessage))
                return false;

            return string.IsNullOrWhiteSpace(IpfsHash);
        }


        public Guid IpfsAddProgressToken { get; private set; }

        public string IpfsHash { get; set; }

        private string _ipfsAddProgress;

        public string IpfsAddProgress
        {
            get
            {
                return _ipfsAddProgress;
            }

            set
            {
                _ipfsAddProgress = value;
                IpfsAddLastTimeProgressChanged = DateTime.UtcNow;
            }
        }

        public DateTime? IpfsAddLastTimeProgressChanged { get; private set; }

        public string IpfsAddErrorMessage { get; set; }



        public VideoFormat VideoFormat { get; private set; }

        public string FFmpegErrorMessage { get; set; }
    }
}