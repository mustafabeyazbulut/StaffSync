using StaffSync.Application.Features.Commands.ContactCommands;
using StaffSync.Application.Features.Results.ContactResults;
using StaffSync.Domain.Entities;
using System.Linq.Expressions;

namespace StaffSync.Application.Features.Mappings
{
    public static class ContactMappings
    {
        // EF sorgularında Select(ContactMappings.AsDto) olarak SQL'e çevrilir; sadece gereken kolonlar okunur.
        public static readonly Expression<Func<Contact, ContactDto>> AsDto = c => new ContactDto
        {
            Id = c.Id,
            DisplayName = c.DisplayName,
            ImageUrl = c.ImageUrl,
            Email = c.Email,
            TelephoneNumber = c.TelephoneNumber,
            Home = c.Home,
            Office = c.Office,
            JobTitle = c.JobTitle,
            Department = c.Department,
            Company = c.Company,
            ManagerName = c.ManagerName
        };

        private static readonly Func<Contact, ContactDto> toDto = AsDto.Compile();

        public static ContactDto ToDto(this Contact contact) => toDto(contact);

        public static Contact ToEntity(this CreateContactCommand command) => new()
        {
            DisplayName = command.DisplayName,
            ImageUrl = command.ImageUrl,
            Email = command.Email,
            TelephoneNumber = command.TelephoneNumber,
            Home = command.Home,
            Office = command.Office,
            JobTitle = command.JobTitle,
            Department = command.Department,
            Company = command.Company,
            ManagerName = command.ManagerName
        };

        // Id ve CreatedDate'e dokunmaz (eski AutoMapper kodu CreatedDate'i eziyordu).
        public static void ApplyTo(this UpdateContactCommand command, Contact contact)
        {
            contact.DisplayName = command.DisplayName;
            contact.ImageUrl = command.ImageUrl;
            contact.Email = command.Email;
            contact.TelephoneNumber = command.TelephoneNumber;
            contact.Home = command.Home;
            contact.Office = command.Office;
            contact.JobTitle = command.JobTitle;
            contact.Department = command.Department;
            contact.Company = command.Company;
            contact.ManagerName = command.ManagerName;
        }
    }
}
