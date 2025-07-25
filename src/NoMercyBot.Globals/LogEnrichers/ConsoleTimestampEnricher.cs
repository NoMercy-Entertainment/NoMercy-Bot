using System.Drawing;
using System.Globalization;
using Pastel;
using Serilog.Core;
using Serilog.Events;

namespace NoMercyBot.Globals.LogEnrichers;

internal class ConsoleTimestampEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        string timestamp = DateTime.Now.ToString("G", CultureInfo.CurrentCulture).Pastel(Color.DarkGray);

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "Time", timestamp));
    }
}