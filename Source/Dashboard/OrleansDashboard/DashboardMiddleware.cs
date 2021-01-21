using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Orleans;
using OrleansDashboard.Implementation;
using OrleansDashboard.Model;

// ReSharper disable ConvertIfStatementToSwitchStatement

namespace OrleansDashboard
{
    public sealed class DashboardMiddleware
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static DashboardMiddleware()
        {
            Options.Converters.Add(new TimeSpanConverter());
        }

        private const int REMINDER_PAGE_SIZE = 50;
        private readonly IOptions<DashboardOptions> options;
        private readonly DashboardLogger logger;
        private readonly RequestDelegate next;
        private readonly IDashboardClient client;
        private readonly string session;

        public DashboardMiddleware(RequestDelegate next,
            IGrainFactory grainFactory,
            IOptions<DashboardOptions> options,
            DashboardLogger logger)
        {
            this.options = options;
            this.logger = logger;
            this.next = next;
            client = new DashboardClient(grainFactory);
            session = Guid.NewGuid().ToString("N");
        }

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;

            if (request.Path == "/" || string.IsNullOrEmpty(request.Path))
            {
                await WriteIndexFile(context);

                return;
            }
            if (request.Path == "/favicon.ico")
            {
                await WriteFileAsync(context, "favicon.ico", "image/x-icon");

                return;
            }
            if (request.Path == "/index.min.js")
            {
                await WriteFileAsync(context, "index.min.js", "application/javascript");

                return;
            }
            if (request.Path == "/Chart.min.js")
            {
                await WriteFileAsync(context, "Chart.min.js", "application/javascript");

                return;
            }
            if (request.Path == "/bootstrap.min.css")
            {
                await WriteFileAsync(context, "bootstrap.min.css", "text/css");

                return;
            }
            if (request.Path == "/AdminLTE.min.css")
            {
                await WriteFileAsync(context, "AdminLTE.min.css", "text/css");

                return;
            }
            if (request.Path == "/Chart.min.css")
            {
                await WriteFileAsync(context, "Chart.min.css", "text/css");

                return;
            }
            if (request.Path == "/font-awesome.min.css")
            {
                await WriteFileAsync(context, "font-awesome.min.css", "text/css");

                return;
            }
            if (request.Path == "/skin-purple.min.css")
            {
                await WriteFileAsync(context, "skin-purple.min.css", "text/css");

                return;
            }
            if (request.Path == "/fontawesome-webfont.woff2")
            {
                await WriteFileAsync(context, "fontawesome-webfont.woff2", "application/font-woff2");

                return;
            }
            if (request.Path == "/fontawesome-webfont.woff")
            {
                await WriteFileAsync(context, "fontawesome-webfont.woff", "application/font-woff");

                return;
            }
            if (request.Path == "/fontawesome-webfont.ttf")
            {
                await WriteFileAsync(context, "fontawesome-webfont.ttf", "application/font-ttf");

                return;
            }

            if (request.Path == "/version")
            {
                await WriteJson(context, new { version = typeof (DashboardMiddleware).Assembly.GetName().Version.ToString(), session = session });

                return;
            }

            StringValues sessionitem = string.Empty;
            if (request.Headers.TryGetValue("ingesttasksession", out sessionitem))
            {
                if (sessionitem != session)
                {
                    return;
                }
            }

            if (request.Path == "/DashboardCounters")
            {
                var result = await client.DashboardCounters();
                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path == "/ClusterStats")
            {
                var result = await client.ClusterStats();

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path == "/Reminders")
            {
                try
                {
                    var result = await client.GetReminders(1, REMINDER_PAGE_SIZE);

                    await WriteJson(context, result.Value);
                }
                catch
                {
                    // if reminders are not configured, the call to the grain will fail
                    await WriteJson(context, new ReminderResponse { Reminders = new ReminderInfo[0], Count = 0 });
                }

                return;
            }

            if (request.Path.StartsWithSegments("/Reminders", out var pageString1) && int.TryParse(pageString1.ToValue(), out var page))
            {
                try
                {
                    var result = await client.GetReminders(page, REMINDER_PAGE_SIZE);

                    await WriteJson(context, result.Value);
                }
                catch
                {
                    // if reminders are not configured, the call to the grain will fail
                    await WriteJson(context, new ReminderResponse { Reminders = new ReminderInfo[0], Count = 0 });
                }

                return;
            }

            if (request.Path.StartsWithSegments("/HistoricalStats", out var remaining))
            {
                var result = await client.HistoricalStats(remaining.ToValue());

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path.StartsWithSegments("/SiloProperties", out var address1))
            {
                var result = await client.SiloProperties(address1.ToValue());

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path.StartsWithSegments("/SiloStats", out var address2))
            {
                var result = await client.SiloStats(address2.ToValue());

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path.StartsWithSegments("/SiloCounters", out var address3))
            {
                var result = await client.GetCounters(address3.ToValue());

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path.StartsWithSegments("/GrainStats", out var grainName1))
            {
                var result = await client.GrainStats(grainName1.ToValue());

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path == "/TopGrainMethods")
            {
                var result = await client.TopGrainMethods();

                await WriteJson(context, result.Value);

                return;
            }

            if (request.Path == "/Trace")
            {
                await TraceAsync(context);

                return;
            }

            await next(context);
        }

        private static async Task WriteJson<T>(HttpContext context, T content)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/json";

            await using (var writer = new Utf8JsonWriter(context.Response.BodyWriter))
            {
                JsonSerializer.Serialize(writer, content, Options);
            }
        }

        private static async Task WriteFileAsync(HttpContext context, string name, string contentType)
        {
            var assembly = typeof(DashboardMiddleware).GetTypeInfo().Assembly;

            context.Response.StatusCode = 200;
            context.Response.ContentType = contentType;

            var stream = OpenFile(name, assembly);

            using (stream)
            {
                await stream.CopyToAsync(context.Response.Body);
            }
        }

        private async Task WriteIndexFile(HttpContext context)
        {
            var assembly = typeof(DashboardMiddleware).GetTypeInfo().Assembly;

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html";

            var stream = OpenFile("Index.html", assembly);

            using (stream)
            {
                var content = new StreamReader(stream).ReadToEnd();

                var basePath = string.IsNullOrWhiteSpace(options.Value.ScriptPath)
                    ? context.Request.PathBase.ToString()
                    : options.Value.ScriptPath;

                if (basePath != "/")
                {
                    basePath += "/";
                }

                content = content.Replace("{{BASE}}", basePath);
                content = content.Replace("{{HIDE_TRACE}}", options.Value.HideTrace.ToString().ToLowerInvariant());

                await context.Response.WriteAsync(content);
            }
        }

        private async Task TraceAsync(HttpContext context)
        {
            if (options.Value.HideTrace)
            {
                context.Response.StatusCode = 403;
                return;
            }

            var token = context.RequestAborted;

            try
            {
                using (var writer = new TraceWriter(logger, context))
                {
                    await writer.WriteAsync(@"
   ____       _                        _____            _     _                         _
  / __ \     | |                      |  __ \          | |   | |                       | |
 | |  | |_ __| | ___  __ _ _ __  ___  | |  | | __ _ ___| |__ | |__   ___   __ _ _ __ __| |
 | |  | | '__| |/ _ \/ _` | '_ \/ __| | |  | |/ _` / __| '_ \| '_ \ / _ \ / _` | '__/ _` |
 | |__| | |  | |  __/ (_| | | | \__ \ | |__| | (_| \__ \ | | | |_) | (_) | (_| | | | (_| |
  \____/|_|  |_|\___|\__,_|_| |_|___/ |_____/ \__,_|___/_| |_|_.__/ \___/ \__,_|_|  \__,_|

You are connected to the Orleans Dashboard log streaming service
").ConfigureAwait(false);

                    await Task.Delay(TimeSpan.FromMinutes(60), token).ConfigureAwait(false);

                    await writer.WriteAsync("Disconnecting after 60 minutes\r\n").ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static Stream OpenFile(string name, Assembly assembly)
        {
            var file = new FileInfo(name);

            return file.Exists
                ? file.OpenRead()
                : assembly.GetManifestResourceStream($"OrleansDashboard.{name}");
        }
    }
}