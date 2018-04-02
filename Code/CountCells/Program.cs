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

            var actualCellCount = new int[40];
            var tempCount = 4;
            var difference = 0;
            for (int i = 1; i <= 40; ++i)
            {
                if (i == 16) tempCount = 5;
                if (i == 18) tempCount = 8;
                if (i == 18) tempCount = 8;
                if (i == 28) tempCount = 9;
                if (i == 29) tempCount = 10;
                if (i == 31) tempCount = 12;
                if (i == 32) tempCount = 16;
                if (i == 38) tempCount = 17;
                actualCellCount[i - 1] = tempCount;
            }


            var cellCounter = new CellCounter(40);
            cellCounter.LoadImages("C:/Users/logic/source/repos/CountCells/CountCells/Resources");
            cellCounter.ProcessCellCounter();

            using (System.IO.StreamWriter file = new System.IO.StreamWriter("C:/Users/logic/source/repos/CountCells/CountCells/Resources/count.txt"))
            {
                double totalCount = 0;
                var cellCount = cellCounter.GetCellCounts();
                for (int i = 0; i < cellCount.Count; ++i)
                {
                    file.WriteLine($"Counted {cellCount[i]}. Actual {actualCellCount[i]}.");
                    difference += Math.Abs(cellCount[i] - actualCellCount[i]);
                    totalCount += actualCellCount[i];
                }
                file.WriteLine($"Difference: {difference}. {(totalCount - difference) / totalCount}");
            }

            Console.WriteLine("Finished counting.");
        }
    }
}
