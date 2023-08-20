using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace random_school_generator
{
    internal class GrowableArea : Grid
    {
        protected int _rectWidth, _rectHeight;
        protected int[,] _weightedGrid;
        protected Point _growthPoint, _growthTopLeft;

        public GrowableArea() : base(0, 0)
        {

        }

        public int[,] WeightedGrid { get => _weightedGrid; set => _weightedGrid = value; }
        public Point GrowthPoint { get => _growthPoint; set => _growthPoint = value; }
        public Point GrowthTopLeft { get => _growthTopLeft; set => _growthTopLeft = value; }
        public int RectWidth { get => _rectWidth; set => _rectWidth = value; }
        public int RectHeight { get => _rectHeight; set => _rectHeight = value; }
    }
}
