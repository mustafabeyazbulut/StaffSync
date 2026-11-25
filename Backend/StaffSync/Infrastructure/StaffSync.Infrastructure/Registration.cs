using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Interfaces.Directory;
using StaffSync.Application.Interfaces.Email;
using StaffSync.Application.Interfaces.Photos;
using StaffSync.Application.Interfaces.Tokens;
using StaffSync.Infrastructure.DirectoryExport;
using StaffSync.Infrastructure.Email;
using StaffSync.Infrastructure.Photos;
using StaffSync.Infrastructure.Tokens;
using System.Text;

namespace StaffSync.Infrastructure
{
    public static class Registration
    {
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TokenSettings>(configuration.GetSection("JWT"));
            services.AddTransient<ITokenService, TokenService>();

            services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
            services.Configure<CorrectionSettings>(configuration.GetSection("Corrections"));
            services.AddTransient<IEmailSender, SmtpEmailSender>();

            services.Configure<DirectoryExportSettings>(configuration.GetSection("DirectoryExport"));
            services.AddSingleton<IDirectoryChangeNotifier, DirectoryChangeNotifier>();
            services.AddHostedService<DirectoryExportBackgroundService>();

            services.Configure<ContactPhotoSettings>(configuration.GetSection("ContactPhotos"));
            services.AddSingleton<IContactPhotoStore, FileContactPhotoStore>();

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
            {
                opt.SaveToken = true;
                opt.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"])),
                    ValidateLifetime = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });
        }
    }
}
