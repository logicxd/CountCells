# CountCells

The program can count the number of - previously highlighted by another algorithm - bacteria cells in each frame of the sample gif with 99.70% accuracy. It does this by finding and going along the highlighted pixels of a frame in a clockwise direction until it is determined to be a cell by heuristics or until it runs out of paths to explore. The program is built to work specifically with the highlighting techniques of the algorithm and might not work as well using other methods. The completion of this program took about 2 days of coding (approximately 15 hours) and one more day to increase the accuracy of cell counting from 85.5% to 99.7% (approximately 5 hours).

Full report and the algorithm can be found here: https://github.com/logicxd/Research-CountCells/blob/master/CellCountResearch.pdf

## Count Process
![before-and-after](media/during.png)

## Before and After
![before-and-after](media/before-and-after.png)
