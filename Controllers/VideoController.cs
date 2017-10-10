using System;
using System.Collections;
using System.Collections.Concurrent;
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
using Newtonsoft.Json;

namespace IpfsUploader.Controllers
{
    [Route("uploadVideo")]
    public class VideoController : Controller
    {
        private static string _tempDirectoryPath = Path.GetTempPath();
        private static ConcurrentDictionary<string,string> ipfsProgresses = new ConcurrentDictionary<string,string>();

        private string sourceFileFullPath;
        private string sourceHash;
        private bool triggerSourceProgressEvent;
        private string hashOutput;
        private DateTime? lastTimeProgressSaved;


        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Post()
        {
            // Copy file to temp location
            sourceFileFullPath = Path.Combine(_tempDirectoryPath, Path.GetRandomFileName());

            try
            {
                // Récupération du fichier
                FormValueProvider formModel;
                using (var stream = System.IO.File.Create(sourceFileFullPath))
                {
                    formModel = await Request.StreamFile(stream);
                }
   
                // Ipfs add only hash
                IpfsGetHash(sourceFileFullPath);
                sourceHash = hashOutput;

                ipfsProgresses.TryAdd(sourceHash, "0.00%");
                Task task = Task.Run(() =>
                {
                    try
                    {
                        triggerSourceProgressEvent = true;
                        IpfsAdd(sourceFileFullPath);
                        UpdateSourceFileProgress("100.00%");
                        triggerSourceProgressEvent = false;

                        //encoding 1024
                        //ipfs add 1024
                        //encoding 720
                        //ipfs add 720
                        //encoding 480
                        //ipfs add 480
                        //steem update
                    }
                    catch(Exception e)
                    {
                        //log exception
                    }
                    finally
                    {
                        SafeCleanSourceFile(sourceFileFullPath);
                    }                    

                    //supprimer le suivi après 10s
                    System.Threading.Thread.Sleep(10000);
                    string thisProgress;
                    ipfsProgresses.TryRemove(sourceHash, out thisProgress);
                });

                // Retourner le hash
                return Ok(new 
                    { 
                        success = true,
                        hash = sourceHash
                    });
            }
            catch(Exception ex)
            {
                SafeCleanSourceFile(sourceFileFullPath);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        private void SafeCleanSourceFile(string sourceFileFullPath)
        {
                try
                {
                    // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                    if(System.IO.File.Exists(sourceFileFullPath))
                        System.IO.File.Delete(sourceFileFullPath);
                }
                catch {}
        }

        [HttpGet]
        public ActionResult GetState(string sourceHash)
        {
            string output;
            if(!ipfsProgresses.TryGetValue(sourceHash, out output))
            {
                return BadRequest(new { errorMessage = "hash not exist"});
            }

            return Json(new { output });
        }

        private void IpfsGetHash(string sourceFileFullPath)
        {
            IpfsCmd(sourceFileFullPath, true);
        }

        private void IpfsAdd(string sourceFileFullPath)
        {
            IpfsCmd(sourceFileFullPath, false);
        }

        private void IpfsCmd(string sourceFileFullPath, bool onlyHash)
        {
            // Send to ipfs and return hash from ipfs
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "ipfs";
            processStartInfo.Arguments = "--local add " + sourceFileFullPath;
            if(onlyHash)
                processStartInfo.Arguments += " --only-hash";

            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            using(Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                hashOutput = null;
                lastTimeProgressSaved = null;
                process.OutputDataReceived += new DataReceivedEventHandler(IpfsOutputDataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(IpfsOutputDataReceived);

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                int timeout = 60 * 60 * 1000; //1h
                if(onlyHash)
                    timeout = 1 * 60 * 1000; //1 minute

                bool success = process.WaitForExit(timeout);
                if(!success)
                {
                    throw new InvalidOperationException("Le fichier n'a pas pu etre envoyé à ipfs en moins de 1 heure.");
                }

                if(process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Le fichier n'a pas pu etre envoyé à ipfs, erreur " + process.ExitCode + "." );
                }
            }
        }

        private void IpfsOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            if(output.StartsWith("added "))
            {
                hashOutput = output.Split(' ')[1];
            }
            else
            {
                if(triggerSourceProgressEvent)
                {                
                    if(lastTimeProgressSaved != null && (DateTime.Now - lastTimeProgressSaved.Value).TotalMilliseconds < 500)
                        return;

                    //Console.WriteLine(sourceHash + " : " + output);

                    string newProgress = output.Substring(output.IndexOf('%') - 6, 7).Trim();
                    lastTimeProgressSaved = DateTime.Now;

                    UpdateSourceFileProgress(newProgress);
                }
            }
        }

        private void UpdateSourceFileProgress(string newProgress)
        {
            // mettre à jour la progression
            string currentProgress;
            ipfsProgresses.TryGetValue(sourceHash, out currentProgress);
            ipfsProgresses.TryUpdate(sourceHash, newProgress, currentProgress);
        }
    }
}