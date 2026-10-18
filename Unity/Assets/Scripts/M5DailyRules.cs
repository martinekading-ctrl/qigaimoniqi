using System;
using System.Collections.Generic;
using System.Globalization;
using Reborn1985.M2;

namespace Reborn1985.M5
{
    // One unit per approved source per refresh cycle. This is a provisional P0 fixture.
    public static class M5DailyRules
    {
        private static readonly Dictionary<string,ItemKind> sources = new Dictionary<string,ItemKind>
        {
            ["B-02"]=ItemKind.Paper, ["D-01"]=ItemKind.Paper,
            ["D-02"]=ItemKind.Bottle, ["D-03"]=ItemKind.Bottle,
            ["F-01"]=ItemKind.Paper, ["F-02"]=ItemKind.Bottle, ["F-03"]=ItemKind.Metal
        };
        public static IReadOnlyList<string> SourceIds { get; } = Array.AsReadOnly(new[]{"B-02","D-01","D-02","D-03","F-01","F-02","F-03"});
        public static bool TryKind(string source,out ItemKind kind) => sources.TryGetValue(source??"",out kind);
        public static int CycleStart(int day,int interval) => 1+(day-1)/interval*interval;
        public static string UnitId(string source,int cycleDay) => cycleDay==1?source:source+"@"+cycleDay.ToString(CultureInfo.InvariantCulture);
    }
}
