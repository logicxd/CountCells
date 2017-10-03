using CountCells.Business;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountCells
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Counting Cells...");

            var cellCounter = new CellCounter(40);
            cellCounter.LoadImages("C:/Users/logic/source/repos/CountCells/CountCells/Resources");
            cellCounter.ProcessCellCounter();

            Console.WriteLine("Finished counting.");
        }
    }
}
