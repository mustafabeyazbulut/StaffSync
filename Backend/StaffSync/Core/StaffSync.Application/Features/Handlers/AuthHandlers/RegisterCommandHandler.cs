using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.AuthCommands;
using StaffSync.Application.Features.Rules.AuthRules;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;


namespace StaffSync.Application.Features.Handlers.AuthHandlers
{
    public class RegisterCommandHandler : BaseHandler, IRequestHandler<RegisterCommand, Unit>
    {
        private readonly AuthRules authRules;
        private readonly UserManager<User> userManager;
        private readonly RoleManager<Role> roleManager;
        public RegisterCommandHandler(AuthRules authRules, UserManager<User> userManager, RoleManager<Role> roleManager,
            IMapper mapper, IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor) : base(mapper, unitOfWork, httpContextAccessor)
        {
            this.authRules = authRules;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<Unit> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {

            await authRules.UserShouldNotBeExist(await userManager.FindByEmailAsync(request.Email));// kullanıcı var mı kontrol ediyoruz

            User user = mapper.Map<User, RegisterCommand>(request); // kullanıcıyı map ediyoruz
            user.UserName = request.Email;
            user.SecurityStamp = Guid.NewGuid().ToString(); // güvenlik damgası ekliyoruz

            IdentityResult result = await userManager.CreateAsync(user, request.Password); // kullanıcıyı oluşturuyoruz
            if (result.Succeeded) // kullanıcı oluşturulduysa
            {
                if (!await roleManager.RoleExistsAsync("user")) // user rolü yoksa
                    await roleManager.CreateAsync(new Role // user rolü oluşturuyoruz
                    {
                        Id = Guid.NewGuid(),
                        Name = "user",
                        NormalizedName = "USER",
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                    });

                await userManager.AddToRoleAsync(user, "user");
            }

            return Unit.Value;
        }
    }
}
