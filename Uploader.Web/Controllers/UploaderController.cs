using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Uploader.Web.Attributes;
using Uploader.Web.Helper;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Video;
using Uploader.Core.Models;

namespace Uploader.Web.Controllers
{
    [Route("uploader")]
    public class UploaderController : Controller
    {
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/uploadVideo")]
        public async Task<IActionResult> UploadVideo(string videoEncodingFormats = null, bool? sprite = null)
        {
            try
            {
                return Ok(new
                {
                    success = true, token = VideoManager.ComputeVideo(await GetFileToTemp(), videoEncodingFormats, sprite)
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception Upload Video : {0}", ex);
                return BadRequest(new
                {
                    errorMessage = ex.Message
                });
            }
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/uploadImage")]
        public async Task<IActionResult> OverlayImage()
        {
            try
            {
                return Ok(new
                {
                    success = true, token = OverlayManager.ComputeOverlay(await GetFileToTemp())
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception OverlayImage : {0}", ex);
                return BadRequest(new
                {
                    errorMessage = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("/uploadSubtitle")]
        public async Task<IActionResult> UploadSubtitle(string subtitle)
        {
            try
            {
                return Ok(new
                {
                    success = true, token = await SubtitleManager.ComputeSubtitle(subtitle)
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception ConvertSubtitle : {0}", ex);
                return BadRequest(new
                {
                    errorMessage = ex.Message
                });
            }
        }

        private async Task<string> GetFileToTemp()
        {
            // Copy file to temp location
            string sourceFilePath = TempFileManager.GetNewTempFilePath();

            FormValueProvider formModel;

            try
            {
                // Récupération du fichier
                using(System.IO.FileStream stream = System.IO.File.Create(sourceFilePath))
                {
                    formModel = await Request.StreamFile(stream);
                }                
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception Download File : {0}", ex);
                TempFileManager.SafeDeleteTempFile(sourceFilePath);
                throw;
            }

            try
            {
                ValueProviderResult fileName = formModel.GetValue("qqFileName");
                if(fileName.Length == 1)
                {
                    var extension = System.IO.Path.GetExtension(fileName.FirstValue);
                    if(!string.IsNullOrWhiteSpace(extension))
                    {
                        string newFilePath = System.IO.Path.ChangeExtension(sourceFilePath, extension);
                        System.IO.File.Move(sourceFilePath, newFilePath);
                        sourceFilePath = newFilePath;
                    }
                }
            }
            catch {}

            return sourceFilePath;
        }
    }
}
