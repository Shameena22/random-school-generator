using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class Zone : GrowableArea
    {
        private int _numberOfRooms, _idealSize, _ID, _area;
        private ZoneType _zoneType;
        private bool _firstGrown, _secondGrown, _thirdGrown;
        private List<Room> _rooms;
        public Zone(int numberOfRooms, int idealSize, string zoneType, int id) : base() 
        {
            _numberOfRooms = numberOfRooms;
            _idealSize = idealSize;
            _zoneType = new ZoneType(zoneType);
            _firstGrown = false;
            _secondGrown = false;
            _thirdGrown = false;
            _growthPoint = new Point (-100, -100);
            _floorRectangles.Add(new Rectangle());
            _ID = id;
            _area = 0;
            _rooms = new List<Room>();
        }

        public int NumberOfRooms { get => _numberOfRooms; set => _numberOfRooms = value; }
        public int IdealSize { get => _idealSize; set => _idealSize = value; }
        public bool FirstGrown { get => _firstGrown; set => _firstGrown = value; }
        public int ID { get => _ID; set => _ID = value; }
        internal ZoneType ZoneType { get => _zoneType; set => _zoneType = value; }
        public bool SecondGrown { get => _secondGrown; set => _secondGrown = value; }
        public bool ThirdGrown { get => _thirdGrown; set => _thirdGrown = value; }
        internal List<Room> Rooms { get => _rooms; set => _rooms = value; }
        public int NumberOfRooms1 { get => _numberOfRooms; set => _numberOfRooms = value; }
        public int Area { get => _area; set => _area = value; }

        public void UpdateArea()
        {
            _area = 0;
            for (int x = 0; x < _rectWidth; x++)
            {
                for (int y = 0; y < _rectHeight; y++)
                {
                    if (_grid[x, y] == (char)('0' | _ID))
                    {
                        _area++;
                    }
                }
            }
        }

        public void DrawZone(SpriteBatch spriteBatch, int scrollX, int scrollY)
        {
            //draws the base of the zone
            if (_floorRectangles.Count > 0)
            {
                foreach (Rectangle r in _floorRectangles)
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), ZoneType.TypeColours[_zoneType.Type]);
                }
            }
            foreach (Room r in _rooms)
            {
                r.DrawRoom(spriteBatch, scrollX, scrollY);
            }
        }
        
    }
}
