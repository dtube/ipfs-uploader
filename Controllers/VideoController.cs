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
using IpfsUploader.Models;
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

        private static ConcurrentQueue<SourceVideoFile> queueSourceFile = new ConcurrentQueue<SourceVideoFile>();
        private static ConcurrentDictionary<Guid, SourceVideoFile> ipfsProgresses = new ConcurrentDictionary<Guid, SourceVideoFile>();

        private static int totalTaskAdded = 0;
        private static int nbTaskDone = 0;

        private static Task daemon = null;

        static VideoController()
        {
            daemon = Task.Run(() =>
            {
                SourceVideoFile sourceVideoFile;
                while(true)
                {
                    if(!queueSourceFile.TryDequeue(out sourceVideoFile))
                    {
                        System.Threading.Thread.Sleep(1000);
                        continue;
                    }

                    try
                    {
                        IpfsAdd(sourceVideoFile);
                        sourceVideoFile.Progress = "100.00%";

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
                        sourceVideoFile.ErrorMessage = e.Message;
                    }
                    finally
                    {
                        nbTaskDone++;
                        SafeCleanSourceFile(sourceVideoFile.SourceFileFullPath);
                        //supprimer le suivi après 1h
                        Task taskClean = Task.Run(() =>
                        {
                            Guid token = sourceVideoFile.Token;
                            System.Threading.Thread.Sleep(60 * 60 * 1000); // 1h
                            SourceVideoFile thisSourceVideoFile;
                            ipfsProgresses.TryRemove(token, out thisSourceVideoFile);
                        });
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            });
        }

        private static SourceVideoFile currentSourceVideoFile;

        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Post()
        {
            var sourceVideoFile = new SourceVideoFile();
            // Copy file to temp location
            sourceVideoFile.SourceFileFullPath = Path.Combine(_tempDirectoryPath, Path.GetRandomFileName());

            try
            {
                // Récupération du fichier
                FormValueProvider formModel;
                using(var stream = System.IO.File.Create(sourceVideoFile.SourceFileFullPath))
                {
                    formModel = await Request.StreamFile(stream);
                }

                Guid token = Guid.NewGuid();
                totalTaskAdded++;
                sourceVideoFile.Token = token;
                sourceVideoFile.Progress = "0.00%";
                sourceVideoFile.Number = totalTaskAdded;

                ipfsProgresses.TryAdd(token, sourceVideoFile);
                queueSourceFile.Enqueue(sourceVideoFile);

                // Retourner le guid
                return Ok(new
                {
                    success = true,
                    token = token
                });
            }
            catch(Exception ex)
            {
                SafeCleanSourceFile(sourceVideoFile.SourceFileFullPath);
                return BadRequest(new { errorMessage = ex.Message });
            }
        }

        [HttpGet]
        [Route("/getProgress/{token}")]
        public ActionResult GetProgress(Guid token)
        {
            SourceVideoFile sourceVideoFile;
            if(!ipfsProgresses.TryGetValue(token, out sourceVideoFile))
            {
                return BadRequest(new { errorMessage = "token not exist" });
            }

            return Json(new
            {
                sourceProgress = sourceVideoFile.Progress,
                sourceHash = sourceVideoFile.SourceHash,
                nbPositionLeft = sourceVideoFile.Number - nbTaskDone - 1,
                errorMessage = sourceVideoFile.ErrorMessage,
                lastTimeIpfs = sourceVideoFile.LastTimeProgressSaved
            });
        }

        private static void SafeCleanSourceFile(string sourceFileFullPath)
        {
            try
            {
                // suppression du fichier temporaire, ne pas jeter d'exception en cas d'erreur
                if(System.IO.File.Exists(sourceFileFullPath))
                    System.IO.File.Delete(sourceFileFullPath);
            }
            catch {}
        }

        private static void IpfsAdd(SourceVideoFile sourceVideoFile)
        {
            currentSourceVideoFile = sourceVideoFile;

            // Send to ipfs and return hash from ipfs
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "ipfs";
            processStartInfo.Arguments = "--local add " + sourceVideoFile.SourceFileFullPath;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            using(Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                sourceVideoFile.SourceHash = null;
                sourceVideoFile.LastTimeProgressSaved = null;
                process.OutputDataReceived += new DataReceivedEventHandler(IpfsOutputDataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(IpfsOutputDataReceived);

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                int timeout = 60 * 60 * 1000; //1h

                bool success = process.WaitForExit(timeout);
                if(!success)
                {
                    throw new InvalidOperationException("Le fichier n'a pas pu être envoyé à ipfs en moins de 1 heure.");
                }

                if(process.ExitCode != 0)
                {
                    throw new InvalidOperationException("Le fichier n'a pas pu être envoyé à ipfs, erreur " + process.ExitCode + ".");
                }
            }
        }

        private static void IpfsOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            string output = e.Data;
            if(string.IsNullOrWhiteSpace(output))
                return;

            if(output.StartsWith("added "))
            {
                currentSourceVideoFile.SourceHash = output.Split(' ')[1];
            }
            else
            {
                if(currentSourceVideoFile.LastTimeProgressSaved != null && (DateTime.Now - currentSourceVideoFile.LastTimeProgressSaved.Value).TotalMilliseconds < 500)
                    return;

                Debug.WriteLine(currentSourceVideoFile.SourceFileFullPath + " : " + output);

                string newProgress = output.Substring(output.IndexOf('%') - 6, 7).Trim();
                currentSourceVideoFile.LastTimeProgressSaved = DateTime.Now;

                currentSourceVideoFile.Progress = newProgress;
            }
        }
    }
}