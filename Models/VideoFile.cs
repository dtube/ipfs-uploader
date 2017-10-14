using System;
using System.Collections.Generic;
using System.Linq;

namespace IpfsUploader.Models
{
    public class VideoFile
    {
        public static int NbTotalInstance { get; private set; }

        public VideoFile (string sourceFilePath, params VideoFormat[] videoFormats)
        {
            SourceFileItem = new FileItem(sourceFilePath, this);

            EncodedFileItems = new List<FileItem>();
            foreach (VideoFormat videoFormat in videoFormats)
            {   
                EncodedFileItems.Add(new FileItem(this, videoFormat));
            }

            NbTotalInstance++;
            NumInstance = NbTotalInstance;
        }

        public int NumInstance { get; private set; }

        public FileItem SourceFileItem { get; private set; }

        public IList<FileItem> EncodedFileItems { get; private set; }

        public bool WorkInProgress()
        {
            return EncodedFileItems.Any(f => f.WorkInProgress());
        }
    }
}