namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

using AutoMapper;
using edt.disclosure.HttpClients.Services.EdtDisclosure;

public class MappingProfile : Profile
{
    public MappingProfile() => this.CreateMap<EdtDisclosureUserProvisioningModel, EdtUserDto>();
}
