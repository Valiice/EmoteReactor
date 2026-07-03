using System.Collections.Generic;

namespace EmoteReactor.Emotes;

public interface IEmoteCatalog
{
    IReadOnlyList<EmoteInfo> All { get; }
    EmoteInfo? FindById(uint id);
    IReadOnlyList<EmoteInfo> Search(string query);
}
