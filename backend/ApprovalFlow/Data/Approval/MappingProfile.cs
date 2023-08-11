namespace ApprovalFlow.Data.Approval;

using AutoMapper;
using Common.Models.Approval;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        this.CreateMap<ApprovalRequest, ApprovalModel>();
        this.CreateMap<Request, RequestModel>();
        this.CreateMap<ApprovalHistory, ApprovalHistoryModel>();

    }
}
