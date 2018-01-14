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

            foreach (var line in System.IO.File.ReadAllLines("../../DraftTierList_01-11-2018_Overall.txt"))
            {
                var entry = line.Split(';');

                rankings.Add(entry[0], entry[1]);
            }

            string path = @"../../EternalCardName_Corpus2.txt";
            if (!SymSpell.CreateDictionary(path, "")) Console.Error.WriteLine("File not found: " + System.IO.Path.GetFullPath(path));

            //verbosity=Top=0: the suggestion with the highest term frequency of the suggestions of smallest edit distance found
            //verbosity=Closest=1: all suggestions of smallest edit distance found, the suggestions are ordered by term frequency 
            //verbosity=All=2: all suggestions <= maxEditDistance, the suggestions are ordered by edit distance, then by term frequency (slower, no early termination)

            SymSpell.verbose = 2;
            SymSpell.editDistanceMax = 3;
            //SymSpell.lp = 7;


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Overlay(rankings));
        }
    }
}
