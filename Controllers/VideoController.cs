using System;
using System.Threading.Tasks;
using IpfsUploader.Managers;
using IpfsUploader.Attributes;
using IpfsUploader.Helper;
using IpfsUploader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using IpfsUploader.Daemons;
using System.IO;
using System.Linq;

namespace IpfsUploader.Controllers
{
    [Route("video")]
    public class VideoController : Controller
    {    
        static VideoController()
        {
            IpfsDaemon.Start();
            EncodeDaemon.Start();
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/upload")]
        public async Task<IActionResult> Upload()
        {
            // Copy file to temp location
            string sourceFilePath = TempFileManager.GetNewTempFilePath();

            try
            {
                // Récupération du fichier
                FormValueProvider formModel;
                using(FileStream stream = System.IO.File.Create(sourceFilePath))
                {
                    formModel = await Request.StreamFile(stream);
                }

                //todo récupération format video demandé 720, 480, ...

                Guid sourceToken = IpfsDaemon.QueueSourceFile(sourceFilePath, VideoSize.F720p);//, VideoSize.F480p);

                // Retourner le guid
                return Ok(new
                {
                    success = true,
                    token = sourceToken
                });
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(sourceFilePath);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public ActionResult GetIpfsProgress(Guid token)
        {
            VideoFile videoFile = IpfsDaemon.GetVideoFile(token);
            if(videoFile == null)
            {
                return BadRequest(new { errorMessage = "token not exist" });
            }

            return GetResult(videoFile);
        }

        [HttpGet]
        [Route("/getProgressByHash/{sourceHash}")]
        public ActionResult GetEncodedVideosProgress(string sourceHash)
        {
            VideoFile videoFile = IpfsDaemon.GetVideoFile(sourceHash);
            if(videoFile == null)
            {
                return BadRequest(new { errorMessage = "hash not exist" });
            }

            return GetResult(videoFile);
        }

        private JsonResult GetResult(VideoFile videoFile)
        {
            return Json(new
            {
                ipfsProgress = videoFile.SourceFileItem.IpfsProgress,
                ipfsHash = videoFile.SourceFileItem.IpfsHash,
                ipfsLastTimeProgress = videoFile.SourceFileItem.IpfsLastTimeProgressChanged,
                ipfsErrorMessage = videoFile.SourceFileItem.IpfsErrorMessage,
                ipfsPositionLeft = videoFile.SourceFileItem.IpfsPositionInQueue - IpfsDaemon.CurrentPositionInQueue,

                EncodedVideos = videoFile.EncodedFileItems.Select(e => 
                    new 
                    {
                        encodeProgress = e.EncodeProgress,
                        encodeSize = e.VideoSize.ToString(),
                        encodeLastTimeProgress = e.EncodeLastTimeProgressChanged,
                        encodeErrorMessage = e.EncodeErrorMessage,
                        encodePositionLeft = e.EncodePositionInQueue == 0 ? 999 : e.EncodePositionInQueue - EncodeDaemon.CurrentPositionInQueue,

                        ipfsProgress = e.IpfsProgress,
                        ipfsHash = e.IpfsHash,
                        ipfsLastTimeProgress = e.IpfsLastTimeProgressChanged,
                        ipfsErrorMessage = e.IpfsErrorMessage,
                        ipfsPositionLeft = e.IpfsPositionInQueue == 0 ? 999 : e.IpfsPositionInQueue - IpfsDaemon.CurrentPositionInQueue,
                    }).ToArray()
            });
        }
    }
}