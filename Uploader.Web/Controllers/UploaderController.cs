using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

using Uploader.Web.Attributes;
using Uploader.Web.Helper;

using Uploader.Core.Managers.Common;
using Uploader.Core.Managers.Front;

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
                LogManager.AddEncodingMessage(LogLevel.Critical, "Exception non gérée", "Exception", ex);
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
        public async Task<IActionResult> UploadImage()
        {
            try
            {
                return Ok(new
                {
                    success = true, token = ImageManager.ComputeImage(await GetFileToTemp())
                });
            }
            catch (Exception ex)
            {
                LogManager.AddImageMessage(LogLevel.Critical, "Exception non gérée", "Exception", ex);
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
                LogManager.AddSubtitleMessage(LogLevel.Critical, $"Exception ConvertSubtitle : {ex}", "Exception");
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
                LogManager.AddGeneralMessage(LogLevel.Critical, $"Exception Download File : {ex}", "Exception");
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
