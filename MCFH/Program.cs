using Hangfire;
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
            // Add services to the container.
            // 1. Cấu hình DbContext sử dụng DI
            builder.Services.AddDbContext<McfhDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));
            builder.Services.AddTransient<IEmailService, EmailService>();
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
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<ScrapeByKeywordService>();

            // 4. Cấu hình Hangfire — dùng chung connection string "MyCnn"
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(builder.Configuration.GetConnectionString("MyCnn")));
            builder.Services.AddHangfireServer();
            builder.Services.AddScoped<ScrapingJobService>();

            var app = builder.Build();
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

            // 5. Đăng ký Recurring Job — chạy mỗi 15 phút (UC-76)
            RecurringJob.AddOrUpdate<ScrapingJobService>(
                "scrape-all-projects",
                service => service.RunAllProjectsAsync(),
                "*/15 * * * *"
            );

            app.Run();
        }
    }
}