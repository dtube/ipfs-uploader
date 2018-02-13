using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;

namespace Uploader.Core.Models
{
    internal class FileContainer
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

            ProgressManager.RegisterProgress(fileContainer);
            return fileContainer;
        }

        public static FileContainer NewOverlayContainer(string originFilePath)
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Overlay, originFilePath);

            fileContainer.SourceFileItem = FileItem.NewSourceImageFileItem(fileContainer);
            fileContainer.OverlayFileItem = FileItem.NewOverlayImageFileItem(fileContainer);

            ProgressManager.RegisterProgress(fileContainer);
            return fileContainer;
        }

        public static FileContainer NewSubtitleContainer()
        {
            FileContainer fileContainer = new FileContainer(TypeContainer.Subtitle, null);
            fileContainer.SubtitleFileItem = FileItem.NewSubtitleFileItem(fileContainer);

            ProgressManager.RegisterProgress(fileContainer);
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
            return (DateTime.UtcNow - LastTimeProgressRequested).TotalSeconds > GeneralSettings.Instance.MaxGetProgressCanceled;
        }
        
        private IEnumerable<FileItem> GetAllFile()
        {
            if(SourceFileItem != null)
                yield return SourceFileItem;

            if(SpriteVideoFileItem != null)
                yield return SpriteVideoFileItem;

            if(EncodedFileItems != null)
                foreach (FileItem fileItem in EncodedFileItems)
                    yield return fileItem;

            if(OverlayFileItem != null)
                yield return OverlayFileItem;

            if(SubtitleFileItem != null)
                yield return SubtitleFileItem;
        }

        public void CancelAll(string message)
        {
            foreach (var item in GetAllFile())
            {
                item.Cancel(message);
            }
        }

        public void CleanFilesIfEnd()
        {
            if(!Finished())
                return;

            foreach (var item in GetAllFile())
            {
                TempFileManager.SafeDeleteTempFiles(item.FilesToDelete.ToArray());
            }

            TempFileManager.SafeDeleteTempFile(OriginFilePath);
        }

        public bool Finished()
        {
            return GetAllFile().All(f => f.Finished());
        }

        public DateTime LastActivityDateTime => Tools.Max(CreationDate, GetAllFile().Max(f => f.LastActivityDateTime));
    }
}