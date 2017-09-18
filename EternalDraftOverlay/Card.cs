using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EternalDraftOverlay
{
    class Card
    {
        public Card()
        {
            Rank = String.Empty;
            RankLocation = new Point();
        }

        public Card(string rank)
        {
            Rank = rank;
            RankLocation = new Point();
        }

        public Card(string rank, Point loc)
        {
            Rank = rank;
            RankLocation = loc;
        }

        public Card(string rank, Point loc, Rectangle textboxRect)
        {
            Rank = rank;
            RankLocation = loc;
            TextboxBounding = textboxRect;
        }

        public Card(string rank, Point loc, Rectangle textboxRect, Rectangle wholecardRect)
        {
            Rank = rank;
            RankLocation = loc;
            TextboxBounding = textboxRect;
            WholeCardBounding = wholecardRect;
        }

        public Point RankLocation;
        public string Rank;
        public Rectangle TextboxBounding;
        public Rectangle WholeCardBounding;
    }
}
