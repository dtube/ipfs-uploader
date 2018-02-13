using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Uploader.Core.Managers.Front;
using Uploader.Core.Managers.Ipfs;
using Uploader.Core.Managers.Video;

namespace Uploader.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration
        {
            get;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            Configuration.GetSection("General").Bind(GeneralSettings.Instance);
            Configuration.GetSection("Ipfs").Bind(IpfsSettings.Instance);
            Configuration.GetSection("Video").Bind(VideoSettings.Instance);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions{ ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });

            // A déclarer avant UseMvc
            string origins = Configuration.GetValue<string>("Front:CORS");
            Console.WriteLine("CORS Settings: " + origins);
            app.UseCors(
                options => options.WithOrigins(origins).AllowAnyMethod()
            );

            app.UseMvc();
            app.UseStaticFiles();
        }
    }
}