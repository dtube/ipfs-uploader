using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Uploader.Web.Attributes;
using Uploader.Web.Helper;

using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;
using Uploader.Core.Models;

namespace Uploader.Web.Controllers
{
    [Route("progress")]
    public class ProgressController : Controller
    {
        [HttpGet]
        [Route("/getStatus")]
        public JsonResult GetStatus(bool details = false)
        {
            return Json(ProgressManager.GetStats(details));
        }

        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public ActionResult GetProgressByToken(Guid token)
        {
            FileContainer fileContainer = ProgressManager.GetFileContainerByToken(token);
            if (fileContainer == null)
            {
                return BadRequest(new
                {
                    errorMessage = "token not exist"
                });
            }
            return GetResult(fileContainer);
        }

        [HttpGet]
        [Route("/getProgressBySourceHash/{sourceHash}")]
        public ActionResult GetProgressBySourceHash(string sourceHash)
        {
            FileContainer fileContainer = ProgressManager.GetFileContainerBySourceHash(sourceHash);
            if (fileContainer == null)
            {
                return BadRequest(new
                {
                    errorMessage = "hash not exist"
                });
            }
            return GetResult(fileContainer);
        }

        private JsonResult GetResult(FileContainer fileContainer)
        {
            switch (fileContainer.TypeContainer)
            {
                case TypeContainer.Video:
                    return Json(new
                    {
                        finished = fileContainer.Finished(),
                        sourceAudioCpuEncoding = AudioCpuEncodeResultJson(fileContainer.SourceFileItem),
                        sourceVideoGpuEncoding = VideoGpuEncodeResultJson(fileContainer.SourceFileItem),
                        ipfsAddSourceVideo = IpfsResultJson(fileContainer.SourceFileItem),
                        sprite = fileContainer.SpriteVideoFileItem == null ? null : (new
                        {
                            spriteCreation = SpriteResultJson(fileContainer.SpriteVideoFileItem),
                            ipfsAddSprite = IpfsResultJson(fileContainer.SpriteVideoFileItem)
                        }),
                        encodedVideos = fileContainer.EncodedFileItems
                            .Select(e =>
                                new
                                {
                                    encode = AudioVideoCpuEncodeResultJson(e),
                                    ipfsAddEncodeVideo = IpfsResultJson(e)
                                })
                            .ToArray()
                    });

                case TypeContainer.Overlay:
                    return Json(new
                    {
                        ipfsAddSource = IpfsResultJson(fileContainer.SourceFileItem),
                        ipfsAddOverlay = IpfsResultJson(fileContainer.OverlayFileItem)
                    });
            }

            Debug.WriteLine("Type container non géré " + fileContainer.TypeContainer);
            throw new InvalidOperationException("type container non géré");
        }

        private dynamic IpfsResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.IpfsProcess == null)
                return null;

            return new
            {
                progress = fileItem.IpfsProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                hash = fileItem.IpfsHash,
                lastTimeProgress = fileItem.IpfsProcess.LastTimeProgressChanged,
                errorMessage = fileItem.IpfsProcess.ErrorMessage,
                step = fileItem.IpfsProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.IpfsProcess, IpfsDaemon.Instance.CurrentPositionInQueue),
                fileSize = fileItem.FileSize
            };
        }

        private dynamic SpriteResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.SpriteEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.SpriteEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.SpriteEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.SpriteEncodeProcess.ErrorMessage,
                step = fileItem.SpriteEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.SpriteEncodeProcess, SpriteDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private dynamic AudioCpuEncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.AudioCpuEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.AudioCpuEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.AudioCpuEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.AudioCpuEncodeProcess.ErrorMessage,
                step = fileItem.AudioCpuEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.AudioCpuEncodeProcess, AudioCpuEncodeDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private dynamic AudioVideoCpuEncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.AudioVideoCpuEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.AudioVideoCpuEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.AudioVideoCpuEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.AudioVideoCpuEncodeProcess.ErrorMessage,
                step = fileItem.AudioVideoCpuEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.AudioVideoCpuEncodeProcess, AudioVideoCpuEncodeDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private dynamic VideoGpuEncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null || fileItem.VideoGpuEncodeProcess == null)
                return null;

            return new
            {
                progress = fileItem.VideoGpuEncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.VideoGpuEncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.VideoGpuEncodeProcess.ErrorMessage,
                step = fileItem.VideoGpuEncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.VideoGpuEncodeProcess, VideoGpuEncodeDaemon.Instance.CurrentPositionInQueue)
            };
        }

        private static int? Position(ProcessItem processItem, int daemonCurrentPositionInQueue)
        {
            if (processItem.CurrentStep != ProcessStep.Waiting)
                return null;

            return processItem.PositionInQueue - daemonCurrentPositionInQueue;
        }
    }
}