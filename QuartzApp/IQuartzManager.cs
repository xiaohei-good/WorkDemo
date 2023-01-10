namespace QuartzApp
{
    public interface IQuartzManager
    {
        Task<bool> StartAsync();
        Task<bool> SaveTrigger(string corn);
        Task<DateTimeOffset?> GetNextFireTime(string triggerKey);
        Task<bool> DeleteJobAndTrigger(Guid id);
    }
}
