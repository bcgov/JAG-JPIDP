namespace Pidp.Features.Parties;

using AutoMapper;
using NodaTime;
using static NodaTime.Extensions.DateTimeExtensions;

using Pidp.Infrastructure.HttpClients.Plr;
using Pidp.Models;
using Pidp.Models.Lookups;
using Pidp.Features.Admin.CourtLocations;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        this.CreateProjection<Party, Demographics.Command>();
        this.CreateProjection<Party, ProfileStatus.ProfileStatusDto>()
            .IncludeMembers(party => party.OrgainizationDetail)
            //.IncludeMembers(party => party.CorrectionServiceDetail, party => party.OrgainizationDetail)
            //.ForMember(dest => dest.EmployeeIdentifier, opt => opt.MapFrom(src =>src.CorrectionServiceDetail.PeronalId))
            .ForMember(dest => dest.OrgDetailId, opt => opt.MapFrom(src => src.OrgainizationDetail!.Id))
            .ForMember(dest => dest.CompletedEnrolments, opt => opt.MapFrom(src => src.AccessRequests.Select(x => x.AccessTypeCode)))
            .ForMember(dest => dest.OrganizationDetailEntered, opt => opt.MapFrom(src => src.OrgainizationDetail != null))
            .ForMember(dest => dest.CompletedEnrolments, opt => opt.MapFrom(src => src.AccessRequests.Select(x => x.AccessTypeCode)))
            .ForMember(dest => dest.AccessRequestStatus, opt => opt.MapFrom(src => src.AccessRequests.Select(x => x.Status)))
            .ForMember(dest => dest.OrganizationDetailEntered, opt => opt.MapFrom(src => src.OrgainizationDetail != null))
            .ForMember(dest => dest.PlrStanding, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
        this.CreateProjection<Party, WorkSetting.Command>()
            .ForMember(dest => dest.PhysicalAddress, opt => opt.MapFrom(src => src.Facility!.PhysicalAddress));

        this.CreateProjection<FacilityAddress, WorkSetting.Command.Address>();
        this.CreateProjection<PartyOrgainizationDetail, ProfileStatus.ProfileStatusDto>();
        this.CreateProjection<CorrectionServiceDetail, ProfileStatus.ProfileStatusDto>();

        this.CreateProjection<PartyAccessAdministrator, AccessAdministrator.Command>();
        this.CreateProjection<PartyLicenceDeclaration, LicenceDeclaration.Command>();
        this.CreateProjection<PartyLicenceDeclaration, ProfileStatus.ProfileStatusDto.LicenceDeclarationDto>();
        this.CreateProjection<PartyOrgainizationDetail, OrganizationDetails.Command>();
        this.CreateMap<SubmittingAgency, SubmittingAgencyModel>();
        this.CreateMap<CourtLocation, CourtLocationAdminModel>();

        this.CreateMap<PlrRecord, CollegeCertifications.Model>()
            .ForMember(dest => dest.IsGoodStanding, opt => opt.MapFrom(src => src.IsGoodStanding()))
            .ForMember(dest => dest.StatusStartDate, opt => opt.MapFrom(src => src.StatusStartDate != null ? src.StatusStartDate.Value.ToLocalDateTime().Date : (LocalDate?)null));
    }
}
