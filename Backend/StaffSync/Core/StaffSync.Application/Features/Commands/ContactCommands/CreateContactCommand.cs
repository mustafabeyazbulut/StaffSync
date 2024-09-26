﻿using MediatR;

namespace StaffSync.Application.Features.Commands.ContactCommands
{
    public class CreateContactCommand : IRequest<Unit>
    {
        public string DisplayName { get; set; }
        public string ImageUrl { get; set; }
        public string Email { get; set; }
        public string TelephoneNumber { get; set; }
        public string Home { get; set; } //KısaKod
        public string Office { get; set; }
        public string JobTitle { get; set; }
        public string Department { get; set; }
        public string Company { get; set; }
        public string ManagerName { get; set; }
    }
}
