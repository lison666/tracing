using AutoMapper;
using Lwaay.Tracing.Core;
using Lwaay.Tracing.Grpc.Collector;
using Lwaay.Tracing.Grpc.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lwaay.Tracing.Grpc
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache(opt =>
            {
                opt.ExpirationScanFrequency = TimeSpan.FromDays(1);
            });
            services.AddCollector();
            services.AddGrpc();
            services.AddAutoMapper(typeof(GrpcProfile),typeof(Elasticsearch.TracingElasticsearchProfile));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GrpcCollector>();
                endpoints.MapGrpcService<GrpcQuery>();               

            });
        }
    }
}
