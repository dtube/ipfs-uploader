using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uploader.Daemons;
using Uploader.Managers.Common;
using Uploader.Managers.Front;
using Uploader.Managers.Ipfs;
using Uploader.Models;

namespace Uploader.Managers.Video
{
    public class EncodeDaemon : BaseDaemon
    {
        public static EncodeDaemon Instance { get; private set; }

        static EncodeDaemon()
        {
            Instance = new EncodeDaemon();
            Instance.Start(VideoSettings.NbEncodeDaemon);
        }

        protected override void ProcessItem(FileItem fileItem)
        {
            // si le client a pas demandé le progress depuis plus de 20s, annuler l'opération
            if ((DateTime.UtcNow - fileItem.FileContainer.LastTimeProgressRequested).TotalSeconds > FrontSettings.MaxGetProgressCanceled)
            {
                fileItem.CancelEncode();
                LogManager.AddEncodingMessage("SourceFileName " + Path.GetFileName(fileItem.SourceFilePath) + " car dernier getProgress a dépassé 20s", "Annulation");
                return;

            }

            // si c'était l'encoding audio, faire l'encoding 1:N video à partir de SourceAudioAacItem
            if(VideoSettings.GpuEncodeMode)
            {
                // si encoding audio de la source
                if(string.IsNullOrWhiteSpace(fileItem.VideoAacTempFilePath))
                {
                    if (EncodeManager.CpuAudioEncodingOnly(fileItem)) // encoding Audio
                        Instance.Queue(fileItem); // ==> encoding videos
                }
                else
                {
                    // sinon c'était l'encoding video 1:N format
                    if (EncodeManager.GpuEncodingVideoOnly(fileItem)) // encoding videos par GPU
                    {                        
                        // rechercher le 480p pour le sprite
                        var video480p = fileItem.FileContainer.EncodedFileItems.FirstOrDefault(v => v.VideoSize == VideoSize.F480p);
                        if(video480p != null)
                        {
                            var newSourceFilePath = Path.ChangeExtension(TempFileManager.GetNewTempFilePath(), ".mp4");
                            File.Copy(video480p.OutputFilePath, newSourceFilePath);
                            fileItem.FileContainer.SpriteVideoFileItem.SourceFilePath = newSourceFilePath;
                            SpriteDaemon.Instance.Queue(fileItem.FileContainer.SpriteVideoFileItem, "Waiting sprite creation...");
                        }

                        foreach (var item in fileItem.FileContainer.EncodedFileItems)
                        {
                            IpfsDaemon.Instance.Queue(item);
                        }
                    }
                }
            }
            else
            {
                if (EncodeManager.CpuEncoding(fileItem))
                {
                    // rechercher le 480p pour le sprite
                    if(fileItem.VideoSize == VideoSize.F480p)
                        SpriteDaemon.Instance.Queue(fileItem.FileContainer.SpriteVideoFileItem, "Waiting sprite creation...");

                    IpfsDaemon.Instance.Queue(fileItem);
                }
            }
        }

        protected override void LogException(FileItem fileItem, Exception ex)
        {
            LogManager.AddEncodingMessage(ex.ToString(), "Exception non gérée");                        
            fileItem.SetEncodeErrorMessage("Exception non gérée");
        }

        public void Queue(FileItem fileItem)
        {
            base.Queue(fileItem, fileItem.EncodeProcess);
        }

        public void Queue(FileItem fileItem, string messageIpfs)
        {
            base.Queue(fileItem, fileItem.EncodeProcess);

            fileItem.IpfsProcess.SetProgress(messageIpfs, true);
        }
    }
}