using System;

namespace IpfsUploader.Models
{
    public class SourceVideoFile {
        public SourceVideoFile () {

        }

        public int Number { get; set; }

        public string SourceFileFullPath { get; set; }

        public string SourceHash { get; set; }

        public string Progress { get; set; }

        public Guid Token { get; set; }

        public DateTime? LastTimeProgressSaved { get; set; }
    }
}