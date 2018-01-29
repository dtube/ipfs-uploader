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

        public static FileContainer NewVideoContainer(string originFilePath, params VideoSize[] videoSizes)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Video, originFilePath);

            fileContainer.SourceFileItem = FileItem.NewSourceVideoFileItem(fileContainer);

            fileContainer.EncodedFileItems = new List<FileItem>();
            foreach (VideoSize videoSize in videoSizes)
            {
                fileContainer.EncodedFileItems.Add(FileItem.NewEncodedVideoFileItem(fileContainer, videoSize));
            }

            return fileContainer;
        }

        public void AddSpriteVideo()
        {
            SpriteVideoFileItem = FileItem.NewSpriteVideoFileItem(this);
        }

        public void DeleteSpriteVideo()
        {
            SpriteVideoFileItem = null;
        }

        public static FileContainer NewOverlayContainer(string originFilePath)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Overlay, originFilePath);

            fileContainer.SourceFileItem = FileItem.NewSourceImageFileItem(fileContainer);
            fileContainer.OverlayFileItem = FileItem.NewOverlayImageFileItem(fileContainer);

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

            LastTimeProgressRequested = DateTime.UtcNow;
        }

        public Guid ProgressToken
        {
            get;
        }

        public DateTime LastTimeProgressRequested
        {
            get;
            set;
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

        public IList<FileItem> EncodedFileItems
        {
            get;
            private set;
        }

        public FileItem OverlayFileItem
        {
            get;
            private set;
        }

        public bool WorkInProgress()
        {
            if (SourceFileItem.WorkInProgress())
                return true;
            if (SpriteVideoFileItem != null && SpriteVideoFileItem.WorkInProgress())
                return true;
            if (EncodedFileItems != null && EncodedFileItems.Any(f => f.WorkInProgress()))
                return true;
            if (OverlayFileItem != null && OverlayFileItem.WorkInProgress())
                return true;
            return false;
        }

        public DateTime LastActivityDateTime => Tools.Max(CreationDate
            , SourceFileItem?.LastActivityDateTime??DateTime.MinValue
            , SpriteVideoFileItem?.LastActivityDateTime??DateTime.MinValue
            , EncodedFileItems?.Max(e => e.LastActivityDateTime)??DateTime.MinValue
            , OverlayFileItem?.LastActivityDateTime??DateTime.MinValue
        );
    }
}