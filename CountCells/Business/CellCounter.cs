using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountCells.Business
{
    public enum Direction
    {
        NORTH_WEST = 7,
        NORTH = 8,
        NORTH_EAST = 9,
        EAST = 6,
        SOUTH_EAST = 3,
        SOUTH = 2,
        SOUTH_WEST = 1,
        WEST = 4
    }

    public class CellCounter
    {
        private Bgr _cellLineColor1, _cellLineColor2;
        private Bgr _visitedColor;
        private int _imageCount;
        private List<Image<Bgr, Byte>> _images;
        private List<int> _cellCount;
        private HashSet<Tuple<int, int>> _allVisitedCellPixels;
        private float _largestCellSize;

        private string _directory;
        private bool _saveImageEveryChange;
        private string _iterativeImageChangeDirectory;

        public CellCounter(int imageCount)
        {
            _cellLineColor1 = new Bgr(127, 127, 255);
            _cellLineColor2 = new Bgr(0, 0, 127);
            _visitedColor = new Bgr(48, 255, 190); // Yellow
            _imageCount = imageCount;
            _images = new List<Image<Bgr, Byte>>(_imageCount);
            _cellCount = new List<int>(_imageCount);
            _allVisitedCellPixels = new HashSet<Tuple<int, int>>();
            _largestCellSize = 40;

            _saveImageEveryChange = true;
            _iterativeImageChangeDirectory = "C:/Users/logic/Documents/IMAGE.jpg";
        }

        /// <summary>
        /// All the files in the directory should have the name: 'frame (i)' where i is from 0 to N - 1. 
        /// </summary>
        /// <param name="directory">The directory that contains all the picture files.</param>
        public void LoadImages(string directory)
        {
            _directory = directory;
            for (int i = 1; i <= _imageCount; ++i)
            {
                _images.Add(new Image<Bgr, byte>($"{directory}/frame ({i}).gif"));
            }
        }

        public void ProcessCellCounter()
        {
            for (int i = 0; i < _imageCount; ++i)
            {
                _allVisitedCellPixels = new HashSet<Tuple<int, int>>();

                Console.WriteLine($"Checking frame {i+1}");
                _cellCount.Add(CellCount(_images[i]));
                _images[i].Save($"{_directory}/frame ({i+1}) counted.gif");
            }
        }

        public List<int> GetCellCounts()
        {
            return _cellCount;
        }

        private int CellCount(Image<Bgr, Byte> image)
        {
            var count = 0;

            // Travels left to right, from top to bottom. 
            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    if (IsSimilarColor(image[y,x], _cellLineColor1) || IsSimilarColor(image[y,x], _cellLineColor2))
                    {
                        if (!_allVisitedCellPixels.Contains(new Tuple<int, int>(y,x)))
                        {
                            count += IsCell(image, y, x) ? 1 : 0;
                        }
                    }
                }
            }

            return count;
        }

        private bool IsCell(Image<Bgr, Byte> image, int y, int x)
        {
            var isCell = false;
            var surroundingOfInitialPixel = new HashSet<Direction>();
            var traveledPixels = (HashSet<Tuple<int, int>>)null;
            var orderedDirections = (List<Direction>)null;
            bool isClockwise = true;

            _allVisitedCellPixels.Add(new Tuple<int, int>(y, x));
            image[y, x] = _visitedColor;

            // Initial set up.
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var coordinates = CoordinatesFor(direction, y, x);  // Item1 = y, Item2 = x

                if (IsValidCoordinate(image, coordinates.Item1, coordinates.Item2) && (IsSimilarColor(image[coordinates.Item1, coordinates.Item2], _cellLineColor1) || IsSimilarColor(image[coordinates.Item1, coordinates.Item2], _cellLineColor2)))
                {
                    surroundingOfInitialPixel.Add(direction);
                }
            }

            // More than 2 pixels next to each other, eliminate.
            if (surroundingOfInitialPixel.Count >= 3)
            {
                IgnoreIntersectionCells(image, y, x);
                return false;
            }

            orderedDirections = OrderDirections(surroundingOfInitialPixel, isClockwise);

            foreach (Direction direction in orderedDirections)
            {
                var currentY = y;
                var currentX = x;
                var nextCoordinates = (Tuple<int, int>)null;
                var currentDirection = direction;
                var numberOfTimesInTraveledPixels = 0;

                traveledPixels = new HashSet<Tuple<int, int>>();
                traveledPixels.Add(new Tuple<int, int>(y, x));

                // Traverse the path until: 
                // it gets back to the original pixel OR
                // the current path runs into a deadend OR
                // encounters a split road and isClockwise is unknown.
                while (true)
                {
                    var nextDirections = (List<Direction>)null;
                    var nextPossibleMoves = new HashSet<Direction>();

                    nextCoordinates = CoordinatesFor(currentDirection, currentY, currentX);
                    
                    if (traveledPixels.Contains(new Tuple<int,int>(nextCoordinates.Item1, nextCoordinates.Item2)))
                    {
                        ++numberOfTimesInTraveledPixels;
                    }

                    _allVisitedCellPixels.Add(new Tuple<int, int>(nextCoordinates.Item1, nextCoordinates.Item2));
                    traveledPixels.Add(new Tuple<int, int>(nextCoordinates.Item1, nextCoordinates.Item2));

                    image[nextCoordinates.Item1, nextCoordinates.Item2] = _visitedColor;
                    if (_saveImageEveryChange)
                        image.Save(_iterativeImageChangeDirectory);

                    // Goal test.
                    if (IsALoop(nextCoordinates.Item2, nextCoordinates.Item1, x, y, traveledPixels.Count, numberOfTimesInTraveledPixels))
                    {
                        _largestCellSize = (_largestCellSize + traveledPixels.Count) / 2.0f;
                        return true;
                    }
                    // After this line, we're on the 'next' pixel. 

                    // Find valid neighbor pixels in the same direction.
                    nextDirections = GetNextDirectionsFull(currentDirection);

                    // Check if they're valid or not and add to a list.
                    foreach (var nextDirection in nextDirections)
                    {
                        var coordinatesForNextDirection = CoordinatesFor(nextDirection, nextCoordinates.Item1, nextCoordinates.Item2);
                        if (
                                IsValidCoordinate(image, coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2)
                                && (IsSimilarColor(image[coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2], _cellLineColor1)
                                    || IsSimilarColor(image[coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2], _cellLineColor2)
                                    || image[coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2].Equals(_visitedColor))
                           )
                        {
                            nextPossibleMoves.Add(nextDirection);
                        }
                    }

                    if (nextPossibleMoves.Count == 0)
                    {
                        // Check one more pixel beyond neighbor to double check.
                        var extraCheck = CoordinatesFor(currentDirection, nextCoordinates.Item1, nextCoordinates.Item2);
                        nextDirections = GetNextDirectionsPartial(currentDirection);
                        currentY = extraCheck.Item1;
                        currentX = extraCheck.Item2;

                        foreach (var nextDirection in nextDirections)
                        {
                            var coordinatesForNextDirection = CoordinatesFor(nextDirection, extraCheck.Item1, extraCheck.Item2);
                            if (IsValidCoordinate(image, coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2) && (IsSimilarColor(image[coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2], _cellLineColor1) || IsSimilarColor(image[coordinatesForNextDirection.Item1, coordinatesForNextDirection.Item2], _cellLineColor2)))
                            {
                                nextPossibleMoves.Add(nextDirection);
                            }
                        }

                        if (nextPossibleMoves.Count == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // The next coordinates will become current coordinates after the iteration ends.
                        currentY = nextCoordinates.Item1;
                        currentX = nextCoordinates.Item2;
                    }

                    // Choose one of neighbor pixel to follow. Prepare 'current' values.

                    if (nextPossibleMoves.Count == 1)
                    {
                        currentDirection = nextPossibleMoves.First();
                    }
                    else if (nextPossibleMoves.Count == 2)
                    {
                        var straightDirections = GetStraightDirections(nextPossibleMoves);
                        var diagonalDirections = GetDiagonalDirections(nextPossibleMoves);

                        if (straightDirections.Count == 1)
                        {
                            currentDirection = straightDirections.First();
                        }
                        else
                        {
                            currentDirection = ChooseDirectionBasedOnCurrentAndPossibleDirections(nextDirections, nextPossibleMoves, isClockwise);
                        }
                    }
                    else
                    {   // y == 123 x == 171
                        // Ignore all nearby pixels with 3 or more adjacent pixels but keep going.
                        IgnoreIntersectionCells(image, nextCoordinates.Item1, nextCoordinates.Item2);
                        currentDirection = ChooseDirectionBasedOnCurrentAndPossibleDirections(nextDirections, nextPossibleMoves, isClockwise);
                    }
                }
            }

            return isCell;
        }

        private void IgnoreIntersectionCells(Image<Bgr, Byte> image, int y, int x)
        {
            var surroundingOfInitialPixel = new HashSet<Direction>();
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var coordinates = CoordinatesFor(direction, y, x);  // Item1 = y, Item2 = x

                if (
                        IsValidCoordinate(image, coordinates.Item1, coordinates.Item2) 
                        && (IsSimilarColor(image[coordinates.Item1, coordinates.Item2], _cellLineColor1) 
                            || IsSimilarColor(image[coordinates.Item1, coordinates.Item2], _cellLineColor2))
                        && !image[coordinates.Item1, coordinates.Item2].Equals(_visitedColor)
                    )
                {
                    surroundingOfInitialPixel.Add(direction);
                }
            }

            if (surroundingOfInitialPixel.Count < 3)
                return;

            foreach (var direction in surroundingOfInitialPixel)
            {
                var coordinates = CoordinatesFor(direction, y, x);  // Item1 = y, Item2 = x
                if (!_allVisitedCellPixels.Contains(new Tuple<int, int>(coordinates.Item1, coordinates.Item2)))
                {
                    _allVisitedCellPixels.Add(new Tuple<int, int>(coordinates.Item1, coordinates.Item2));
                    image[coordinates.Item1, coordinates.Item2] = _visitedColor;
                    if (_saveImageEveryChange)
                        image.Save(_iterativeImageChangeDirectory);
                    IgnoreIntersectionCells(image, coordinates.Item1, coordinates.Item2);
                }
            }
        }

        private bool IsALoop(int initialX, int initialY, int currentX, int currentY, int countOfTraveledPixels, int numberOfTimesInTraveledPixels)
        {
            bool isLooping = false;
            if (initialY == currentX && initialX == currentX && countOfTraveledPixels > _largestCellSize * .75)
            {
                isLooping = true;
            }
            else if (numberOfTimesInTraveledPixels > _largestCellSize * 1.25)
            {
                isLooping = true;
            }
            return isLooping;
        }

        private bool IsSimilarColor(Bgr color1, Bgr color2)
        {
            const int ColorDifference = 15;
            const int TotalDifference = 20;

            var redDifference = Math.Abs(color1.Red - color2.Red);
            var greenDifference = Math.Abs(color1.Green - color2.Green);
            var blueDifference = Math.Abs(color1.Blue - color2.Blue);

            if (redDifference >= ColorDifference || greenDifference >= ColorDifference || blueDifference >= ColorDifference)
                return false;

            if (redDifference + greenDifference + blueDifference >= TotalDifference)
                return false;

            return true;
        }

        private Tuple<int ,int> CoordinatesFor(Direction direction, int y, int x)
        {
            switch (direction)
            {
                case Direction.NORTH_WEST:
                    return new Tuple<int, int>(y - 1, x - 1);
                case Direction.NORTH:
                    return new Tuple<int, int>(y - 1, x);
                case Direction.NORTH_EAST:
                    return new Tuple<int, int>(y - 1, x + 1);
                case Direction.EAST:
                    return new Tuple<int, int>(y, x + 1);
                case Direction.SOUTH_EAST:
                    return new Tuple<int, int>(y + 1, x + 1);
                case Direction.SOUTH:
                    return new Tuple<int, int>(y + 1, x);
                case Direction.SOUTH_WEST:
                    return new Tuple<int, int>(y + 1, x - 1);
                case Direction.WEST:
                    return new Tuple<int, int>(y, x - 1);
                default:
                    return null;
            }
        }

        private bool IsValidCoordinate(Image<Bgr, Byte> image, int y, int x)
        {
            return y >= 0 && y < image.Height && x >= 0 && x < image.Width;
        }

        private List<Direction> GetNextDirectionsFull(Direction previousMove)
        {
            switch (previousMove)
            {
                case Direction.NORTH_WEST: 
                    return new List<Direction> { Direction.SOUTH_WEST, Direction.WEST, Direction.NORTH_WEST, Direction.NORTH, Direction.NORTH_EAST };
                case Direction.NORTH:
                    return new List<Direction> { Direction.WEST, Direction.NORTH_WEST, Direction.NORTH, Direction.NORTH_EAST, Direction.EAST };
                case Direction.NORTH_EAST:
                    return new List<Direction> { Direction.NORTH_WEST, Direction.NORTH, Direction.NORTH_EAST, Direction.EAST, Direction.SOUTH_EAST };
                case Direction.EAST:
                    return new List<Direction> { Direction.NORTH, Direction.NORTH_EAST, Direction.EAST, Direction.SOUTH_EAST, Direction.SOUTH };
                case Direction.SOUTH_EAST:
                    return new List<Direction> { Direction.NORTH_EAST, Direction.EAST, Direction.SOUTH_EAST, Direction.SOUTH, Direction.SOUTH_WEST };
                case Direction.SOUTH:
                    return new List<Direction> { Direction.EAST, Direction.SOUTH_EAST, Direction.SOUTH, Direction.SOUTH_WEST, Direction.WEST };
                case Direction.SOUTH_WEST:
                    return new List<Direction> { Direction.SOUTH_EAST, Direction.SOUTH, Direction.SOUTH_WEST, Direction.WEST, Direction.NORTH_WEST };
                case Direction.WEST:
                    return new List<Direction> { Direction.SOUTH, Direction.SOUTH_WEST, Direction.WEST, Direction.NORTH_WEST, Direction.NORTH };
                default:
                    return new List<Direction>();
            }
        }

        private List<Direction> GetNextDirectionsPartial(Direction previousMove)
        {
            switch (previousMove)
            {
                case Direction.NORTH_WEST:
                    return new List<Direction> { Direction.WEST, Direction.NORTH_WEST, Direction.NORTH };
                case Direction.NORTH:
                    return new List<Direction> { Direction.NORTH_WEST, Direction.NORTH, Direction.NORTH_EAST };
                case Direction.NORTH_EAST:
                    return new List<Direction> { Direction.NORTH, Direction.NORTH_EAST, Direction.EAST };
                case Direction.EAST:
                    return new List<Direction> { Direction.NORTH_EAST, Direction.EAST, Direction.SOUTH_EAST };
                case Direction.SOUTH_EAST:
                    return new List<Direction> { Direction.EAST, Direction.SOUTH_EAST, Direction.SOUTH };
                case Direction.SOUTH:
                    return new List<Direction> { Direction.SOUTH_EAST, Direction.SOUTH, Direction.SOUTH_WEST };
                case Direction.SOUTH_WEST:
                    return new List<Direction> { Direction.SOUTH, Direction.SOUTH_WEST, Direction.WEST };
                case Direction.WEST:
                    return new List<Direction> { Direction.SOUTH_WEST, Direction.WEST, Direction.NORTH_WEST };
                default:
                    return new List<Direction>();
            }
        }

        public HashSet<Direction> GetStraightDirections(HashSet<Direction> directions)
        {
            var straightDirections = new HashSet<Direction>();

            foreach (var direction in directions)
            {
                switch (direction)
                {
                    case Direction.NORTH:
                    case Direction.EAST:
                    case Direction.SOUTH:
                    case Direction.WEST:
                        straightDirections.Add(direction);
                        break;
                    default:
                        break;
                }
            }
                
            return straightDirections;
        }

        public HashSet<Direction> GetDiagonalDirections(HashSet<Direction> directions)
        {
            var diagonalDirections = new HashSet<Direction>();

            foreach (var direction in directions)
            {
                switch (direction)
                {
                    case Direction.NORTH_WEST:
                    case Direction.NORTH_EAST:
                    case Direction.SOUTH_EAST:
                    case Direction.SOUTH_WEST:
                        diagonalDirections.Add(direction);
                        break;
                    default:
                        break;
                }
            }

            return diagonalDirections;
        }

        public List<Direction> OrderDirections(HashSet<Direction> directions, bool isClockwise)
        {
            var orderedDirections = new List<Direction>(8);

            if (isClockwise)
            {
                if (directions.Contains(Direction.NORTH_WEST))
                    orderedDirections.Add(Direction.NORTH_WEST);
                if (directions.Contains(Direction.NORTH))
                    orderedDirections.Add(Direction.NORTH);
                if (directions.Contains(Direction.NORTH_EAST))
                    orderedDirections.Add(Direction.NORTH_EAST);
                if (directions.Contains(Direction.EAST))
                    orderedDirections.Add(Direction.EAST);
                if (directions.Contains(Direction.SOUTH_EAST))
                    orderedDirections.Add(Direction.SOUTH_EAST);
                if (directions.Contains(Direction.SOUTH))
                    orderedDirections.Add(Direction.SOUTH);
                if (directions.Contains(Direction.SOUTH_WEST))
                    orderedDirections.Add(Direction.SOUTH_WEST);
                if (directions.Contains(Direction.WEST))
                    orderedDirections.Add(Direction.WEST);
            }
            else
            {
                if (directions.Contains(Direction.SOUTH_WEST))
                    orderedDirections.Add(Direction.SOUTH_WEST);
                if (directions.Contains(Direction.SOUTH))
                    orderedDirections.Add(Direction.SOUTH);
                if (directions.Contains(Direction.SOUTH_EAST))
                    orderedDirections.Add(Direction.SOUTH_EAST);
                if (directions.Contains(Direction.EAST))
                    orderedDirections.Add(Direction.EAST);
                if (directions.Contains(Direction.NORTH_EAST))
                    orderedDirections.Add(Direction.NORTH_EAST);
                if (directions.Contains(Direction.NORTH))
                    orderedDirections.Add(Direction.NORTH);
                if (directions.Contains(Direction.NORTH_WEST))
                    orderedDirections.Add(Direction.NORTH_WEST);
                if (directions.Contains(Direction.WEST))
                    orderedDirections.Add(Direction.WEST);
            }

            return orderedDirections;
        }

        // 'possibleDirections' must have at least one direction.
        public Direction ChooseDirectionBasedOnCurrentAndPossibleDirections(List<Direction> allDirections, HashSet<Direction> validDirections, bool isClockwise)
        {
            if (isClockwise)
            {
                for (int i = allDirections.Count - 1; i >= 0; --i)
                {
                    if (validDirections.Contains(allDirections[i]))
                        return allDirections[i];
                }
            }
            else
            {
                for (int i = 0; i < allDirections.Count; ++i)
                {
                    if (validDirections.Contains(allDirections[i]))
                        return allDirections[i];
                }
            }
            return validDirections.First();
        }
    }
}
