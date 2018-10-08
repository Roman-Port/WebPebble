using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WebPebble
{
    class Program
    {
        public static WebPebbleConfig config;


        static void Main(string[] args)
        {
            Console.WriteLine("Starting WebPebble");
            //Set everything up
            MainAsync().GetAwaiter().GetResult();
        }

        public static Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            /* Main code happens here! */
            QuickWriteToDoc(e, "Testing");

            return null;
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(OnHttpRequest);

        }

        public static void QuickWriteToDoc(Microsoft.AspNetCore.Http.HttpContext context, string content, string type = "text/html", int code = 200)
        {
            var response = context.Response;
            response.StatusCode = code;
            response.ContentType = type;

            //Load the template.
            string html = content;
            var data = Encoding.UTF8.GetBytes(html);
            response.ContentLength = data.Length;
            response.Body.Write(data, 0, data.Length);
            //Console.WriteLine(html);
        }

        public static void QuickWriteJsonToDoc<T>(Microsoft.AspNetCore.Http.HttpContext context, T data, int code = 200)
        {
            Program.QuickWriteToDoc(context, JsonConvert.SerializeObject(data), "application/json", code);
        }

        public static Task MainAsync()
        {
            //Set Kestrel up to get replies.
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Any;
                    options.Listen(addr, 80);
                    /*options.Listen(addr, 443, listenOptions =>
                    {
                        listenOptions.UseHttps(LibRpwsCore.config.ssl_cert_path, "");
                    });*/

                })
                .UseStartup<Program>()
                .Build();

            return host.RunAsync();
        }
    }
}
