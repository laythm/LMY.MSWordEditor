using LMY.MSWordEditor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Text;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add any services you need here
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseLMYMSWordEditor(o =>
        {
            o.PhysicalFolderPath = @"D:\FolderRoot";
            o.OnAuthentication = (string token, HttpContext httpContext) =>
            {
                //do the validation here
                //return false;
                return true;
            };
            o.OnError = (string error, HttpContext httpContext) =>
            {
               //handle errors here
            };
        });

        app.Run(async context => { });
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var host = new WebHostBuilder()
            .UseKestrel(options =>
            {
                options.ListenAnyIP(6000); 
            })
            .UseStartup<Startup>()
            .Build();

        host.Run();
    }
}
