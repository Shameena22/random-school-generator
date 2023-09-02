using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class Room: GrowableArea
    {
        private int _ID;
        private RoomType _roomType;
        private bool _grown;
        private Point _growthFloorPoint;
        private int _idealSize;
        private Dictionary<Room, List<Point>> _adjacencies;
        private List<Rectangle> _doors;
        private Dictionary<Room, Rectangle> _connections;
        private List<List<Rectangle>> _allDrawingRects;
        private static Dictionary<int, Color> _componentColours;
        public Room(int ID, string roomType) : base()
        {
            _ID = ID;
            _roomType = new RoomType(roomType);
            _doors = new List<Rectangle>();
            _allDrawingRects = new List<List<Rectangle>> { _floorRectangles, _doors };
            _connections = new Dictionary<Room, Rectangle>();
        }

        public bool Grown { get => _grown; set => _grown = value; }
        public Point GrowthFloorPoint { get => _growthFloorPoint; set => _growthFloorPoint = value; }
        public int ID { get => _ID; set => _ID = value; }
        public int IdealSize { get => _idealSize; set => _idealSize = value; }
        public List<Rectangle> Doors { get => _doors; set => _doors = value; }
        internal Dictionary<Room, List<Point>> Adjacencies { get => _adjacencies; set => _adjacencies = value; }
        internal Dictionary<Room, Rectangle> Connections { get => _connections; set => _connections = value; }


        public static void SetComponentColours()
        {
            //set the colours for each type of component in a floor
            _componentColours = new Dictionary<int, Color>
            {
                //door TODO: tweak
                { 1, Color.DarkSeaGreen },
            };
        }

        public void DrawRoom(SpriteBatch spriteBatch, int scrollX, int scrollY)
        {
            _allDrawingRects = new List<List<Rectangle>> { _floorRectangles, _doors };
            //draws the base of the room
            if (_floorRectangles.Count > 0)
            {
                foreach (Rectangle r in _floorRectangles)
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), RoomType.TypeColours[_roomType.Type]);
                }
            }

            for (int i = 1; i < _allDrawingRects.Count; i++)
            {
                foreach (Rectangle r in _allDrawingRects[i])
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), _componentColours[i]);
                }
            }

        }
    }
}
