namespace Pidp.Features.Parties;

using AutoMapper;

using Pidp.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        this.CreateProjection<Party, Demographics.Command>();
        this.CreateProjection<Party, ProfileStatus.ProfileStatusDto>()
            .IncludeMembers(party => party.PartyCertification)
            .IncludeMembers(party => party.OrgainizationDetail)
            //.IncludeMembers(party => party.CorrectionServiceDetail, party => party.OrgainizationDetail)
            //.ForMember(dest => dest.EmployeeIdentifier, opt => opt.MapFrom(src =>src.CorrectionServiceDetail.PeronalId))
            .ForMember(dest => dest.OrgDetailId, opt => opt.MapFrom(src => src.OrgainizationDetail.Id))
            .ForMember(dest => dest.CompletedEnrolments, opt => opt.MapFrom(src => src.AccessRequests.Select(x => x.AccessTypeCode)))
            .ForMember(dest => dest.OrganizationDetailEntered, opt => opt.MapFrom(src => src.OrgainizationDetail != null))
            .ForMember(dest => dest.PlrRecordStatus, opt => opt.Ignore());

        this.CreateProjection<Party, WorkSetting.Command>()
            .ForMember(dest => dest.PhysicalAddress, opt => opt.MapFrom(src => src.Facility!.PhysicalAddress));

        this.CreateProjection<PartyAccessAdministrator, AccessAdministrator.Command>();
        this.CreateProjection<FacilityAddress, WorkSetting.Command.Address>();
        this.CreateProjection<PartyCertification, CollegeCertification.Command>();
        this.CreateProjection<PartyCertification, ProfileStatus.ProfileStatusDto>();
        this.CreateProjection<PartyOrgainizationDetail, ProfileStatus.ProfileStatusDto>();
        this.CreateProjection<CorrectionServiceDetail, ProfileStatus.ProfileStatusDto>();
        this.CreateProjection<PartyOrgainizationDetail, OrganizationDetails.Command>();
    }
}
