using Microsoft.AspNetCore.Authentication;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace CheckHexaApi.MiddleWares
{
    /// <summary>
    /// Use Middleware for Cida Authentication
    /// </summary>
    public static partial class MiddlewareExtensions
    {
        /// <summary>
        /// Add Cida Authentication
        /// </summary>
        public static IServiceCollection AddCidaAuthentication(this IServiceCollection services, string authorityEndpoint)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services
                .AddAuthentication(static options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie("Cookies")
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = authorityEndpoint;
                    options.RequireHttpsMetadata = false;

                    options.ClientId = "js";
                    //options.ClientSecret = "K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=";
                    options.ClientSecret = "secret";
                    options.ResponseType = "id_token token";
                    options.SaveTokens = true;
                    options.Scope.Add("api1");
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.SaveTokens = true;
                    options.TokenValidationParameters.ValidateIssuer = false;
                    //options.Events.OnRedirectToIdentityProvider = OnRedirectToIdentityProvider;
                })
                .AddJwtBearer("jwt", options =>
                {
                    options.Authority = authorityEndpoint;
                    options.RequireHttpsMetadata = false;
                    options.Audience = "api1";
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.SaveToken = true;
                });

            return services;
        }

        internal static IServiceCollection AddCidaJwtAuthentication(this IServiceCollection services, string authorityEndpoint)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            services
                .AddAuthentication()
                .AddJwtBearer("jwt", options =>
                {
                    options.Authority = authorityEndpoint;
                    options.RequireHttpsMetadata = false;
                    options.Audience = "api1";
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.SaveToken = true;
                });

            return services;
        }
        /// <summary>
        /// Use Middleware for Cida Authentication
        /// </summary>
        public static IApplicationBuilder UseCidaAuthentication(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CidaAuthenticationMiddleware>();
        }

        private sealed class CidaAuthenticationMiddleware(RequestDelegate next)
        {
            private readonly RequestDelegate next = next;

            public async Task Invoke(HttpContext httpContext)
            {
                bool challenge;
                var request = httpContext.Request;

                var test = httpContext.User.Identity?.IsAuthenticated;

                // Only (CIDA) logged-in user is allowed to access the app
                // except when accessing "/Public/" or "/api/App/" which has custom logic.
                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    challenge = true;

                    var path = request.Path;

                    if (path.HasValue)
                    {
                        var pathBase = request.PathBase;

                        //foreach (var segment in new string[] { "/Public/", "/api/App/" })
                        //{
                        var segment = "/Public/";

                        if (path.Value.StartsWith(segment) ||
                        (
                            pathBase.HasValue && pathBase.Value != "/" &&
                            path.Value.StartsWith(pathBase.Value + segment)
                        ))
                        {
                            challenge = false;
                            //break;
                        }
                        //}
                    }
                }
                else
                {
                    var userSID = httpContext.User.Claims?.FirstOrDefault(c => c.Type == "sid")?.Value;

                    // Get new session once relogged-in from AppHost
                    challenge = !string.IsNullOrWhiteSpace(userSID) &&
                        request.Cookies.TryGetValue("x-sid", out var x_sid) &&
                        !string.IsNullOrWhiteSpace(x_sid) &&
                        userSID != x_sid;

                    if (!challenge)
                    {
                        var EmulatedUserId_fromCookies = request.Cookies["EmulatedUserId"];
                        if (!string.IsNullOrWhiteSpace(EmulatedUserId_fromCookies))
                        {
                            var EmulatedUserId_fromClaims = httpContext.User.Claims?.FirstOrDefault(c => c.Type == "EmulatedUserId")?.Value;

                            // Get new session once switched user from AppHost
                            challenge = EmulatedUserId_fromCookies != EmulatedUserId_fromClaims;
                        }
                    }
                }

                if (challenge)
                {
                    if (request.Headers.ContainsKey("x-fromhttpclient"))
                    {
                        await httpContext.ChallengeAsync(new AuthenticationProperties()
                        {
                            RedirectUri = request.Headers.Referer
                        });

                        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                        httpContext.Response.Headers.Append("x-challenged", "1");
                    }
                    else
                    {
                        await httpContext.ChallengeAsync();
                    }

                    return;
                }

                await next(httpContext);
            }
        }
    }
}
