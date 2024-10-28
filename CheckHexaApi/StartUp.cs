using CheckHexaApi.Models.Shared;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using CheckHexaApi.MiddleWares;

namespace CheckHexaApi
{
    /// <summary>
    /// Handles start up tasks.
    /// </summary>
    public class Startup
    {
        private readonly IAppSettings _appSettings;
        private readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
        private readonly string MyAllowEndpoints = "_MyAllowEndpoints";
        /// <summary>
        /// Handles start up tasks.
        /// </summary>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            // Getting app settings from appsettings.json
            _appSettings = configuration.GetSection("AppSettings").Get<AppSettings>() ?? throw new ArgumentNullException(nameof(AppSettings));
        }

        /// <summary>
        /// Handles configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        /// <summary>
        /// Handles configuration tasks.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "CheckHexaApi",
                    Version = "v1",
                    Contact = new OpenApiContact
                    {
                        Name = "Check Hexa",
                        Url = new Uri("https://webqa.mbtcheck.com/CIDA"),
                    }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme.
                        Follow format 'Bearer {token}'
                        Example: 'Bearer gTcg53qwfsxC'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,

                        },
                        new List<string>()
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

          
            services.AddCors(options =>
            {
                options.AddPolicy("MyAllowSpecificOrigins",
                    builder => builder
                        .WithOrigins(_appSettings.AllowedOrigins) // Use configured origins
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            var authority = _appSettings.AuthorityEndpoint;
            services.AddCidaAuthentication(authority);
            //configure jwt authentication
            //var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            //services.AddAuthentication(x =>
            //{
            //    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //.AddJwtBearer(x =>
            //{
            //    x.RequireHttpsMetadata = false;
            //    x.SaveToken = true;
            //    x.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(key),
            //        ValidateIssuer = false,
            //        ValidateAudience = false,
            //        ClockSkew = TimeSpan.Zero
            //    };
            //});
            ////services.AddSignalR();
            //services.AddSignalR(hubOptions =>
            //{
            //    hubOptions.EnableDetailedErrors = true;
            //    hubOptions.MaximumParallelInvocationsPerClient = 20;
            //    // hubOptions.ClientTimeoutInterval = TimeSpan.FromHours(24);
            //    // hubOptions.KeepAliveInterval = TimeSpan.FromHours(24);
            //});
            AddServices(services, Configuration);

        }

        private static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            // Dependency injection for application services
         
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CheckHexaApi v1");
                    c.RoutePrefix = "";
                });
            }


            app.UseHttpsRedirection();

            app.UseAuthentication();
         
            app.UseRouting();
            app.UseCidaAuthentication();
            // Cors policy
            app.UseCors("MyAllowSpecificOrigins");

            app.UseAuthorization();
          
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            app.UseStaticFiles(); // for wwwroot folder
           
        }
    }
}