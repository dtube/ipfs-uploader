using System;
using System.Collections.Generic;
using System.Linq;
using Uploader.Managers;

namespace Uploader.Models
{
    public class FileContainer
    {
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

        public static FileContainer NewImageContainer(string sourceFilePath)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Image);

            fileContainer.SourceFileItem = FileItem.NewSourceImageFileItem(fileContainer, sourceFilePath);

            return fileContainer;
        }

        public void SetSprite(string filePath)
        {
            SpriteFileItem = FileItem.NewAttachedImageFileItem(this, filePath);
        }

        public void SetOverlay(string filePath)
        {
            OverlayFileItem = FileItem.NewAttachedImageFileItem(this, filePath);
        }

        private FileContainer(TypeContainer typeContainer)
        { 
            TypeContainer = typeContainer;

            ProgressToken = Guid.NewGuid();
            ProgressManager.RegisterProgress(this);
        }

        public Guid ProgressToken { get; }

        public TypeContainer TypeContainer { get; }

        public FileItem SourceFileItem { get; private set; }

        public IList<FileItem> EncodedFileItems { get; private set; }


        public FileItem SpriteFileItem { get; private set; }

        public FileItem OverlayFileItem { get; private set; }


        public bool WorkInProgress()
        {
            if(SourceFileItem.WorkInProgress())
                return true;

            switch(TypeContainer)
            {
                case TypeContainer.Video:
                    return EncodedFileItems.Any(f => f.WorkInProgress());

                case TypeContainer.Image:
                    if(SpriteFileItem != null && SpriteFileItem.WorkInProgress())
                        return true;
                    if(OverlayFileItem != null && OverlayFileItem.WorkInProgress())
                        return true;
                    return false;

                default:
                    return false;
            }
        }
    }
}