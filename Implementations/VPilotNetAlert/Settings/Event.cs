namespace VPilotNetAlert.Settings
{
  public class Event
  {
    public Event()
    {
    }

    public EventAction Action { get; set; } = EventAction.Unused;
    public File File { get; set; } = new File();
  }
}
