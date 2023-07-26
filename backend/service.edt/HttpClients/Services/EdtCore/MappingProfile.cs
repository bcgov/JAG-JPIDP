namespace edt.service.HttpClients.Services.EdtCore;

using AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        this.CreateMap<EdtPersonProvisioningModel, EdtPersonDto>();
        this.CreateMap<EdtUserProvisioningModel, EdtUserDto>()
               .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.PhoneNumber));
    }
}
