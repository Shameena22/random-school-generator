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
        private int _numberOfRooms, _idealSize, _ID, _area, _roomGrowthRetries;
        private ZoneType _zoneType;
        private bool _firstStageGrown, _secondStageGrown, _thirdStageGrown, _roomGrowthFailed, _finishedDoors;
        private List<Room> _rooms;
        private List<Point> _badGrowthPoints;

        public int NumberOfRooms { get => _numberOfRooms; set => _numberOfRooms = value; }
        public int IdealSize { get => _idealSize; set => _idealSize = value; }
        public bool FirstStageGrown { get => _firstStageGrown; set => _firstStageGrown = value; }
        public int ID { get => _ID; set => _ID = value; }
        internal ZoneType ZoneType { get => _zoneType; set => _zoneType = value; }
        public bool SecondStageGrown { get => _secondStageGrown; set => _secondStageGrown = value; }
        public bool ThirdStageGrown { get => _thirdStageGrown; set => _thirdStageGrown = value; }
        internal List<Room> Rooms { get => _rooms; set => _rooms = value; }
        public int Area { get => _area; set => _area = value; }
        public List<Point> BadGrowthPoints { get => _badGrowthPoints; set => _badGrowthPoints = value; }
        public int RoomGrowthRetries { get => _roomGrowthRetries; set => _roomGrowthRetries = value; }
        public bool RoomGrowthFailed { get => _roomGrowthFailed; set => _roomGrowthFailed = value; }
        public bool FinishedDoors { get => _finishedDoors; set => _finishedDoors = value; }
        public Zone(int numberOfRooms, int idealSize, string zoneType, int id) : base()
        {
            _numberOfRooms = numberOfRooms;
            _idealSize = idealSize;
            _zoneType = new ZoneType(zoneType);
            _firstStageGrown = false;
            _secondStageGrown = false;
            _thirdStageGrown = false;
            _growthPoint = new Point(-100, -100);
            _floorRectangles.Add(new Rectangle());
            _badGrowthPoints = new List<Point>();
            _ID = id;
            _area = 0;
            _rooms = new List<Room>();
            _roomGrowthRetries = 0;
            _roomGrowthFailed = false;
            _finishedDoors = false;
        }

        // - update -
        public void UpdateArea()
        {
            //go through floor grid and count total number of points belonging to the zone
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

        // - display
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

            //draws each room over the base
            foreach (Room r in _rooms)
            {
                r.DrawRoom(spriteBatch, scrollX, scrollY);
            }
        }
        
    }
}
