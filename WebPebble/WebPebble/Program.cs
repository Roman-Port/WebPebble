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
using System.Reflection;
using WebPebble.WebSockets;
using System.Net.WebSockets;
using System.Diagnostics;

namespace WebPebble
{
    public delegate Task HttpServiceDelegate(Microsoft.AspNetCore.Http.HttpContext e, E_RPWS_User user, WebPebbleProject project);

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

        private static Process qemuControllerProcess;

        public static LiteDatabase database;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting WebPebble ver 3");
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
            AddService(false, Services.Me.MeService.PollUserData, "/users/@me/", true);

            AddService(false, Services.LoginService.BeginLogin, "/login", false);
            AddService(false, Services.LoginService.FinishLogin, "/complete_login", false);
            //Add services that are in the project.
            AddService(true, Services.Projects.FileManager.OnRequest, "/media/", true);
            AddService(true, Services.Projects.FileManager.UploadFile, "/upload_media/", true);
            AddService(true, Services.Projects.FileManager.CreateFileRequest, "/create_empty_media/", true);
            AddService(true, Services.Projects.FileManager.AppInfoJson, "/appinfo.json", true);
            AddService(true, Services.Projects.FileManager.AppInfoJson_DeleteResource, "/appinfo.json/delete_resource", true);
            AddService(true, Services.Projects.FileManager.AppInfoJson_AddResource, "/appinfo.json/add_resource", true);
            AddService(true, Services.Projects.FileManager.OnProjSettingsRequest, "/settings/", true);
            AddService(true, Services.Projects.FileList.ListFiles, "/media_list/", true);
            AddService(true, Services.Projects.Compile.DoCompile, "/build/", true);
            AddService(true, Services.Projects.History.OnRequest, "/build_history/", true);
            AddService(true, Services.Projects.PbwMedia.OnRequest, "/pbw_media/", true);
            AddService(true, Services.Projects.FileManager.CheckIfIdentifierExists, "/check_identifier", true);
            AddService(true, Services.Projects.FileManager.ZipProjectDownload, "/zip", true);
            AddService(true, Services.Projects.FileManager.DeleteProject, "/delete_project", true);
            //Start the WebSocket server.
            WebSocketServer.StartServer();
            //Start YCMDs.
            WebPebble.WebSockets.ycmd.YcmdProcess.StartServer( WebSockets.ycmd.YcmdProcesses.Any, "ycm_extra_conf_sdk3.py");
            //Start QEMU
            Console.WriteLine("Starting QEMU controller...");
            qemuControllerProcess = Process.Start(new ProcessStartInfo
            {
                Arguments = config.qemu_controller_command_line,
                UseShellExecute = true,
                FileName = "/usr/bin/dotnet",
            });
            //Start
            MainAsync().GetAwaiter().GetResult();
        }

        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            /* Main code happens here! */
            e.Response.Headers.Add("Access-Control-Allow-Origin", "https://webpebble.get-rpws.com");

            if(e.Request.Headers.ContainsKey("origin"))
            {
                if(e.Request.Headers["origin"] == "https://webpebble.get-rpws.com")
                {
                    //Allow creds
                    e.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                }
            }

            //If this is a websocket, switch to that.
            if (e.WebSockets.IsWebSocketRequest)
            {
                //Determine the service this is accessing.

                string pathname = e.Request.Path.ToString().ToLower();
                switch(pathname)
                {
                    case "/device":
                        WebSocket deviceWs = await e.WebSockets.AcceptWebSocketAsync();
                        CloudPebbleDevice device = new CloudPebbleDevice();
                        await device.StartSession(e, deviceWs);
                        return;
                    case "/webpebble":
                        WebSocket webpebbleWs = await e.WebSockets.AcceptWebSocketAsync();
                        WebPebbleClient wc = new WebPebbleClient();
                        await wc.StartSession(e, webpebbleWs);
                        return;
                    default:
                        //Unknown. Abort.
                        break;
                }
            }

            //Manage CORS by responding with the preflight header request.
            if (e.Request.Method.ToLower() == "options")
            {
                e.Response.Headers.Add("Access-Control-Allow-Headers", "*");
                e.Response.Headers.Add("Access-Control-Allow-Methods", "GET, PUT, POST");
                await QuickWriteToDoc(e, "Preflight OK");
                return;
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
                //If the user was authorized, get the WebPebble data.
                if(user != null)
                {
                    //Get the collection for user data
                    var userDataCollection = database.GetCollection<WebPebbleUserData>("users");
                    //Try to find this user
                    WebPebbleUserData data = userDataCollection.FindOne(x => x.rpwsId == user.uuid);
                    if (data == null)
                    {
                        //We'll need to create data.
                        data = new WebPebbleUserData()
                        {
                            rpwsId = user.uuid,
                            theme = "dark_visualstudio.css"
                        };
                        data._id = userDataCollection.Insert(data);
                    }
                    user.webpebble_data = data;
                }
                //Check if you must be logged in to do this.
                if (service.requiresAuth && user == null)
                {
                    await WriteErrorText(e, "You are not logged in.", ErrorHttpCode.NotLoggedIn, 400);
                    return;
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
                        var projects = collect.Find(x => x.projectId == id && x.authorId == user.uuid);
                        if (projects.Count() == 1)
                            proj = projects.ToArray()[0];
                    } else
                    {
                        //Malformed URL path.
                        await QuickWriteToDoc(e, "Malformed URL path.");
                    }
                    
                }
                //Run the code.
                try
                {
                    if(service.inProject == (proj==null))
                    {
                        //Requires a project, but none was found.
                        await WriteErrorText(e, "You don't have access to this project, or it didn't exist.", ErrorHttpCode.BadOwnership, 400);
                        return;
                    }
                    await service.code(e, user, proj);
                    
                } catch (Exception ex)
                {
                    //Error.
                    await WriteErrorText(e, "error "+ex.Message+" @ "+ex.StackTrace, ErrorHttpCode.Unknown, 500);
                    return;
                }
            } else
            {
                await WriteErrorText(e, "Service not found.", ErrorHttpCode.ServiceNotFound, 404);
                return;
            }

            return;
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

        public static async Task WriteErrorText(Microsoft.AspNetCore.Http.HttpContext e,string message, ErrorHttpCode code, int httpCode = 400)
        {
            HttpJsonError d = new HttpJsonError
            {
                message = message,
                code = code,
                code_text = code.ToString()
            };
            await QuickWriteJsonToDoc(e, d, httpCode);
        }

        public enum ErrorHttpCode
        {
            NotLoggedIn,
            BadOwnership,
            ServiceNotFound,
            Unknown
        }

        class HttpJsonError
        {
            public string message;
            public ErrorHttpCode code;
            public string code_text;
        }

        public void Configure(IApplicationBuilder app, IApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            app.Run(OnHttpRequest);
        }

        private void OnShutdown()
        {
            //Shutdown the WebSocket server gracefully.
            //WebSockets.WebSocketServer.wssv.WaitTime = new TimeSpan(0, 0, 3);
            //WebSockets.WebSocketServer.wssv.Stop();
            //Console.WriteLine("Shutting down WS server...");
            WebPebble.WebSockets.ycmd.YcmdProcess.KillAll();

            //Shut down QEMU process.
            Console.WriteLine("Shutting down QEMU...");
            qemuControllerProcess.CloseMainWindow();
        }

        public static async Task QuickWriteToDoc(Microsoft.AspNetCore.Http.HttpContext context, string content, string type = "text/html", int code = 200)
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
                await response.Body.WriteAsync(data, 0, data.Length);
                //Console.WriteLine(html);
            } catch
            {

            }
        }

        public static async Task QuickWriteBytesToDoc(Microsoft.AspNetCore.Http.HttpContext context, byte[] content, string type = "text/html", int code = 200)
        {
            try
            {
                var response = context.Response;
                response.StatusCode = code;
                response.ContentType = type;

                //Load the template.
                var data = content;
                response.ContentLength = data.Length;
                await response.Body.WriteAsync(data, 0, data.Length);
                //Console.WriteLine(html);
            }
            catch
            {

            }
        }

        public static async Task QuickWriteJsonToDoc<T>(Microsoft.AspNetCore.Http.HttpContext context, T data, int code = 200)
        {
            await Program.QuickWriteToDoc(context, JsonConvert.SerializeObject(data), "application/json", code);
        }

        public static Task MainAsync()
        {
            //Set Kestrel up to get replies.
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Any;
                    options.Listen(addr, 80);
                    options.Listen(addr, 443, listenOptions =>
                    {
                        listenOptions.UseHttps(config.ssl_cert, "");
                    });

                })
                .UseStartup<Program>()
                .Build();

            return host.RunAsync();
        }
    }
}
