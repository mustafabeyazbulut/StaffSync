using MediatR;
using StaffSync.Application.Features.Mediator.Commands;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.Repositories;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers
{
    public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand>
    {
        private readonly IRepository<Contact> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper mapper;

        public UpdateContactCommandHandler(IRepository<Contact> repository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            this.mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            //var value = await _repository.GetByIdAsync(request.ContactId);
            //value.CoverImageUrl = request.CoverImageUrl;
            //value.FirstName = request.FirstName;
            //value.LastName = request.LastName;
            //value.DisplayName = request.DisplayName;
            //value.Email = request.Email;
            //value.TelephoneNumber = request.TelephoneNumber;
            //value.JobTitle = request.JobTitle;
            //value.Department = request.Department;
            //value.Company = request.Company;
            //await _repository.UpdateAsync(value);

            var value = await _unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.Id && !x.IsDeleted);

            var map = mapper.Map<Contact, UpdateContactCommand>(request);

            await _unitOfWork.GetWriteRepository<Contact>().UpdateAsync(map);
            await _unitOfWork.SaveAsync();
        }
    }
}
