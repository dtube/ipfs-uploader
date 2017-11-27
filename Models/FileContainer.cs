using System;
using System.Collections.Generic;
using System.Linq;

using Uploader.Managers;

namespace Uploader.Models
{
    public class FileContainer
    {
        private static long nbInstance;

        public long NumInstance
        {
            get;
        }

        public static FileContainer NewVideoContainer(string sourceFilePath, params VideoSize[] videoSizes)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Video);

            fileContainer.SourceFileItem = FileItem.NewSourceVideoFileItem(fileContainer, sourceFilePath);

            fileContainer.EncodedFileItems = new List<FileItem>();
            foreach (VideoSize videoSize in videoSizes)
            {
                fileContainer.EncodedFileItems.Add(FileItem.NewEncodedVideoFileItem(fileContainer, videoSize));
            }

            return fileContainer;
        }

        public void SetSpriteVideo()
        {
            SpriteVideoFileItem = FileItem.NewSpriteVideoFileItem(this);
        }

        public static FileContainer NewImageContainer(string sourceFilePath)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Image);

            fileContainer.SourceFileItem = FileItem.NewSourceImageFileItem(fileContainer, sourceFilePath);

            return fileContainer;
        }

        public void SetOverlay(string filePath)
        {
            if (TypeContainer != TypeContainer.Image)
                throw new InvalidOperationException("operation overlay possible que sur image");

            OverlayFileItem = FileItem.NewAttachedImageFileItem(this, filePath);
        }

        private FileContainer(TypeContainer typeContainer)
        {
            nbInstance++;
            NumInstance = nbInstance;

            TypeContainer = typeContainer;

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
            set;
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

            switch (TypeContainer)
            {
                case TypeContainer.Video:
                    if (SpriteVideoFileItem != null && SpriteVideoFileItem.WorkInProgress())
                        return true;
                    return EncodedFileItems.Any(f => f.WorkInProgress());

                case TypeContainer.Image:
                    if (OverlayFileItem != null && OverlayFileItem.WorkInProgress())
                        return true;
                    return false;

                default:
                    return false;
            }
        }
    }
}