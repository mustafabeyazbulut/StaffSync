using MediatR;
using StaffSync.Application.Features.Mediator.Queries.ContactQueries;
using StaffSync.Application.Features.Mediator.Results.ContactResults;
using StaffSync.Application.Interfaces.AutoMapper;
using StaffSync.Application.Interfaces.UnitOfWorks;
using StaffSync.Domain.Entities;

namespace StaffSync.Application.Features.Mediator.Handlers.ContactHandlers
{
    public class GetContactQueryHandler : IRequestHandler<GetContactQuery, List<GetContactQueryResult>>
    {
        //private readonly IRepository<Contact> _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GetContactQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<List<GetContactQueryResult>> Handle(GetContactQuery request, CancellationToken cancellationToken)
        {
            throw new Exception("Deneme Hatası");
            //var values= await _repository.GetAllAsync();
            var values = await _unitOfWork.GetReadRepository<Contact>().GetAllAsync();
            //return values.Select(x=> new GetContactQueryResult
            //{
            //    ContactId = x.Id,
            //    CoverImageUrl = x.CoverImageUrl,
            //    FirstName = x.FirstName,
            //    LastName = x.LastName,
            //    DisplayName = x.DisplayName,
            //    Email = x.Email,
            //    TelephoneNumber = x.TelephoneNumber,
            //    JobTitle = x.JobTitle,
            //    Department = x.Department,
            //    Company = x.Company,
            //}).ToList();
            var map = _mapper.Map<GetContactQueryResult, Contact>(values);
            return map.ToList();
        }
    }
}
