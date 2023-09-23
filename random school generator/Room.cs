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
        private string _roomType;
        private bool _grown;
        private Point _growthFloorPoint;
        private List<Rectangle> _doors, _walls, _equipmentDesks, _tables, _chairs;
        private Rectangle _teacherDesk, _teacherChair, _cupboard;
        private Dictionary<Room, Rectangle> _adjacencyDoors;
        private List<List<Rectangle>> _allDrawingRects;
        private static Dictionary<int, Color> _componentColours;
        private List<Room> _connections;
        private List<Point> _clearPoints, _innerClearPoints, _innerEdgePoints;
        private Point _zoneTopLeft;
        public Room(int ID, string roomType, Point zoneTopLeft) : base()
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

        public void DrawRoom(SpriteBatch spriteBatch, int scrollX, int scrollY)
        {
            _allDrawingRects = new List<List<Rectangle>> { _floorRectangles, _doors, new List<Rectangle> { _teacherDesk }, new List<Rectangle> { _teacherChair }, new List<Rectangle> { _cupboard }, _equipmentDesks, _tables, _chairs };

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

            //draw walls
            foreach (Rectangle r in _walls)
            {
                spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), RoomType.WallColours[_roomType]);
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
    }
}
