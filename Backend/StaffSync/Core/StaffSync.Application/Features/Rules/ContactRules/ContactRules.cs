using StaffSync.Application.Bases;
using StaffSync.Application.Features.Exceptions.ContactExceptions;
using StaffSync.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Rules.ContactRules
{
    public class ContactRules:BaseRules
    {
        public Task ContactTelephoneNumberMustNotBeSame(IList<Contact> contacts,string requestTitle)
        {
            if (contacts.Any(x => x.TelephoneNumber == requestTitle)) throw new ContactTelephoneNumberMustNotBeSameException(); 
            return Task.CompletedTask;
        }
    }
}
