using System.Text.Json;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TimeManager.Tests")]

namespace TimeManager;

/// <summary>
/// Categories used to classify time entries.
/// </summary>
public enum Category
{
    /// <summary>
    /// Technical debt reduction, bug fixes, and maintenance work.
    /// </summary>
    Debt,
    /// <summary>
    /// Growth activities such as deploying new functionality, learning new technologies, and improving documentation.
    /// </summary>
    Growth,
    /// <summary>
    /// Other work that doesn't fit into the above categories, such as meetings, administration, or miscellaneous tasks.
    /// </summary>
    Other
}


/// <summary>
/// Activities that can be recorded for a time entry.
/// </summary>
public enum Activity {
    /// <summary>
    /// Programming, coding, or other hands-on development work.
    /// </summary>
    Programming,
    /// <summary>
    /// Research, reading, learning, or improving documentation related to the work.
    /// </summary>
    Research,
    /// <summary>
    /// Documentation writing, updating, or improving existing documentation.
    /// </summary>
    Documentation,
    /// <summary>
    /// Learning activities such as training sessions, courses, or self-directed learning to improve skills and knowledge.
    /// </summary>
    Learning, 
    /// <summary>
    /// Represents a scheduled meeting event.
    /// </summary>
    Meeting,
    /// <summary>
    /// Internal administration tasks such as time tracking, reporting, or other non-development work that supports the overall project but doesn't directly contribute to code changes or learning activities.
    /// </summary>
    Administration,
    /// <summary>
    /// Other activities that don't fit into the above categories, such as miscellaneous tasks, breaks, or any work that doesn't directly contribute to programming, research, documentation, learning, meetings, or administration.
    /// </summary>
    Other
}

/// <summary>
/// Represents a single recorded time entry.
/// </summary>
public class TimeEntry
{
    /// <summary>The unique identifier for this entry.</summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>The date and time when the entry was recorded.</summary>
    public DateTime Date { get; set; }

    /// <summary>Short description of the work performed.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Classification category for the entry.</summary>
    public Category Category { get; set; }

    /// <summary>The activity type for the entry.</summary>
    public Activity Activity { get; set; }

    /// <summary>Duration of the work interval.</summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Simple JSON-backed store for <see cref="TimeEntry"/> instances.
/// </summary>
public static class TimeEntryStore
{
    // Make these internal and writable so tests can point the store to a temp folder.
    internal static string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TimeManager");

    internal static string DataFile = Path.Combine(DataFolder, "entries.json");

    /// <summary>
    /// Saves a new time entry to the data file. If the entry has an empty Id, a new one is generated.
    /// </summary>
    /// <param name="entry">The time entry to save.</param>
    public static void Save(TimeEntry entry)
    {
        Directory.CreateDirectory(DataFolder);

        if (entry.Id == Guid.Empty)
            entry.Id = Guid.NewGuid();

        var entries = Load();
        entries.Add(entry);

        WriteAll(entries);
    }

    /// <summary>
    /// Updates an existing entry identified by <see cref="TimeEntry.Id"/>. If not found, no action is taken.
    /// </summary>
    /// <param name="updated">The updated entry value.</param>
    public static void Update(TimeEntry updated)
    {
        var entries = Load();
        var idx = entries.FindIndex(e => e.Id == updated.Id);
        if (idx >= 0)
        {
            entries[idx] = updated;
            WriteAll(entries);
        }
    }

    /// <summary>
    /// Loads all entries from persistent storage.
    /// </summary>
    /// <returns>List of <see cref="TimeEntry"/> instances; an empty list if no data file exists.</returns>
    public static List<TimeEntry> Load()
    {
        if (!File.Exists(DataFile))
            return new List<TimeEntry>();

        var json = File.ReadAllText(DataFile);
        return JsonSerializer.Deserialize<List<TimeEntry>>(json) ?? new List<TimeEntry>();
    }

    /// <summary>
    /// Returns entries that fall within the 7-day window starting at <paramref name="weekStart"/>.
    /// </summary>
    /// <param name="weekStart">Start of the week (inclusive).</param>
    /// <returns>Ordered list of entries within the specified week.</returns>
    public static List<TimeEntry> GetWeek(DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        return Load()
            .Where(e => e.Date.Date >= weekStart.Date && e.Date.Date < weekEnd.Date)
            .OrderBy(e => e.Date)
            .ToList();
    }

    private static void WriteAll(List<TimeEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DataFile, json);
    }
}
