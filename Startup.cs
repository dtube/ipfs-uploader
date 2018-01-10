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

using Microsoft.Extensions.Options;

namespace Uploader
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
            string origins;
            if (env.IsStaging() || env.IsProduction())
            {
                origins = "https://d.tube";
            }
            else
            {
                origins = "http://localhost:3000";
                app.UseDeveloperExceptionPage();
            }

            Console.WriteLine("CORS Settings: " + origins);

            app.UseForwardedHeaders(new ForwardedHeadersOptions{ ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });

            app.UseCors(
                options => options.WithOrigins(origins).AllowAnyMethod()
            );

            app.UseMvc();
            app.UseStaticFiles();
        }
    }
}