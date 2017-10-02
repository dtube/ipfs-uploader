using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace IpfsUploader.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static Dictionary<int,string> datas = new Dictionary<int,string>();

        static ValuesController()
        {
            datas.Add(1, "values1");
            datas.Add(2, "values2");
        }

        // GET api/values
        [HttpGet]
        public Dictionary<int,string> Get()
        {
            return datas;
        }

        // GET api/values/1
        [HttpGet("{id}")]
        public string Get(int id)
        {
            if(datas.ContainsKey(id))
                return datas[id];

            return "undefined";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
            datas.Add(datas.Count, value);
        }

        // PUT api/values/1
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
            if(datas.ContainsKey(id))
                datas[id] = value;
        }

        // DELETE api/values/1
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            if(datas.ContainsKey(id))
                datas.Remove(id);
        }
    }
}
