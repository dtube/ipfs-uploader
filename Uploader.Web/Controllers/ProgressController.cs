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
        public JsonResult GetStatus(bool details = false)
        {
            return Json(ProgressManager.GetStats(details));
        }

        [HttpGet]
        [Route("/getProgressByToken/{token}")]
        public ActionResult GetProgressByToken(Guid token)
        {
            var result = ProgressManager.GetFileContainerByToken(token);
            if (result == null)
            {
                return BadRequest(new
                {
                    errorMessage = "token not exist"
                });
            }
            return Json(result);
        }

        [HttpGet]
        [Route("/getProgressBySourceHash/{sourceHash}")]
        public ActionResult GetProgressBySourceHash(string sourceHash)
        {
            var result = ProgressManager.GetFileContainerBySourceHash(sourceHash);
            if (result == null)
            {
                return BadRequest(new
                {
                    errorMessage = "hash not exist"
                });
            }
            return Json(result);
        }
    }
}