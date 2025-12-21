using System.Collections.Generic;
namespace Chris.Events.Editor
{
    class EventLog
    {
        public List<EventLogLine> lines { get; } = new List<EventLogLine>();

        public EventLog(params EventLogLine[] eventLogLines)
        {
            lines.AddRange(eventLogLines);
        }

        public void AddLine(EventLogLine eventLogLine)
        {
            lines.Add(eventLogLine);
        }

        public void Clear()
        {
            lines.Clear();
        }
    }
}
