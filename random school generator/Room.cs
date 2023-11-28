using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class Room: GrowableArea
    {
        private int _ID, _idealSize;
        private string _roomType, _facingTowards;
        private bool _grown, _firstGrown;
        private Point _growthFloorPoint;
        private List<Rectangle> _doors, _walls, _equipmentDesks, _tables, _chairs, _extraFurnitureList1, _extraFurnitureList2;
        private Rectangle _teacherDesk, _teacherChair, _cupboard;
        private Dictionary<Room, Rectangle> _adjacencyDoors;
        private List<List<Rectangle>> _allDrawingRects;
        private static Dictionary<int, Color> _componentColours;
        private List<Room> _connections;
        private List<Point> _clearPoints, _innerClearPoints, _innerEdgePoints;
        private Point _zoneTopLeft;
        public Room(int ID, string roomType, Point zoneTopLeft, int rectWidth = 0, int rectHeight = 0) : base()
        {
            _ID = ID;
            _roomType = roomType;
            _doors = new List<Rectangle>();
            _walls = new List<Rectangle>();
            _allDrawingRects = new List<List<Rectangle>> ();
            _adjacencyDoors = new Dictionary<Room, Rectangle>();
            _connections = new List<Room>();
            _clearPoints = new List<Point>();
            _zoneTopLeft = zoneTopLeft;
            _doors = new List<Rectangle>();
            _equipmentDesks = new List<Rectangle>();
            _tables = new List<Rectangle>();
            _chairs = new List<Rectangle>();
            _rectWidth = rectWidth;
            _rectHeight = rectHeight;
            _extraFurnitureList1 = new List<Rectangle>();
            _extraFurnitureList2 = new List<Rectangle>();
        }

        public bool Grown { get => _grown; set => _grown = value; }
        public Point GrowthFloorPoint { get => _growthFloorPoint; set => _growthFloorPoint = value; }
        public int ID { get => _ID; set => _ID = value; }
        public int IdealSize { get => _idealSize; set => _idealSize = value; }
        public List<Rectangle> Doors { get => _doors; set => _doors = value; }
        internal Dictionary<Room, Rectangle> AdjacencyDoors { get => _adjacencyDoors; set => _adjacencyDoors = value; }
        internal List<Room> Connections { get => _connections; set => _connections = value; }
        public string Type { get => _roomType; set => _roomType = value; }
        public List<Point> ClearPoints { get => _clearPoints; set => _clearPoints = value; }
        public List<Rectangle> Walls { get => _walls; set => _walls = value; }
        public Point ZoneTopLeft { get => _zoneTopLeft; set => _zoneTopLeft = value; }
        public Rectangle TeacherDesk { get => _teacherDesk; set => _teacherDesk = value; }
        public Rectangle TeacherChair { get => _teacherChair; set => _teacherChair = value; }
        public Rectangle Cupboard { get => _cupboard; set => _cupboard = value; }
        public List<Rectangle> EquipmentDesks { get => _equipmentDesks; set => _equipmentDesks = value; }
        public List<Point> InnerClearPoints { get => _innerClearPoints; set => _innerClearPoints = value; }
        public List<Point> InnerEdgePoints { get => _innerEdgePoints; set => _innerEdgePoints = value; }
        public List<Rectangle> Tables { get => _tables; set => _tables = value; }
        public List<Rectangle> Chairs { get => _chairs; set => _chairs = value; }
        public string FacingTowards { get => _facingTowards; set => _facingTowards = value; }
        public bool FirstGrown { get => _firstGrown; set => _firstGrown = value; }
        public List<Rectangle> ExtraFurnitureList1 { get => _extraFurnitureList1; set => _extraFurnitureList1 = value; }
        public List<Rectangle> ExtraFurnitureList2 { get => _extraFurnitureList2; set => _extraFurnitureList2 = value; }

        public static void SetComponentColours()
        {
            //set the colours for each type of component in a floor
            _componentColours = new Dictionary<int, Color>
            {
                {1, Color.DarkSeaGreen },
                {2, Color.Firebrick },
                {3, Color.LemonChiffon },
                {4, Color.LavenderBlush },
                {5, Color.Teal },
                {6, Color.Maroon },
                {7, Color.PaleVioletRed }
            };
        }
        public static void RemoveRoomAdjacency(Room r1, Room r2)
        {
            if (r1.AdjacencyDoors.ContainsKey(r2))
            {
                Rectangle door1 = r1.AdjacencyDoors[r2], door2 = r2.AdjacencyDoors[r1];

                r1.Doors.Remove(door1);
                r2.Doors.Remove(door2);

                r1.AdjacencyDoors.Remove(r2);
                r2.AdjacencyDoors.Remove(r1);
            }
    
        }

        public Rectangle MakeRectRelativeToFloor(Rectangle r, int extraX = 0, int extraY = 0)
        {
            return new Rectangle(r.X + _growthTopLeft.X + _zoneTopLeft.X + extraX, r.Y + _growthTopLeft.Y + _zoneTopLeft.Y + extraY, r.Width, r.Height);
        }

        public Point MakePointRelativeToFloor(Point p)
        {
            return new Point(p.X + _growthTopLeft.X + _zoneTopLeft.X, p.Y + _growthTopLeft.Y + _zoneTopLeft.Y);
        }

        public Rectangle MakeRectRelativeToRoom(Rectangle r)
        {
            return new Rectangle(r.X - _growthTopLeft.X - _zoneTopLeft.X, r.Y - _growthTopLeft.Y - _zoneTopLeft.Y, r.Width, r.Height);
        }

        public void CopyChairAndTableDataToGrid()
        {
            //teacher deskbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
            //teacher chair
            //chairs
            //tables

            //check cupboard + add
            //check subj desks + add

            AddRectToGrid(MakeRectRelativeToRoom(_teacherChair), 'C', true, addRect: false);
            AddRectToGrid(MakeRectRelativeToRoom(_teacherDesk), 'T', true, addRect: false);

            foreach (Rectangle r in _chairs)
            {
                AddRectToGrid(MakeRectRelativeToRoom(r), 'C', true, addRect: false);
            }

            foreach (Rectangle r in _tables)
            {
                AddRectToGrid(MakeRectRelativeToRoom(r), 'C', true, addRect: false);
            }

            //int j;
            //new sub for checking
            AddCupboardOrSubjDesk(MakeRectRelativeToRoom(_cupboard), true);

            //just gotta fix this...
            for (int i = 0; i < _equipmentDesks.Count; i++)
            {
                i = AddCupboardOrSubjDesk(MakeRectRelativeToRoom(_equipmentDesks[i]), false, i);
            }

        }

        private int AddCupboardOrSubjDesk(Rectangle r, bool cupboard, int i = 0)
        {
            bool add = true;
            for (int x = r.X; x < r.X + r.Width; x++)
            {
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if ((_grid[x, y] == 'C' || _grid[x, y] == 'T' || _grid[x, y] == 'S') && _roomType != "toilets")
                    {
                        //remove 
                        add = false;

                        if (cupboard)
                        {
                            _cupboard = new Rectangle(0, 0, 0, 0);
                        }
                        else
                        {
                            //this isn't working...
                            
                            _equipmentDesks.Remove(MakeRectRelativeToFloor(r));
                            i--;
                        }

                        break;
                    }
                }
                if (!add)
                {
                    break;
                }
            }

            if (add)
            {
                AddRectToGrid(r, 'S', true, addRect: false);
            }
            return i;
        }
        public Point MakePointRelativeToRoom(Point p)
        {
            return new Point(p.X - _zoneTopLeft.X, p.Y - _zoneTopLeft.Y);
        }


        public void DrawRoom(SpriteBatch spriteBatch, int scrollX, int scrollY)
        {
            _allDrawingRects = new List<List<Rectangle>> { _floorRectangles, _doors };

            //how to draw separate colours?

            //draw the base of the room
            if (_floorRectangles.Count > 0)
            {
                foreach (Rectangle r in _floorRectangles)
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), RoomType.TypeColours[_roomType]);
                }
            }

            //draw other components of the room
            for (int i = 1; i < _allDrawingRects.Count; i++)
            {
                foreach (Rectangle r in _allDrawingRects[i])
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), _componentColours[i]);
                }
            }

            //spriteBatch.Draw(_pixel, new Rectangle(_teacherDesk.X - scrollX, _teacherDesk.Y - scrollY, _teacherDesk.Width, _teacherDesk.Height), Color.Firebrick);
            //spriteBatch.Draw(_pixel, new Rectangle(_teacherChair.X - scrollX, _teacherChair.Y - scrollY, _teacherChair.Width, _teacherChair.Height), Color.LemonChiffon);

            DrawFurniture(spriteBatch, scrollX, scrollY, _teacherChair, RoomType.TeacherChairColours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _teacherDesk, RoomType.TeacherDeskColours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _cupboard, RoomType.CupboardColours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _equipmentDesks, RoomType.SubjDeskColours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _tables, RoomType.DeskColours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _chairs, RoomType.ChairColours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _extraFurnitureList1, RoomType.ExtraFurniture1Colours[_roomType]);
            DrawFurniture(spriteBatch, scrollX, scrollY, _extraFurnitureList2, RoomType.ExtraFurniture2Colours[_roomType]);

            //draw walls
            foreach (Rectangle r in _walls)
            {
                spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), RoomType.WallColours[_roomType]);
            }

        }

        private void DrawFurniture(SpriteBatch spriteBatch, int scrollX, int scrollY, Rectangle furniture, Color colour)
        {
            spriteBatch.Draw(_pixel, new Rectangle(furniture.X - scrollX, furniture.Y - scrollY, furniture.Width, furniture.Height), colour);
        }
        private void DrawFurniture(SpriteBatch spriteBatch, int scrollX, int scrollY, List<Rectangle> furniture, Color colour)
        {
            foreach (Rectangle r in furniture)
            {
                spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), colour);
            }
        }

    }
}
