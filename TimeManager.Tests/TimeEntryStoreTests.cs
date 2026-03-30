using Xunit;

namespace TimeManager.Tests;

public class TempFolderFixture : IDisposable
{
    private bool _disposed;

    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "TimeManagerTests", Guid.NewGuid().ToString());

    public TempFolderFixture() => Directory.CreateDirectory(Folder);

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && Directory.Exists(Folder))
            {
                Directory.Delete(Folder, true);
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class TimeEntryStoreTests : IClassFixture<TempFolderFixture>
{
    private readonly TempFolderFixture _fixture;

    public TimeEntryStoreTests(TempFolderFixture fixture)
    {
        _fixture = fixture;
        TimeEntryStore.DataFolder = _fixture.Folder;
        TimeEntryStore.DataFile = Path.Combine(_fixture.Folder, "entries.json");
    }

    private static void EnsureClean()
    {
        if (File.Exists(TimeEntryStore.DataFile))
            File.Delete(TimeEntryStore.DataFile);
    }

    [Fact]
    public void SaveAndLoad_PersistsEntry()
    {
        EnsureClean();

        var entry = new TimeEntry { Date = DateTime.Today, Description = "Test", Category = Category.Growth, Activity = Activity.Learning, Duration = TimeSpan.FromHours(1) };

        TimeEntryStore.Save(entry);

        var entries = TimeEntryStore.Load();
        Assert.Single(entries);
        var loaded = entries[0];
        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal(entry.Description, loaded.Description);
    }

    [Fact]
    public void Update_ReplacesExistingEntry()
    {
        EnsureClean();

        var entry = new TimeEntry { Date = DateTime.Today, Description = "Original", Category = Category.Growth, Activity = Activity.Learning, Duration = TimeSpan.FromHours(1) };
        TimeEntryStore.Save(entry);

        entry.Description = "Updated";
        TimeEntryStore.Update(entry);

        var loaded = TimeEntryStore.Load().First();
        Assert.Equal("Updated", loaded.Description);
    }

    [Fact]
    public void GetWeek_ReturnsEntriesWithinWeek()
    {
        EnsureClean();

        var weekStart = new DateTime(2026, 03, 23, 0, 0, 0, DateTimeKind.Utc); // Monday
        var inWeek = new TimeEntry { Date = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc), Description = "InWeek", Category = Category.Debt, Activity = Activity.Programming, Duration = TimeSpan.FromHours(2) };
        var outWeek = new TimeEntry { Date = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc), Description = "OutWeek", Category = Category.Debt, Activity = Activity.Programming, Duration = TimeSpan.FromHours(1) };

        TimeEntryStore.Save(inWeek);
        TimeEntryStore.Save(outWeek);

        var weekEntries = TimeEntryStore.GetWeek(weekStart);
        Assert.Single(weekEntries);
        Assert.Equal("InWeek", weekEntries[0].Description);
    }
}
