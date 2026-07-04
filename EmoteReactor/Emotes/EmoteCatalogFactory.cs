using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace EmoteReactor.Emotes;

public static class EmoteCatalogFactory
{
    public static IEmoteCatalog Build(IDataManager dataManager)
    {
        var list = new List<EmoteInfo>();
        var sheet = dataManager.GetExcelSheet<Emote>();
        if (sheet is null)
            return new EmoteCatalog(list);

        foreach (var row in sheet)
        {
            var name = row.Name.ExtractText();
            var command = row.TextCommand.ValueNullable?.Command.ExtractText() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
                continue;
            list.Add(new EmoteInfo(row.RowId, name, command, row.Icon));
        }

        return new EmoteCatalog(list);
    }
}
