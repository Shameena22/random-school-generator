using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class Floor : Grid
    {
        private int _floorID, _averageSize, _totalArea;
        private static Dictionary<int, Color> _componentColours;
        private List<Zone> _zones;
        private List<Rectangle> _stairRects, _corridorStartingRects, _corridorRects = new List<Rectangle>();
        private Rectangle _entrance;
        private bool _finishedFirstZoneGrowth, _finishedSecondZoneGrowth, _finishedThirdZoneGrowth, _madeWalls;
        private char[,] _roomGrid;
        private List<List<Rectangle>> _drawingList;
        public int TotalArea { get => _totalArea; set => _totalArea = value; }
        internal List<Zone> Zones { get => _zones; set => _zones = value; }
        public int FloorID { get => _floorID;  }
        public List<Rectangle> StairPoints { get => _stairRects; set => _stairRects = value; }
        public Rectangle Entrance { get => _entrance; set => _entrance = value; }
        public List<Rectangle> CorridorStartingRects { get => _corridorStartingRects; set => _corridorStartingRects = value; }
        public bool FinishedFirstZoneGrowth { get => _finishedFirstZoneGrowth; set => _finishedFirstZoneGrowth = value; }
        public bool FinishedSecondZoneGrowth { get => _finishedSecondZoneGrowth; set => _finishedSecondZoneGrowth = value; }
        public bool FinishedThirdZoneGrowth { get => _finishedThirdZoneGrowth; set => _finishedThirdZoneGrowth = value; }
        public char[,] RoomGrid { get => _roomGrid; set => _roomGrid = value; }
        public List<Rectangle> CorridorRects { get => _corridorRects; set => _corridorRects = value; }
        public bool MadeWalls { get => _madeWalls; set => _madeWalls = value; }

        public Floor(int floorID, int averageSize, int gridDimensions) : base(gridDimensions, gridDimensions)
        {
            _floorID = floorID;
            _averageSize = averageSize;
            _zones = new List<Zone>();
            _stairRects = new List<Rectangle>();
            _entrance = new Rectangle(0, 0, 0, 0);
            _corridorStartingRects = new List<Rectangle>();
            _finishedFirstZoneGrowth = false;
            _drawingList = new List<List<Rectangle>> { _floorRectangles, _corridorRects, _corridorStartingRects, _stairRects };
            _madeWalls = false;
        }

        // - loading data -
        public static void SetComponentColours()
        {
            //set the colours for each type of component in a floor
            _componentColours = new Dictionary<int, Color>
            {
                //floor base
                { 0, Color.LightSlateGray },
                //corridor
                { 1, Color.DarkOliveGreen },
                //corridor starting points
                { 2, Color.Crimson },
                //stairs
                { 3, Color.DarkGoldenrod }
            };
        }

        // - creating floor shape -
        public void CreateFloorGrid(int irregularity, List<Floor> floorsBelow)
        {
            Random random = new Random();
            int baseRectWidth, baseRectHeight,baseRectX, baseRectY, tempRectWidth, tempRectHeight, tempRectX, tempRectY, numOfRects, i = 0;
            Rectangle tempRectangle, baseRectangle;

            //number of extra rectangles depends on irregularity
            numOfRects = random.Next(irregularity - 5, irregularity);

            //set width + height of the first rectangle based on the average size
            baseRectWidth = random.Next((int)(Math.Sqrt(_averageSize) ), (int)(Math.Sqrt(_averageSize) * 1.25));
            baseRectHeight = random.Next((int)(Math.Sqrt(_averageSize) ), (int)(Math.Sqrt(_averageSize) * 1.25));

            //set x and y position of the first rectangle
            baseRectX = random.Next(0, _grid.GetLength(0) - baseRectWidth);
            baseRectY = random.Next(0, _grid.GetLength(1) - baseRectHeight);

            baseRectangle = new Rectangle(baseRectX, baseRectY, baseRectWidth, baseRectHeight);

            //add first rectangle to grid and modify floors below
            AddRectToGrid(baseRectangle, true);
            ModifyFloorsBelow(baseRectangle, floorsBelow);

            //update total area created
            _totalArea = baseRectHeight * baseRectWidth;

            do
            {
                //set width and height of extra rectangle
                tempRectWidth = random.Next((int)Math.Ceiling(Math.Sqrt(_averageSize)), (int)Math.Ceiling(Math.Sqrt(_averageSize) * 1.5));
                tempRectHeight = random.Next((int)Math.Ceiling(Math.Sqrt(_averageSize)), (int)Math.Ceiling(Math.Sqrt(_averageSize) * 1.5));

                //set x and y position, ensuring that it is connected to the base rectangle
                tempRectX = random.Next(GetRectPosLowerBoundary(baseRectX, tempRectWidth), GetRectPosUpperBoundary(baseRectX, tempRectWidth));
                tempRectY = random.Next(GetRectPosLowerBoundary(baseRectY, tempRectHeight), GetRectPosUpperBoundary(baseRectY, tempRectHeight));

                //add extra rectangle to grid
                tempRectangle = new Rectangle(tempRectX, tempRectY, tempRectWidth, tempRectHeight);
                UpdateTotalArea(tempRectangle);
                AddRectToGrid(tempRectangle, true);
                ModifyFloorsBelow(tempRectangle, floorsBelow);

                //update counter
                i++;
                //keep adding extra rectangles until total area is large enough and the rectangle quota has been reached
            } while (!(_totalArea >= _averageSize && i >= numOfRects));
        }
        private void UpdateTotalArea(Rectangle r)
        {
            //update total area if the grid is filled in new places
            for (int x = r.X; x < r.X +r.Width; x++ )
            {
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if (_grid[x, y] == ' ')
                    {
                        _totalArea++;
                    }
                }
            }
        }      
        private int GetRectPosLowerBoundary(int mainRectPos, int rectLength)
        {
            if (mainRectPos - rectLength < 0)
            {
               return 0;
            }
            return mainRectPos - rectLength;         
        }
        private int GetRectPosUpperBoundary(int mainRectPos, int rectLength)
        {
            if (mainRectPos + rectLength >= _grid.GetUpperBound(0))
            {
                return _grid.GetUpperBound(0) - rectLength - 1;
            }
            return mainRectPos;
        }
        private void ModifyFloorsBelow(Rectangle newRect, List<Floor> floorsBelow)
        {
            //preventing a floor from going over the space of those below it
            foreach (Floor f in floorsBelow)
            {
                //if part of a rectangle lies outside the floor below
                if (!CheckIfRectFitsBelow(newRect, f))
                {
                    //add the rectangle to the floor below
                    f.AddRectToGrid(newRect, true);
                }
            }
        }
        private bool CheckIfRectFitsBelow(Rectangle r, Floor f)
        {
            bool spacesFound = true;

            //check through each point of a rectangle and see if it fits in the floor rectangles
            //get all the points that go over and add to total area

            for (int x = r.X; x < r.X + r.Width; x++)
            {
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if (f.GetGrid[x, y] == ' ')
                    {
                        f.TotalArea++;
                        spacesFound = false;
                    }
                }
            }
            return spacesFound;
        }
        public void AddRectToGrid(Rectangle r, bool overwrite, char symbol = 'X')
        {
            base.AddRectToGrid(r, symbol, overwrite);
        }
        
        // - creating stairs and entrance -
        public void AddStairs(Rectangle r)
        {
            //add rectangle to stair list for drawing
            _stairRects.Add(r);

            //update the list of edge points so they don't contain any part of the stair block
            UpdateEdgePoints(r);

            //update grid with "S" symbol where the stair block is
            AddRectToGrid(r, true, 'S');
        }
        public void AddEntrance(Rectangle r)
        {
            //store entrance rectangle
            _entrance = r;

            //update edge points list to not include anything in the entrance block
            UpdateEdgePoints(r);

            //fill entrance rectangle with "E" symbols on the grid
            AddRectToGrid(r, true, 'E');
        }
        private void UpdateEdgePoints(Rectangle r)
        {
            //iterate through edges of the rectangle
            //remove these points from the list of edgepoints (if they are present)
            for (int x = r.X; x < r.X + r.Width; x++)
            {
                if (_edgepoints.Contains(new Point(x, r.Y)))
                {
                    _edgepoints.Remove(new Point(x, r.Y));
                }
                if (_edgepoints.Contains(new Point(x, r.Y + r.Height - 1)))
                {
                    _edgepoints.Remove(new Point(x, r.Y + r.Height - 1));
                }
            }
            for (int y = r.Y; y < r.Y + r.Height; y++)
            {
                if (_edgepoints.Contains(new Point(r.X, y)))
                {
                    _edgepoints.Remove(new Point(r.X, y));
                }
                if (_edgepoints.Contains(new Point(r.X + r.Width - 1, y)))
                {
                    _edgepoints.Remove(new Point(r.X + r.Width - 1, y));
                }
            }
        }

        // - creating corridors -
        public void AddCorridor(List<Point> corridorPoints)
        {
            //making a drawable list of rectangles based on the list of points created by the pathfinding algorithm
            int rectX = 0, rectY = 0, corridorLength = 15;       
            Rectangle tempRect;

            //iterate through each point
            foreach (Point p in corridorPoints)
            {
                //get the x and y positions of the top left corner of the rectangle
                GetCorridorRectBoundaries(p, corridorLength, ref rectX, ref rectY);

                //store as a rectangle; mark it on the grid as 'C' and add it to the list of corridor rectangles
                tempRect = new Rectangle(rectX, rectY, corridorLength, corridorLength);
                AddRectToGrid(tempRect, true, 'C');
                _corridorRects.Add(tempRect);
            }
        }
        private void GetCorridorRectBoundaries(Point p, int corridorLength, ref int rectX, ref int rectY)
        {
            int i, j;
            rectX = p.X;
            rectY = p.Y;

            //prevent the rectangle from going outside the grid's X boundary by shifting it to the left if required
            if (p.X + corridorLength - 1 > _grid.GetUpperBound(0))
            {
                rectX = p.X - (p.X + corridorLength - 1 - _grid.GetUpperBound(0));
            }

            //prevent the rectangle from going outside the building by shifting it to the left / right if required

            //calculate amount to shift left
            for (i = 0; i < corridorLength; i++)
            {
                if (_grid[rectX + i, p.Y] == ' ' )
                {
                    break;
                }
            }

            //now shift to left; rectangle's X position is 0 at a minimum to keep it within the grid's boundary
            rectX = Math.Max(rectX - (corridorLength - i), 0);

            //calculate amount to shift right
            for (j = 0; j < corridorLength; j++)
            {
                if (_grid[rectX + corridorLength - 1 - j, p.Y] == ' ' )
                {
                    break;
                }
            }
    
            //now shift to right; rectangle's X position can't go outside the grid
            rectX = Math.Min(rectX + (corridorLength - j), _grid.GetUpperBound(0) - corridorLength);

            //prevent the rectangle from going outside the grid's Y boundary by shifting it up if required
            if (p.Y + corridorLength - 1 > _grid.GetUpperBound(1))
            {
                rectY = p.Y - (p.Y + corridorLength - 1 - _grid.GetUpperBound(1));
            } 
            //prevent the rectangle from going outside the building by shifting it up / down if required

            //calculate amount to shift up
            for (i = 0; i < corridorLength; i++)
            {
                if (_grid[p.X, rectY + i] == ' ')
                {
                    break;
                }
            }

            //now shift up; rectangle's Y position is 0 at a minimum to keep it within the grid's boundary
            rectY = Math.Max(rectY - (corridorLength - i), 0);

            //calculate amount to shift down
            for (j = 0; j < corridorLength; j++)
            {
                if (_grid[p.X, rectY + corridorLength - 1 - j] == ' ')
                {
                    break;
                }
            }

            //now shift down; rectangle's Y position cannot go over the grid boundary   
            rectY = Math.Min(rectY + (corridorLength - j), _grid.GetUpperBound(1) - corridorLength);
        }
        public void RemoveCorridorPoint(int x, int y, char replacementSymbol)
        {
            //easy solution is to add a grey rect to draw over the corridor - might add a lotta rects though
            //or check each rect and remove it if it overlaps with the point......but inefficient?? 
            _grid[x, y] = replacementSymbol;
            Rectangle r;
            for (int i = 0; i < _corridorRects.Count; i++)
            {
                r = _corridorRects[i];
                if (r.X <= x && r.X + r.Width > x && r.Y <= y && r.Y + r.Height > y)
                {                   
                    _corridorRects.RemoveAt(i);
                    i--;
                }
            }
        }

        // - adding rooms -
        public void SetRoomGrid()
        {
            //ResetGrid(_grid.GetLength(0), _grid.GetLength(1), 'X');

            _roomGrid = new char[_grid.GetLength(0), _grid.GetLength(1)];

            for (int x = 0; x <= _roomGrid.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= _roomGrid.GetUpperBound(1); y++)
                {
                    if (_grid[x, y] == ' ')
                    {
                        _roomGrid[x, y] = ' ';
                    }
                    else
                    {
                        _roomGrid[x, y] = 'X';
                    }
                }
            }
        }
        public void AddToRoomGrid(Rectangle r)
        {
            for (int x = 0; x < r.Width; x++)
            {
                for (int y = 0; y < r.Height; y++)
                {
                    _roomGrid[x + r.X, y + r.Y] = 'R';
                }
            }
        }

        // - drawing floor -
        public void DrawFloor(SpriteBatch spriteBatch, int scrollX, int scrollY)
        {
         

            //draw base rectangle first
            foreach (Rectangle r in _drawingList[0])
            {
                spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), _componentColours[0]);
            }

            //then draw zone rectangles over the base
            foreach (Zone z in _zones)
            {
                if (z.GrowthPoint.X > 0)
                {
                    z.DrawZone(spriteBatch, scrollX, scrollY);
                }
            }

            //draw each rectangle from each type of component list, with its set colour
            for (int i = 1; i < _drawingList.Count; i++)
            {
                foreach (Rectangle r in _drawingList[i])
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), _componentColours[i]);
                }
            }

            //if there is an entrance, draw the entrance
            if (_entrance.Width != 0)
            {
                spriteBatch.Draw(_pixel, new Rectangle(_entrance.X - scrollX, _entrance.Y - scrollY, _entrance.Width, _entrance.Height), Color.Beige);
            }

        }

    }
}
