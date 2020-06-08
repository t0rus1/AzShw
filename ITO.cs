namespace AzShw
{
    public enum CreatedUpdated
    {
        Created,
        Updated,
        Skipped,
    }

    public enum StopGo
    {
        Stop,
        Go,
    }

    public class Guidance<T>
    {
        public Guidance(StopGo status, T payload)
        {
            Status = status;
            Payload = payload;

        }
        public StopGo Status { get; set; }

        public T Payload { get; set; }

    }

}