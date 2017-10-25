using System;
using System.Threading.Tasks;
using IpfsUploader.Managers;
using IpfsUploader.Attributes;
using IpfsUploader.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.IO;

namespace IpfsUploader.Controllers
{
    [Route("uploader")]
    public class UploaderController : Controller
    {
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/uploadVideo")]
        public async Task<IActionResult> UploadVideo(string videoEncodingFormats)
        {
            try
            {            
                return Ok(new { success = true, token = VideoManager.ComputeVideo(await GetFileToTemp(), videoEncodingFormats) });
            }
            catch(Exception ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        [Route("/uploadImage")]
        public async Task<IActionResult> UploadImage(bool? sprite = null, bool? overlay = null)
        {
            try
            {
                return Ok(new { success = true, token = ImageManager.ComputeImage(await GetFileToTemp(), sprite, overlay) });
            }
            catch(Exception ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        private async Task<string> GetFileToTemp()
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
                //var fileName = formModel.GetValue("qqFileName");
                return sourceFilePath;
            }
            catch
            {
                TempFileManager.SafeDeleteTempFile(sourceFilePath);
                throw;
            }
        }
    }
}