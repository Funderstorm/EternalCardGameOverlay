using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EternalDraftOverlay
{
    public class Node
    {
        public string Name;
        public (int, int, int)[] PixelData;

        public Node() { }

        public Node(string name, (int, int, int)[] pixelData)
        {
            Name = name;
            PixelData = pixelData;
        }
    }
}
