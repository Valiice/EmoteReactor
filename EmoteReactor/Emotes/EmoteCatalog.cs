using System;
using System.Collections.Generic;
using System.Linq;

namespace EmoteReactor.Emotes;

public class EmoteCatalog : IEmoteCatalog
{
    private readonly List<EmoteInfo> _all;
    private readonly Dictionary<uint, EmoteInfo> _byId;

    public EmoteCatalog(IEnumerable<EmoteInfo> emotes)
    {
        _all = emotes
            .Where(e => !string.IsNullOrWhiteSpace(e.Name) && !string.IsNullOrWhiteSpace(e.Command))
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _byId = _all.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<EmoteInfo> All => _all;

    public EmoteInfo? FindById(uint id) => _byId.TryGetValue(id, out var info) ? info : null;

    public IReadOnlyList<EmoteInfo> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _all;

        return _all
            .Where(e => e.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
