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

            builder.Services.AddScoped<ScrapeByKeywordService>();

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

            app.MapControllers();

            app.Run();
        }
    }
}