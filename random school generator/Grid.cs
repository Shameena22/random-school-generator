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
    internal class Grid
    {
        protected char[,] _grid;
        protected static Texture2D _pixel;
        protected List<Point> _edgepoints;
        protected List<Rectangle> _floorRectangles;

        public char[,] GetGrid { get => _grid; set => _grid = value; }
        public List<Point> Edgepoints { get => _edgepoints; set => _edgepoints = value; }
        public List<Rectangle> FloorRectangles { get => _floorRectangles; set => _floorRectangles = value; }

        public Grid(int x, int y)
        {
            ResetGrid(x, y);
            _floorRectangles = new List<Rectangle>();
        }

        // - loading data -
        public static void LoadGridPixelData(GraphicsDevice graphicsDevice)
        {
            //loads data to draw rectangles
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData<Color>(new Color[] { Color.White });
        }

        // - other functions -
        public void ResetGrid(int x, int y, char c = ' ')
        {
            //makes a new grid of the size given, fill with specified character
            _grid = new char[x, y];
            for (int i = 0; i < x; i++)
            {
                for (int j = 0; j < y; j++)
                {
                    _grid[i, j] = c;
                }
            }
        }
        public void AddRectToGrid(Rectangle r, char symbol, bool overwrite = false, char emptySymbol = 'X', bool addRect = true)
        {
            //update grid with filled rectangle
            //can be set to overwrite all characters in the rectangle's position, or just to overwrite characters of a specific symbol
            for (int i = r.X; i < r.X + r.Width; i++)
            {
                for (int j = r.Y; j < r.Y + r.Height; j++)
                {
                    if ((overwrite || _grid[i, j] == emptySymbol) && i >= 0 && j >= 0 && i <= _grid.GetUpperBound(0) && j <= _grid.GetUpperBound(1))
                    {
                        _grid[i, j] = symbol;
                    }     
                }
            }

            //add to list of rectangles for drawing
            if (addRect)
            {
                _floorRectangles.Add(r);
            }
        }
        public void RemoveFromGrid(char symbol, char replacementSymbol = 'X')
        {
            //removes all instances of a character from the grid and replaces it with a set character
            for (int x = 0; x <= _grid.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= _grid.GetUpperBound(1); y++)
                {
                    if (_grid[x, y] == symbol)
                    {
                        _grid[x, y] = replacementSymbol;
                    }
                }
            }

        }
        public void FindAllEdgePoints(char c)
        {
            //creates a list of all points on the grid that are on an edge

            _edgepoints = new List<Point>();
            bool atAnEdge;

            //iterates through grid
            for (int x = 0; x <= _grid.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= _grid.GetUpperBound(1); y++)
                {
                    //c represents the "inside" character
                    if (_grid[x, y] == c)
                    {
                        atAnEdge = false;

                        //if a point is adjacent to an empty space / edge of grid, it is at an edge
                        if ((x > 0 && _grid[x - 1, y] != c) || x == 0)
                        {
                            atAnEdge = true;
                        }
                        else if ((x < _grid.GetUpperBound(0) && _grid[x + 1, y] != c) || x == _grid.GetUpperBound(0))
                        {
                            atAnEdge = true;
                        }
                        else if ((y > 0 && _grid[x, y - 1] != c) || y == 0)
                        {
                            atAnEdge = true;
                        }
                        else if ((y < _grid.GetUpperBound(1) && _grid[x, y + 1] != c) || y == _grid.GetUpperBound(1))
                        {
                            atAnEdge = true;
                        }

                        //add point to list if it is at an edge
                        if (atAnEdge)
                        {
                            _edgepoints.Add(new Point(x, y));
                        }
                    }
                }
            }
        }
        public void UpdateBaseRect(int x, int y, int width, int height)
        {
            //changes the base rectangle to match new positions and dimensions
            _floorRectangles[0] = new Rectangle(x, y, width, height);
        }
    }
}
