using Inostvor.Core.Model.Library;
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Data;
using Inostvor.Data.Sqlite;
using Shouldly;
using Xunit;

namespace Inostvor.Data.Tests;

public sealed class SqliteRepositoryTests : IDisposable
{
    private readonly SqliteDatabase _db = new("Data Source=:memory:");

    public void Dispose() => _db.Dispose();

    [Fact]
    public void MachineProfile_CrudRoundTrip_SvaPolja()
    {
        var repo = new MachineProfileRepository(_db);
        var profile = new MachineProfile
        {
            Name = "EC300 Plasma",
            Manufacturer = "DIY",
            HasThc = true,
            PostProcessorId = "inostvor.post.ec300",
            Process = CutProcess.Plasma,
            TableWidth = 900,
            TableHeight = 1200,
            SafeZ = 38.0,
            PierceHeight = 3.8,
            CutHeight = 1.5,
            ProbeMacro = "M101",
            DefaultTechnology = TechnologySettings.Default with { KerfWidth = 1.4 },
        };

        repo.Save(profile);
        var loaded = repo.GetAll().ShouldHaveSingleItem();

        loaded.Id.ShouldBe(profile.Id); // stabilan identitet preživio round-trip
        loaded.HasThc.ShouldBeTrue();
        loaded.ProbeMacro.ShouldBe("M101");
        loaded.DefaultTechnology.KerfWidth.ShouldBe(1.4);
    }

    [Fact]
    public void MachineProfile_UpsertPoStabilnomIdu_PreimenovanjeNeStvaraDuplikat()
    {
        var repo = new MachineProfileRepository(_db);
        var profile = new MachineProfile { Name = "A", PostProcessorId = "x" };
        repo.Save(profile);

        // Preimenovanje istog profila (isti Id) — mora UPDATEATI, ne stvoriti drugi zapis.
        repo.Save(profile with { Name = "A (novo ime)", PostProcessorId = "y" });

        var all = repo.GetAll().ShouldHaveSingleItem();
        all.Id.ShouldBe(profile.Id);
        all.Name.ShouldBe("A (novo ime)");
        all.PostProcessorId.ShouldBe("y");

        repo.Delete(profile.Id);
        repo.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public void Technology_CrudRoundTrip()
    {
        var repo = new TechnologyRepository(_db);
        var entry = new TechnologyEntry
        {
            Name = "Steel 2 mm",
            Material = "S235",
            ThicknessMm = 2.0,
            Gas = "Air",
            Amperage = 45,
            Settings = TechnologySettings.Default with { KerfWidth = 1.3, FeedRate = 3200, PierceTime = 0.4 },
        };

        repo.Save(entry);
        var loaded = repo.GetAll().ShouldHaveSingleItem();

        loaded.Id.ShouldBe(entry.Id);
        loaded.Name.ShouldBe("Steel 2 mm");
        loaded.Amperage.ShouldBe(45);
        loaded.Settings.FeedRate.ShouldBe(3200);

        repo.Delete(entry.Id);
        repo.GetAll().ShouldBeEmpty();
    }

    [Fact]
    public void Settings_KeyValue()
    {
        var repo = new SettingsRepository(_db);
        repo.GetValue("tema").ShouldBeNull();

        repo.SetValue("tema", "tamna");
        repo.GetValue("tema").ShouldBe("tamna");

        repo.SetValue("tema", "svijetla");
        repo.GetValue("tema").ShouldBe("svijetla");
    }

    [Fact]
    public void PortService_IzvozPaUvozUNovuBazu_IdenticniPodaci()
    {
        var machines = new MachineProfileRepository(_db);
        var technologies = new TechnologyRepository(_db);
        machines.Save(new MachineProfile { Name = "Stroj 1", PostProcessorId = "inostvor.post.mach3", HasThc = true });
        technologies.Save(new TechnologyEntry { Name = "Steel 3 mm", ThicknessMm = 3, Amperage = 60 });

        var json = new SettingsPortService(machines, technologies).ExportToJson();

        using var freshDb = new SqliteDatabase("Data Source=:memory:");
        var freshMachines = new MachineProfileRepository(freshDb);
        var freshTechnologies = new TechnologyRepository(freshDb);
        var summary = new SettingsPortService(freshMachines, freshTechnologies).ImportFromJson(json);

        summary.ShouldContain("1 profila");
        freshMachines.GetAll().ShouldHaveSingleItem().HasThc.ShouldBeTrue();
        freshTechnologies.GetAll().ShouldHaveSingleItem().Amperage.ShouldBe(60);
    }
}
