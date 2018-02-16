using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Uploader.Web.Attributes;
using Uploader.Web.Helper;

using Uploader.Core.Managers.Front;

namespace Uploader.Web.Controllers
{
    [Route("progress")]
    public class ProgressController : Controller
    {
        [HttpGet]
        [Route("/getStatus")]
        public IActionResult GetStatus(bool details = false)
        {
            return Ok(ProgressManager.GetStats(details));
        }

        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public IActionResult GetProgressByToken(Guid token)
        {
            dynamic result = ProgressManager.GetFileContainerByToken(token);
            if (result == null)
            {
                return BadRequest(new
                {
                    errorMessage = "token not exist"
                });
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("/getProgressBySourceHash/{sourceHash}")]
        public IActionResult GetProgressBySourceHash(string sourceHash)
        {
            dynamic result = ProgressManager.GetFileContainerBySourceHash(sourceHash);
            if (result == null)
            {
                return BadRequest(new
                {
                    errorMessage = "hash not exist"
                });
            }
            return Ok(result);
        }
    }
}