using System;
using Uploader.Core.Managers.Common;

namespace Uploader.Core.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //bool success0 = CheckManager.CheckIpfs();
            bool success1 = CheckManager.CheckFfmpeg();
            bool success2 = CheckManager.CheckFfprobe();
            bool success3 = CheckManager.CheckImageMagickComposite();
            bool success4 = CheckManager.CheckImageMagickConvert();
        }
    }
}
