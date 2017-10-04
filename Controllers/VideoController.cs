using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IpfsUploader.Attributes;
using IpfsUploader.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IpfsUploader.Controllers
{
    [Route("uploadVideo")]
    public class VideoController : Controller
    {
        private static string _tempDirectoryPath = Path.GetTempPath();

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Post()
        {
            // Copy file to temp location
            string randomTempFileName = Path.GetRandomFileName();
            string tempFileFullPath = Path.Combine(_tempDirectoryPath, randomTempFileName);

            try
            {
                FormValueProvider formModel;
                using (var stream = System.IO.File.Create(tempFileFullPath))
                {
                    formModel = await Request.StreamFile(stream);
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

            try
            {            
                // Send to ipfs and return hash from ipfs
                var info = new ProcessStartInfo("ipfs", "--local add " + tempFileFullPath);
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;
                using(Process process = Process.Start(info))
                {
                    bool success = process.WaitForExit(1 * 60 * 1000); // 1 minute pour envoyer à ipfs
                    if(!success)
                    {
                        return BadRequest(new { errorMessage = "Le fichier n'a pas pu etre envoyé à ipfs en moins de 1 minute." });
                    }

                    if(process.ExitCode != 0)
                    {
                        return BadRequest(new { errorMessage = "Le fichier n'a pas pu etre envoyé à ipfs, erreur " + process.ExitCode + "." });
                    }

                    // Récupérer le hash
                    string hash = process.StandardOutput.ReadToEnd().Split(' ')[1];
                    return Ok(new 
                        { 
                            success = true,
                            hash = hash 
                        });
                }
            }
            catch(Exception ex)
            {
                return BadRequest(new { errorMessage = ex.Message });
            }
            finally
            {
                try
                {
                    // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                    if(System.IO.File.Exists(tempFileFullPath))
                        System.IO.File.Delete(tempFileFullPath);
                }
                catch {}
            }
        }
    }
}
