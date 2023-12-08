namespace edt.service.ServiceEvents.PersonFolioLinkageHandler;

public interface IFolioLinkageService
{
    public Task<int> ProcessPendingRequests();
}
