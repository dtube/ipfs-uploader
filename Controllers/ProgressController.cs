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
                return BadRequest(new { errorMessage = "sourceHash not exist" });

            return GetResult(fileContainer);
        }

        private JsonResult GetResult(FileContainer fileContainer)
        {
            switch (fileContainer.TypeContainer)
            {
                case TypeContainer.Video:
                    return Json(new
                    {
                        source = IpfsResultJson(fileContainer.SourceFileItem),
                        EncodedVideos = fileContainer.EncodedFileItems
                            .Select(e => 
                                new 
                                {
                                    encode = EncodeResultJson(e),
                                    ipfs = IpfsResultJson(e)
                                })
                            .ToArray()
                    });

                case TypeContainer.Image:
                    return Json(new
                    {
                        source = IpfsResultJson(fileContainer.SourceFileItem),
                        sprite = IpfsResultJson(fileContainer.SpriteFileItem),
                        overlay = IpfsResultJson(fileContainer.OverlayFileItem)
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
                positionInQueue = fileItem.IpfsPositionInQueue == 0 ? 999 : fileItem.IpfsPositionInQueue - IpfsDaemon.CurrentPositionInQueue,
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
                positionInQueue = fileItem.EncodePositionInQueue == 0 ? 999 : fileItem.EncodePositionInQueue - EncodeDaemon.CurrentPositionInQueue,
            };
        }
    }
}