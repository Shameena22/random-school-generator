using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    abstract class RoomType
    {
       // private string _type;
        private static Dictionary<string, Color> _typeColours;
        private static Dictionary<string, Color> _wallColours;
       // public string Type { get => _type; set => _type = value; }
        private static Random _random = new Random();
        private static Dictionary<string, int> _sideLengths;

        public static Dictionary<string, Color> TypeColours { get => _typeColours; set => _typeColours = value; }
        public static Dictionary<string, Color> WallColours { get => _wallColours; set => _wallColours = value; }
        public static Dictionary<string, int> SideLengths { get => _sideLengths; set => _sideLengths = value; }


        //public RoomType(string type)
        //{
        //    _type = type;
        //    _random = new Random();
        //}

        //public static void LoadData()
        //{

        //}
        public static void LoadData()
        {
            //setting colours of base for each type of room
            _typeColours = new Dictionary<string, Color>
            {
                {"hall", Color.Gold },
                {"gym", Color.AliceBlue }, //homage to sgs pre-dutton era gym floor
                {"office", Color.BlanchedAlmond},
                {"canteen", Color.DarkGoldenrod},
                {"staffroom", Color.Goldenrod },
                {"toilets", Color.LightPink },
                {"english", Color.Goldenrod },
                {"maths", Color.DarkOrchid},
                {"science", Color.Azure },
                {"religious education", Color.Indigo},
                {"languages", Color.Lavender },
                {"computer science", Color.BlueViolet },
                {"art", Color.DarkKhaki },
                {"design technology", Color.DarkGreen },
                {"music", Color.DarkMagenta}
            };

            //setting colours of wall for each type of room
            _wallColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.DarkGoldenrod },
                {"maths", Color.MediumPurple},
                {"science", Color.DarkBlue },
                {"religious education", Color.DarkSlateBlue},
                {"languages", Color.Purple },
                {"computer science", Color.Purple },
                {"art", Color.ForestGreen },
                {"design technology", Color.GreenYellow },
                {"music", Color.Purple}
            };

            //setting min side lengths based on room's zone secondary type
            _sideLengths = new Dictionary<string, int>
            {
                {"classroom", 125 },
                {"large", 150 },
                { "service", 75},
                {"staff", 100 }
            };
        }


    }
}
