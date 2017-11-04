using System;
using System.Threading.Tasks;
using Uploader.Managers;
using Uploader.Attributes;
using Uploader.Helper;
using Uploader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Uploader.Daemons;
using System.IO;
using System.Linq;

namespace Uploader.Controllers
{
    [Route("progress")]
    public class ProgressController : Controller
    {
        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public ActionResult GetProgressByToken(Guid token)
        {
            FileContainer fileContainer = ProgressManager.GetFileContainerByToken(token);
            if(fileContainer == null)
                return BadRequest(new { errorMessage = "token not exist" });

            return GetResult(fileContainer);
        }

        [HttpGet]
        [Route("/getProgressBySourceHash/{sourceHash}")]
        public ActionResult GetProgressBySourceHash(string sourceHash)
        {
            FileContainer fileContainer = ProgressManager.GetFileContainerBySourceHash(sourceHash);
            if(fileContainer == null)
            {
                fileContainer = ProgressManager.GetFileContainerByChildHash(sourceHash);
                if(fileContainer == null)
                    return BadRequest(new { errorMessage = "hash not exist" });
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
                        sprite = new
                            {
                                spriteCreation = EncodeResultJson(fileContainer.SpriteVideoFileItem),
                                ipfsAddSprite = IpfsResultJson(fileContainer.SpriteVideoFileItem)
                            },
                        encodedVideos = fileContainer.EncodedFileItems
                            .Select(e => 
                                new 
                                {
                                    encode = EncodeResultJson(e),
                                    ipfsAddEncodeVideo = IpfsResultJson(e)
                                })
                            .ToArray()
                    });

                case TypeContainer.Image:
                    return Json(new
                    {
                        ipfsAddSource = IpfsResultJson(fileContainer.SourceFileItem),
                        ipfsAddOverlay = IpfsResultJson(fileContainer.OverlayFileItem)
                    });
            }

            throw new InvalidOperationException("type container non géré");
        }

        private dynamic IpfsResultJson(FileItem fileItem)
        {
            if(fileItem == null)
                return string.Empty;

            return new
            {
                progress = fileItem.IpfsProgress,
                hash = fileItem.IpfsHash,
                lastTimeProgress = fileItem.IpfsLastTimeProgressChanged,
                errorMessage = fileItem.IpfsErrorMessage,
                positionInQueue = Position(fileItem.IpfsPositionInQueue, IpfsDaemon.CurrentPositionInQueue),
                fileSize = fileItem.FileSize
            };
        }

        private dynamic EncodeResultJson(FileItem fileItem)
        {
            if(fileItem == null)
                return string.Empty;

            return new
            {
                progress = fileItem.EncodeProgress,
                encodeSize = fileItem.VideoSize.ToString(),
                lastTimeProgress = fileItem.EncodeLastTimeProgressChanged,
                errorMessage = fileItem.EncodeErrorMessage,
                positionInQueue = Position(fileItem.EncodePositionInQueue, EncodeDaemon.CurrentPositionInQueue)
            };
        }

        private static int? Position(int? positionInQueue, int currentPositionInQueue)
        {
            if(!positionInQueue.HasValue)
                return null;
            if(positionInQueue.Value < currentPositionInQueue)
                return null;
            if(positionInQueue.Value == currentPositionInQueue)
                return null;
            return positionInQueue.Value - currentPositionInQueue;
        }
    }
}