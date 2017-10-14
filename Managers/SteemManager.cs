using System;
using System.Diagnostics;
using System.IO;
using IpfsUploader.Models;

namespace IpfsUploader.Managers
{
    public class SteemManager
    {
        public static void Update(VideoFile videoFile)
        {
            try
            {
                //todo call steem
                Debug.WriteLine("steem update " + Path.GetFileName(videoFile.SourceFileItem.FilePath));
            }
            catch
            {

            }
        }
    }
}