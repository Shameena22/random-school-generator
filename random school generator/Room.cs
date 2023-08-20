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

        public Room(int ID, string roomType) : base()
        {
            _ID = ID;
            _roomType = new RoomType(roomType);
        }

        public bool Grown { get => _grown; set => _grown = value; }
        public Point GrowthFloorPoint { get => _growthFloorPoint; set => _growthFloorPoint = value; }
        public int ID { get => _ID; set => _ID = value; }

        public void DrawRoom(SpriteBatch spriteBatch, int scrollX, int scrollY)
        {
            //draws the base of the zone
            if (_floorRectangles.Count > 0)
            {
                foreach (Rectangle r in _floorRectangles)
                {
                    spriteBatch.Draw(_pixel, new Rectangle(r.X - scrollX, r.Y - scrollY, r.Width, r.Height), Color.CadetBlue);
                }
            }
        }
    }
}
