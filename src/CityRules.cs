using System;

namespace JinSongPoc
{
    internal static class CityRules
    {
        internal enum Ending { None, Song, Execution, Commendation }
        internal const int SiegeStartDay = 43;

        internal static Ending Resolve(bool siege, int song, int jin, bool inside, bool outside,
            bool unresolvedOffense, bool commissioned, int verifiedLinks)
        {
            if (!siege) return Ending.None;
            if (jin >= 8 && unresolvedOffense) return Ending.Execution;
            if (jin >= 8 && commissioned && verifiedLinks == 3) return Ending.Commendation;
            if (song >= 8 && jin <= 4 && inside && outside) return Ending.Song;
            return Ending.None;
        }

        internal static bool CanCommission(int reports, int reputation, bool suppliedEvidence)
        {
            return reports >= 2 || (reports >= 1 && reputation > 0) || suppliedEvidence;
        }

        internal static long Price(long value, int percent)
        {
            if (value <= 0 || percent == 100) return value;
            if (value >= int.MaxValue) return int.MaxValue;
            return Math.Min(int.MaxValue, (value * percent + 50) / 100);
        }
    }
}
