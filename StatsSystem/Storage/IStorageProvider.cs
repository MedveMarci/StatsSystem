using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StatsSystem.API;

namespace StatsSystem.Storage;

public interface IStorageProvider : IDisposable
{
    IReadOnlyDictionary<string, PlayerStats> Load(string identifier);

    void Save(string identifier, IReadOnlyDictionary<string, PlayerStats> data);

    Task SaveAsync(string identifier, IReadOnlyDictionary<string, PlayerStats> data);
}
