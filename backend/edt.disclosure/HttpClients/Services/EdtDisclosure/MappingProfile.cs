namespace edt.disclosure.HttpClients.Services.EdtDisclosure;

using AutoMapper;
using Common.Models.EDT;

public class MappingProfile : Profile
{
    public MappingProfile() => this.CreateMap<EdtDisclosureUserProvisioningModel, EdtUserDto>();
}
