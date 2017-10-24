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
    [Route("uploader")]
    public class UploaderController : Controller
    {    
        static UploaderController()
        {
            IpfsDaemon.Start();
            EncodeDaemon.Start();
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/upload")]
        public async Task<IActionResult> Upload(string videoEncodingFormats)
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

                // todo détecter type de fichier
                // si fichier entrant est vidéo
                if(true)
                {
                    var formats = videoEncodingFormats
                            .Split(',')
                            .Select(v => 
                            {
                                switch(v)
                                {
                                    case "720p": return VideoSize.F720p;
                                    case "480p": return VideoSize.F480p;
                                    default: return VideoSize.F720p;
                                }
                            })
                            .ToArray();

                    var fileContainer = new FileContainer(sourceFilePath, formats);

                    IpfsDaemon.QueueSourceFile(fileContainer);

                    // si encoding est demandé
                    foreach (FileItem file in fileContainer.EncodedFileItems)
                    {   
                        EncodeDaemon.Queue(file);
                    }                

                    // Retourner le guid
                    return Ok(new
                    {
                        success = true,
                        token = fileContainer.SourceFileItem.IpfsProgressToken
                    });
                }

                return BadRequest(new { errorMessage = "format de fichier non géré." });
            }
            catch(Exception ex)
            {
                TempFileManager.SafeDeleteTempFile(sourceFilePath);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public ActionResult GetProgressByToken(Guid token)
        {
            FileContainer fileContainer = IpfsDaemon.GetFileContainer(token);
            if(fileContainer == null)
            {
                return BadRequest(new { errorMessage = "token not exist" });
            }

            return GetResult(fileContainer);
        }

        [HttpGet]
        [Route("/getProgressByHash/{sourceHash}")]
        public ActionResult GetProgressByHash(string sourceHash)
        {
            FileContainer fileContainer = IpfsDaemon.GetFileContainer(sourceHash);
            if(fileContainer == null)
            {
                return BadRequest(new { errorMessage = "hash not exist" });
            }

            return GetResult(fileContainer);
        }

        private JsonResult GetResult(FileContainer fileContainer)
        {

            // todo si est de type video ?
            return Json(new
            {
                ipfsProgress = fileContainer.SourceFileItem.IpfsProgress,
                ipfsHash = fileContainer.SourceFileItem.IpfsHash,
                ipfsLastTimeProgress = fileContainer.SourceFileItem.IpfsLastTimeProgressChanged,
                ipfsErrorMessage = fileContainer.SourceFileItem.IpfsErrorMessage,
                ipfsPositionLeft = fileContainer.SourceFileItem.IpfsPositionInQueue - IpfsDaemon.CurrentPositionInQueue,

                EncodedVideos = fileContainer.EncodedFileItems.Select(e => 
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