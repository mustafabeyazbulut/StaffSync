using StaffSync.Application.Bases;
using StaffSync.Application.Features.Commands.CorrectionCommands;
using StaffSync.Application.Features.Corrections;
using StaffSync.Application.Features.Exceptions.ContactExceptions;
using StaffSync.Application.Features.Exceptions.CorrectionExceptions;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Rules.CorrectionRules
{
    public class CorrectionRules : BaseRules
    {
        public const int HourlyLimit = 5;

        public Task ContactMustExist(Contact? contact)
        {
            if (contact is null) throw new ContactNotFoundException();
            return Task.CompletedTask;
        }

        public Task ValuesMustChange(Contact contact, IEnumerable<CorrectionItemInput> items)
        {
            foreach (var item in items.Where(i => i.FieldName != ContactFields.Other))
                if (item.NewValue.Trim() == ContactFields.CurrentValue(contact, item.FieldName).Trim())
                    throw new CorrectionValueUnchangedException(ContactFields.Label(item.FieldName));
            return Task.CompletedTask;
        }

        public Task MustNotExceedHourlyLimit(int countInLastHour)
        {
            if (countInLastHour >= HourlyLimit) throw new TooManyCorrectionsException();
            return Task.CompletedTask;
        }
    }
}
