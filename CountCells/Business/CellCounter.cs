﻿using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
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
        private int _imageCount;
        private List<Image<Bgr, Byte>> _images;
        private List<int> _cellCount;
        private HashSet<Tuple<int, int>> _allVisitedCellPixels;

        public CellCounter(int imageCount)
        {
            _cellLineColor1 = new Bgr(127, 127, 255);
            _cellLineColor2 = new Bgr(0, 0, 127);
            _imageCount = imageCount;
            _images = new List<Image<Bgr, Byte>>(_imageCount);
            _cellCount = new List<int>(_imageCount);
            _allVisitedCellPixels = new HashSet<Tuple<int, int>>();
        }

        /// <summary>
        /// All the files in the directory should have the name: 'frame (i)' where i is from 0 to N - 1. 
        /// </summary>
        /// <param name="directory">The directory that contains all the picture files.</param>
        public void LoadImages(string directory)
        {
            for (int i = 1; i <= _imageCount; ++i)
            {
                _images.Add(new Image<Bgr, byte>($"{directory}/frame ({i}).gif"));
            }
        }

        public void ProcessCellCounter()
        {
            for (int i = 0; i < _imageCount; ++i)
            {
                _cellCount[i] = CellCount(_images[i]);
            }
        }

        private int CellCount(Image<Bgr, Byte> image)
        {
            var count = 0;

            // Travels left to right, from top to bottom. 
            for (int y = 0; y < image.Height; y = y + 3)
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
            var surroundingOfInitialPixel = new List<Direction>();
            var traveledPixels = new HashSet<Tuple<int, int>>();

            _allVisitedCellPixels.Add(new Tuple<int, int>(y, x));
            traveledPixels.Add(new Tuple<int, int>(y, x));

            // Initial set up. See which way the line seems to go. 
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var coordinates = CoordinatesFor(direction, y, x);  // Item1 = y, Item2 = x

                if (IsValidCoordinate(image, coordinates.Item1, coordinates.Item2) && (IsSimilarColor(image[coordinates.Item1, coordinates.Item2], _cellLineColor1) || IsSimilarColor(image[coordinates.Item1, coordinates.Item2], _cellLineColor2)))
                {
                    surroundingOfInitialPixel.Add(direction);
                }
            }

            foreach (Direction direction in surroundingOfInitialPixel)
            {
                var currentY = y;
                var currentX = x;
                var nextCoordinates = (Tuple<int, int>)null;
                var currentDirection = direction;
                bool? isClockwise = null;

                // Traverse the path until: 
                // it gets back to the original pixel OR
                // the current path runs into a deadend OR
                // encounters a split road and isClockwise is unknown.
                while (true)
                {
                    var nextDirections = (List<Direction>)null;
                    var nextPossibleMoves = new List<Tuple<int, int>>();

                    nextCoordinates = CoordinatesFor(currentDirection, currentY, currentX);
                    _allVisitedCellPixels.Add(new Tuple<int, int>(currentY, currentX));
                    traveledPixels.Add(new Tuple<int, int>(currentY, currentX));
                    // After this line, we're on the 'next' pixel. 

                    // Find valid neighbor pixels in the same direction.
                    nextDirections = GetNextDirections(currentDirection);

                    // Check if they're valid or not and add to a list.
                    foreach (var possibleNextMove in nextDirections)
                    {
                        if (IsValidCoordinate(image, nextCoordinates.Item1, nextCoordinates.Item2) && (IsSimilarColor(image[nextCoordinates.Item1, nextCoordinates.Item2], _cellLineColor1) || IsSimilarColor(image[nextCoordinates.Item1, nextCoordinates.Item2], _cellLineColor2)))
                        {
                            nextPossibleMoves.Add(new Tuple<int, int>(nextCoordinates.Item1, nextCoordinates.Item2));
                        }
                    }

                    if (nextPossibleMoves.Count == 0)
                    {
                        return false;
                    }

                    // Choose one of neighbor pixel to follow. (Look ahead maybe 2 moves?)
                    // Determine isClockwise or not.

                }


            }

            return isCell;
        }

        private bool IsSimilarColor(Bgr color1, Bgr color2)
        {
            const int ColorDifference = 7;
            const int TotalDifference = 10;

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

        private List<Direction> GetNextDirections(Direction previousMove)
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
    }
}