using MediatR;
using StaffSync.Application.Features.Mediator.Queries.ContactQueries;
using StaffSync.Application.Features.Mediator.Results.ContactResults;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers.ContactHandlers
{
    public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, GetContactByIdQueryResult>
    {
        //private readonly IRepository<Contact> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetContactByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<GetContactByIdQueryResult> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            //var value = await _repository.GetByIdAsync(request.ContactId);
            var value = await _unitOfWork.GetReadRepository<Contact>().GetAsync(x => x.Id == request.ContactId);
            //return new GetContactByIdQueryResult
            //{
            //    ContactId = value.Id,
            //    CoverImageUrl = value.CoverImageUrl,
            //    FirstName = value.FirstName,
            //    LastName = value.LastName,
            //    DisplayName = value.DisplayName,
            //    Email = value.Email,
            //    TelephoneNumber = value.TelephoneNumber,
            //    JobTitle = value.JobTitle,
            //    Department = value.Department,
            //    Company = value.Company,
            //};
            var map = _mapper.Map<GetContactByIdQueryResult, Contact>(value);
            return map;
        }
    }
}
