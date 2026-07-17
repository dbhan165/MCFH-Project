using Hangfire;
using MCFH.Configuration;
using MCFH.Models;
using MCFH.Services;
using MCFH.Services.Scraping;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace MCFH
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Local: browsers trong .playwright (playwright.ps1 install).
            // Docker: giữ path mặc định của image mcr.microsoft.com/playwright/dotnet (đã có Chromium).
            var runningInContainer = string.Equals(
                Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            if (!runningInContainer &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")))
            {
                var playwrightBrowsers = Path.Combine(builder.Environment.ContentRootPath, ".playwright");
                Directory.CreateDirectory(playwrightBrowsers);
                Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", playwrightBrowsers);
            }

            ScrapeCookiePaths.Initialize(builder.Environment.ContentRootPath);

            // Add services to the container.
            builder.Services.AddMemoryCache();
            // 1. Cấu hình DbContext sử dụng DI
            builder.Services.AddDbContext<McfhDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));
            builder.Services.AddTransient<IEmailService, EmailService>();
            builder.Services.AddHttpClient(nameof(EmailService));
            builder.Services.AddSingleton<IAuthEmailTemplateService, AuthEmailTemplateService>();
            // 2. Kích hoạt xác thực bằng JWT Bearer
            var jwtKey = builder.Configuration["Jwt:Key"]!;
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });
            // 3. Cấu hình CORS - Cho phép Frontend (React/Vite) gọi API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()   // Cho phép mọi tên miền (Ví dụ: localhost:5173)
                          .AllowAnyMethod()   // Cho phép mọi phương thức (GET, POST, PUT, DELETE...)
                          .AllowAnyHeader();  // Cho phép gửi mọi loại Header (kể cả Token)
                });
            });
            builder.Services.AddControllers()
                .AddXmlSerializerFormatters()
                .AddXmlDataContractSerializerFormatters();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Nhập JWT token. Ví dụ: Bearer {token}"
                });
                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id   = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // ── Đăng ký Services (Dependency Injection) ──
            builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();

            builder.Services.Configure<AiModelOptions>(builder.Configuration.GetSection(AiModelOptions.SectionName));
            builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
            builder.Services.Configure<ScrapeOptions>(builder.Configuration.GetSection(ScrapeOptions.SectionName));
            builder.Services.Configure<SerpApiOptions>(builder.Configuration.GetSection(SerpApiOptions.SectionName));
            builder.Services.Configure<ProxyOptions>(builder.Configuration.GetSection(ProxyOptions.SectionName));
            builder.Services.AddHttpClient<SerpApiNewsDiscovery>();
            builder.Services.AddHttpClient<IAiSentimentService, AiSentimentService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ProjectAlertService>();
            builder.Services.AddScoped<MentionManagementService>();
            builder.Services.AddScoped<AiAnalysisService>();
            builder.Services.AddScoped<ScrapeByKeywordService>();
            builder.Services.AddScoped<ProxyRotationService>();
            builder.Services.AddScoped<ProxyAdminService>();
            builder.Services.AddScoped<FbSourceAdminService>();
            builder.Services.AddSingleton<IPlatformCookiePathProvider, PlatformCookiePathProvider>();
            builder.Services.AddScoped<PlatformCookieAdminService>();
            builder.Services.AddSingleton<ScrapeJobStore>();
            builder.Services.AddSingleton<ScrapeJobRunner>();
            builder.Services.AddScoped<ScrapeOrderService>();

            // 4. Cấu hình Hangfire — dùng chung connection string "MyCnn"
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("MyCnn")));
            builder.Services.AddHangfireServer();
            builder.Services.AddScoped<ScrapingJobService>();

            var app = builder.Build();

            PlatformCookieRuntime.Initialize(app.Services.GetRequiredService<IPlatformCookiePathProvider>());

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            // QUAN TRỌNG: UseCors phải nằm TRƯỚC UseAuthentication và UseAuthorization
            app.UseCors("AllowAll");
            app.UseAuthentication(); // Xác thực danh tính
            app.UseAuthorization();  // Kiểm tra phân quyền

            app.UseHangfireDashboard("/hangfire");

            app.MapControllers();

            // 5. Scheduler UC-76: tick thường xuyên, mỗi project cào khi đủ PerProjectScrapeIntervalMinutes từ started_at
            var scrapeSchedulerCron = builder.Configuration["Scraping:HangfireSchedulerCron"] ?? "*/1 * * * *";
            RecurringJob.AddOrUpdate<ScrapingJobService>(
                "scrape-due-projects",
                service => service.RunDueProjectsAsync(),
                scrapeSchedulerCron
            );

            // Recovery: nhặt lại các scrape order kẹt ở scraping/analyzing sau khi backend restart.
            RecurringJob.AddOrUpdate<ScrapeOrderService>(
                "recover-stuck-scrape-orders",
                service => service.RecoverStuckOrdersAsync(),
                "*/5 * * * *"
            );

            app.Run();
        }
    }
}