namespace NotificationService;

public interface IBaseEntity
{
    #region Properties
    /// <summary>
    /// get/set - The concurrency row version.
    /// </summary>
    long ConcurrencyControlNumber { get; set; }
    #endregion
}