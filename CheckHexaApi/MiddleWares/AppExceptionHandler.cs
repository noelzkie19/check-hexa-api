using System.Text.Encodings.Web;
using System.Text.Json;

namespace CheckHexaApi.MiddleWares
{
    public sealed class AppExceptionHandlerOptions
    {
        public bool ShowErrorDetails { get; set; }
    }

    public static partial class MiddlewareExtensions
    {
        public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app, AppExceptionHandlerOptions options)
        {
            return app.UseMiddleware<AppExceptionHandler>(options);
        }

        private sealed class AppExceptionHandler(RequestDelegate next, AppExceptionHandlerOptions options)
        {
            private readonly RequestDelegate next = next;
            private readonly AppExceptionHandlerOptions options = options;

            public async Task Invoke(HttpContext httpContext)
            {
                var path = httpContext.Request.Path.ToString();
                var idx = path.IndexOf("/?error=");
                if (idx >= 0)
                {
                    path = path[..idx];
                    httpContext.Response.Redirect(path);
                    return;
                }

                try
                {
                    await next(httpContext);
                }
                catch (Exception ex)
                {
                    if (path == "/signin-oidc" &&
                        ex.Message == "An error was encountered while handling the remote login.")
                    {
                        var exMsg = $"{ex.Message} {ex.InnerException?.Message}";
                        httpContext.Response.Redirect("/?error=" + UrlEncoder.Default.Encode(exMsg));
                        return;
                    }

                    //httpContext.Items.TryGetValue("UserInfo", out var UserInfo);

                    var errMessage = ex.Message;
                    if (options.ShowErrorDetails)
                    {
                        var errDetails = ex.ToString() + (ex.InnerException?.ToString() ?? string.Empty);
                        errMessage = $"{errMessage}. {errDetails}";
                    }
                    //var logId = await SCMApp.SaveLog(UserInfo as UserInfo, errMessage, errDetails, LogLevel.Error); // TODO

                    httpContext.Response.Clear();

                    var content = JsonSerializer.Serialize(new
                    {
                        Success = false,
                        Message = errMessage
                    });

                    await httpContext.Response.WriteAsync(content);

                    return;
                }
            }
        }
    }
}
