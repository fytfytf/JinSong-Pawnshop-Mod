namespace JinSongPoc
{
    internal sealed class WindowTrade
    {
        internal bool Arrived;
        internal long OpeningValue;
        private long paid;

        internal void Record(int price)
        {
            if (price > 0) paid += price;
        }

        internal bool Passed { get { return OpeningValue > 0 && paid * 100 >= OpeningValue * 66; } }
    }
}
