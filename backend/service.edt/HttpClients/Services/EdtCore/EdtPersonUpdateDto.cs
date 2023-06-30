namespace edt.service.HttpClients.Services.EdtCore;

public class EdtPersonUpdateDto : EdtPersonDto
{
    public int? Id { get; set; }
    public new EdtPersonAddressUpdate? Address { get; set; } = new EdtPersonAddressUpdate();

}

public class EdtPersonAddressUpdate : EdtPersonAddress
{
    public int? Id { get; set; }

}
