using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uploader.Managers.Common;
using Uploader.Managers.Front;

namespace Uploader.Models
{
    public class FileContainer
    {
        private static long nbInstance;

        public long NumInstance
        {
            get;
        }

        public DateTime CreationDate
        {
            get;
        }

        public string OriginFilePath { get; }

        public static FileContainer NewVideoContainer(string originFilePath, bool sprite, params VideoSize[] videoSizes)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Video, originFilePath);

            fileContainer.SourceFileItem = FileItem.NewSourceVideoFileItem(fileContainer);

            // si sprite demandé
            if (sprite)
            {
                fileContainer.SpriteVideoFileItem = FileItem.NewSpriteVideoFileItem(fileContainer);
            }

            var list = new List<FileItem>();
            foreach (VideoSize videoSize in videoSizes)
            {
                list.Add(FileItem.NewEncodedVideoFileItem(fileContainer, videoSize));
            }
            fileContainer.EncodedFileItems = list;

            return fileContainer;
        }

        public static FileContainer NewOverlayContainer(string originFilePath)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Overlay, originFilePath);

            fileContainer.SourceFileItem = FileItem.NewSourceImageFileItem(fileContainer);
            fileContainer.OverlayFileItem = FileItem.NewOverlayImageFileItem(fileContainer);

            return fileContainer;
        }

        public static FileContainer NewSubtitleContainer()
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Subtitle, null);
            fileContainer.SubtitleFileItem = FileItem.NewSubtitleFileItem(fileContainer);
            return fileContainer;
        }

        private FileContainer(TypeContainer typeContainer, string originFilePath)
        {
            nbInstance++;
            NumInstance = nbInstance;
            CreationDate = DateTime.UtcNow;

            TypeContainer = typeContainer;
            OriginFilePath = originFilePath;

            ProgressToken = Guid.NewGuid();
            ProgressManager.RegisterProgress(this);

            UpdateLastTimeProgressRequest();
        }

        public Guid ProgressToken
        {
            get;
        }

        public DateTime LastTimeProgressRequested
        {
            get;
            private set;
        }

        public void UpdateLastTimeProgressRequest()
        {
            LastTimeProgressRequested = DateTime.UtcNow;
        }

        public TypeContainer TypeContainer
        {
            get;
        }

        public FileItem SourceFileItem
        {
            get;
            private set;
        }

        public FileItem SpriteVideoFileItem
        {
            get;
            private set;
        }

        public IReadOnlyList<FileItem> EncodedFileItems
        {
            get;
            private set;
        }

        public FileItem OverlayFileItem
        {
            get;
            private set;
        }

        public FileItem SubtitleFileItem
        {
            get;
            private set;
        }

        public bool MustAbort()
        {
            return (DateTime.UtcNow - LastTimeProgressRequested).TotalSeconds > FrontSettings.MaxGetProgressCanceled;
        }
        
        public void CancelAll(string message)
        {
            SourceFileItem.Cancel(message);
            SpriteVideoFileItem?.Cancel(message);
            if(EncodedFileItems != null)
            {
                foreach (FileItem fileItem in EncodedFileItems)
                {
                    fileItem.Cancel(message);
                }
            }
            OverlayFileItem?.Cancel(message);
            SubtitleFileItem?.Cancel(message);
        }

        public void CleanFilesIfEnd()
        {
            if(!Finished())
                return;

            SourceFileItem.CleanFiles();
            SpriteVideoFileItem?.CleanFiles();
            if(EncodedFileItems != null)
            {
                foreach (FileItem fileItem in EncodedFileItems)
                {
                    fileItem.CleanFiles();
                }
            }
            OverlayFileItem?.CleanFiles();
            SubtitleFileItem?.CleanFiles();

            TempFileManager.SafeDeleteTempFile(OriginFilePath);
        }

        public bool Finished()
        {
            if (!SourceFileItem.Finished())
                return false;
            if (SpriteVideoFileItem != null && !SpriteVideoFileItem.Finished())
                return false;
            if (EncodedFileItems != null && EncodedFileItems.Any(f => !f.Finished()))
                return false;
            if (OverlayFileItem != null && !OverlayFileItem.Finished())
                return false;
            if (SubtitleFileItem != null && !SubtitleFileItem.Finished())
                return false;
            return true;
        }

        public DateTime LastActivityDateTime => Tools.Max(CreationDate
            , SourceFileItem?.LastActivityDateTime??DateTime.MinValue
            , SpriteVideoFileItem?.LastActivityDateTime??DateTime.MinValue
            , EncodedFileItems?.Max(e => e.LastActivityDateTime)??DateTime.MinValue
            , OverlayFileItem?.LastActivityDateTime??DateTime.MinValue
            , SubtitleFileItem?.LastActivityDateTime??DateTime.MinValue
        );
    }
}