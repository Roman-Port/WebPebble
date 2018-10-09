using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WebPebble.Entities;
using WebPebble.Oauth;
using System.Linq;
using System.IO;

namespace WebPebble
{
    public delegate void HttpServiceDelegate(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject project);

    public struct HttpService
    {
        public HttpServiceDelegate code;
        public string pathname;
        public bool requiresAuth;
        public bool inProject;
    }

    class Program
    {
        public static WebPebbleConfig config;

        private static List<HttpService> services = new List<HttpService>();

        public static LiteDatabase database;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting WebPebble");
            //Set everything up
            //Get the config.
            string pathname = "E:/RPWS_Production/WebPebble/conf.json";
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                pathname = "/root/webpebble/conf.json"; //Assume this is my server. FWI: You aren't running as root. Oopsies.

            config = JsonConvert.DeserializeObject<WebPebbleConfig>(File.ReadAllText(pathname));
            //Get database
            database = new LiteDatabase(config.database_file);
            //Add services that are out of the project.
            AddService(false, Services.CreateProject.CreateProject.OnRequest, "/create", true);
            //Add services that are in the project.
            AddService(true, null, "/manage", true);
            AddService(true, Services.Projects.FileManager.OnRequest, "/media/", true);
            AddService(true, Services.Projects.FileList.ListFiles, "/media_list/", true);
            //Start
            MainAsync().GetAwaiter().GetResult();
        }

        public static Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            /* Main code happens here! */

            //Manage CORS by responding with the preflight header request.
            if(e.Request.Method.ToLower() == "options")
            {
                e.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                e.Response.Headers.Add("Access-Control-Allow-Headers", "*");
                QuickWriteToDoc(e, "Preflight OK");
                return null;
            }

            //Try to find a service to handle the request. Ew.

            //This is ugly and was written wayyyyy too quickly to be such a core part of the program.

            HttpService service = new HttpService();
            bool serviceFound = false;
            foreach (HttpService serv in services)
            {
                //Check if the pathname requested begins with this.
                bool projectRequest = e.Request.Path.ToString().Contains("/project");
                string requestPath = e.Request.Path.ToString().TrimEnd('/').Replace("/project","");
                //If this is a project request, we can remove the ID of the project.
                if(projectRequest && requestPath.Split('/').Length >=2)
                {
                    requestPath = requestPath.Substring(1+requestPath.TrimStart('/').IndexOf('/'));
                }
                string servicePath = serv.pathname.TrimEnd('/');
                if (requestPath.StartsWith(servicePath))
                {
                    //The request path starts with the service. If wildcard is true, this is okay.
                    //Check if this path is longer than the currently selected service.
                    bool isOlderLonger = false;
                    if (serviceFound)
                    {
                        isOlderLonger = service.pathname.TrimEnd('/').Length > servicePath.Length;
                    }
                    //If we're longer than the new one, use us.
                    if (!isOlderLonger)
                    {
                        //Check if this requires a project.
                        if(projectRequest == serv.inProject)
                        {
                            //This service is okay to use.
                            service = serv;
                            serviceFound = true;
                        }
                    }
                }
            }

            //If we did find a service, use it.
            if(serviceFound)
            {
                //Try to authenticate.
                E_RPWS_User user = null;
                if(e.Request.Cookies.ContainsKey("access-token"))
                {
                    user = Oauth.RpwsAuth.AuthenticateUser(e.Request.Cookies["access-token"]);
                }
                if(e.Request.Headers.ContainsKey("Authorization") && user == null)
                {
                    user = Oauth.RpwsAuth.AuthenticateUser(e.Request.Headers["Authorization"]);
                }
                //Check if you must be logged in to do this.
                if (service.requiresAuth && user == null)
                {
                    Program.QuickWriteToDoc(e, "you must be logged in to do that.");
                    return null;
                }
                //Get the project if there is one.
                WebPebbleProject proj = null;
                if(e.Request.Path.ToString().Contains("/project/"))
                {
                    //Get the project ID.
                    string id = e.Request.Path.ToString().Substring(e.Request.Path.ToString().IndexOf("/project/")+ "/project/".Length);
                    if(id.Contains("/"))
                    {
                        id = id.Substring(0, id.IndexOf('/'));
                        //Now, use this ID to load it from the database.
                        var collect = database.GetCollection<WebPebbleProject>("projects");
                        Console.WriteLine(id);
                        var projects = collect.Find(x => x.projectId == id && x.authorId == user.uuid);
                        if (projects.Count() == 1)
                            proj = projects.ToArray()[0];
                    } else
                    {
                        //Malformed URL path.
                        QuickWriteToDoc(e, "Malformed URL path.");
                    }
                    
                }
                //Run the code.
                try
                {
                    if(service.inProject == (proj==null))
                    {
                        //Requires a project, but none was found.
                        QuickWriteToDoc(e, "you don't own that project or it didn't exist");
                        return null;
                    }
                    service.code(e, user, proj);
                    
                } catch (Exception ex)
                {
                    //Error.
                    QuickWriteToDoc(e, "error "+ex.Message+" @ "+ex.StackTrace);
                }
            } else
            {
                QuickWriteToDoc(e, "no service was found there");
            }

            return null;
        }

        public static void AddService(bool project, HttpServiceDelegate del, string pathname, bool requiresAuth = true)
        {
            HttpService ser = new HttpService();
            ser.code = del;
            ser.pathname = pathname;
            ser.requiresAuth = requiresAuth;
            ser.inProject = project;
            services.Add(ser);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(OnHttpRequest);

        }

        public static void QuickWriteToDoc(Microsoft.AspNetCore.Http.HttpContext context, string content, string type = "text/html", int code = 200)
        {
            try
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
            } catch
            {

            }
        }

        public static void QuickWriteBytesToDoc(Microsoft.AspNetCore.Http.HttpContext context, byte[] content, string type = "text/html", int code = 200)
        {
            try
            {
                var response = context.Response;
                response.StatusCode = code;
                response.ContentType = type;

                //Load the template.
                var data = content;
                response.ContentLength = data.Length;
                response.Body.Write(data, 0, data.Length);
                //Console.WriteLine(html);
            }
            catch
            {

            }
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
