using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Uploader.Attributes;
using Uploader.Helper;
using Uploader.Managers.Front;
using Uploader.Managers.Ipfs;
using Uploader.Managers.Video;
using Uploader.Models;

namespace Uploader.Controllers
{
    [Route("progress")]
    public class ProgressController : Controller
    {
        [HttpGet]
        [Route("/getStatus")]
        public JsonResult GetStatus()
        {
            return Json(ProgressManager.GetStats());
        }

        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public ActionResult GetProgressByToken(Guid token)
        {
            FileContainer fileContainer = ProgressManager.GetFileContainerByToken(token);
            if (fileContainer == null)
                return BadRequest(new
                {
                    errorMessage = "token not exist"
                });

            return GetResult(fileContainer);
        }

        [HttpGet]
        [Route("/getProgressBySourceHash/{sourceHash}")]
        public ActionResult GetProgressBySourceHash(string sourceHash)
        {
            FileContainer fileContainer = ProgressManager.GetFileContainerBySourceHash(sourceHash);
            if (fileContainer == null)
            {
                fileContainer = ProgressManager.GetFileContainerByChildHash(sourceHash);
                if (fileContainer == null)
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
                                    encode = EncodeResultJson(e),
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
            if (fileItem == null)
                return null;

            return new
            {
                progress = fileItem.IpfsProcess.Progress,
                hash = fileItem.IpfsHash,
                lastTimeProgress = fileItem.IpfsProcess.LastTimeProgressChanged,
                errorMessage = fileItem.IpfsProcess.ErrorMessage,
                step = fileItem.IpfsProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.IpfsProcess, IpfsDaemon.CurrentPositionInQueue),
                fileSize = fileItem.FileSize
            };
        }

        private dynamic SpriteResultJson(FileItem fileItem)
        {
            if (fileItem == null)
                return null;

            return new
            {
                progress = fileItem.EncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.EncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.EncodeProcess.ErrorMessage,
                step = fileItem.EncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.EncodeProcess, SpriteDaemon.CurrentPositionInQueue)
            };
        }

        private dynamic EncodeResultJson(FileItem fileItem)
        {
            if (fileItem == null)
                return null;

            return new
            {
                progress = fileItem.EncodeProcess.Progress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.EncodeProcess.LastTimeProgressChanged,
                errorMessage = fileItem.EncodeProcess.ErrorMessage,
                step = fileItem.EncodeProcess.CurrentStep.ToString(),
                positionInQueue = Position(fileItem.EncodeProcess, EncodeDaemon.CurrentPositionInQueue)
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