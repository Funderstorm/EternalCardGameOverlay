using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EternalDraftOverlay
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            var rankings = new Dictionary<string, string>();

            foreach (var line in System.IO.File.ReadAllLines("../../DraftTierList_9-7-2017.txt"))
            {
                var entry = line.Split(';');

                rankings.Add(entry[0], entry[1]);
            }

            //var NodeList = new List<Node>();
            //foreach (var line in System.IO.File.ReadAllLines("../../CardPixelData.txt"))
            //{
            //    var entry = line.Split(':');

            //    (int, int, int)[] tmpArray = new (int, int, int)[10];
            //    int i = 0;
            //    foreach (var coords in entry[1].Split(';'))
            //    {
            //        var split = coords.Split(',');
            //        var coord = (Int32.Parse(split[0]), Int32.Parse(split[1]), Int32.Parse(split[2]));

            //        tmpArray[i] = coord;
            //        i++;
            //    }

            //    NodeList.Add(new Node(entry[0], tmpArray));
            //}

            //var sakura = new CardCapturer();
            //sakura.Capture();

            string path = @"../../EternalCardName_Corpus.txt";
            if (!SymSpell.CreateDictionary(path, "")) Console.Error.WriteLine("File not found: " + System.IO.Path.GetFullPath(path));

            SymSpell.verbose = 0;
            SymSpell.editDistanceMax = 2;
            //SymSpell.lp = 7;


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Overlay(rankings, NodeList));
            Application.Run(new Overlay(rankings));
        }
    }
}
