using System;
using System.Threading.Tasks;
using IpfsUploader.Managers;
using IpfsUploader.Attributes;
using IpfsUploader.Helper;
using IpfsUploader.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using IpfsUploader.Daemons;

namespace IpfsUploader.Controllers
{
    [Route("uploadVideo")]
    public class VideoController : Controller
    {    
        static VideoController()
        {
            IpfsDaemon.Start();
            FFmpegDaemon.Start();
            SteemDaemon.Start();
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Post()
        {
            // Copy file to temp location
            string sourceFilePath = TempFileManager.GetNewTempFilePath();

            try
            {
                // Récupération du fichier
                FormValueProvider formModel;
                using(var stream = System.IO.File.Create(sourceFilePath))
                {
                    formModel = await Request.StreamFile(stream);
                }

                Guid sourceToken = IpfsDaemon.QueueSourceFile(sourceFilePath, VideoFormat.F720p, VideoFormat.F480p);

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
        [Route("/getProgress/{token}")]
        public ActionResult GetProgress(Guid token)
        {
            FileItem fileItem = IpfsDaemon.GetFileItem(token);
            if(fileItem == null)
            {
                return BadRequest(new { errorMessage = "token not exist" });
            }

            return Json(new
            {
                sourceProgress = fileItem.IpfsAddProgress,
                sourceHash = fileItem.IpfsHash,
                nbPositionLeft = fileItem.VideoFile.NumInstance - IpfsDaemon.NbAddSourceDone - 1,
                errorMessage = fileItem.IpfsAddErrorMessage,
                lastTimeIpfs = fileItem.IpfsAddLastTimeProgressChanged
            });
        }
    }
}