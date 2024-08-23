using StaffSync.Application.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaffSync.Application.Features.Exceptions.ContactExceptions
{
    public class ContactTelephoneNumberMustNotBeSameException:BaseException
    {
        public ContactTelephoneNumberMustNotBeSameException() : base("Telefon Numarası zaten kayıtlıdır!"){ }
    }
}
