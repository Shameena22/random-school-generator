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
       // private static Random _random = new Random();
        private static Dictionary<string, int> _sideLengths;
        private static Dictionary<string, Color> _chairColours;
        private static Dictionary<string, Color> _deskColours;
        private static Dictionary<string, Color> _teacherChairColours;
        private static Dictionary<string, Color> _teacherDeskColours;
        private static Dictionary<string, Color> _cupboardColours;
        private static Dictionary<string, Color> _subjDeskColours;
        public static Dictionary<string, Color> TypeColours { get => _typeColours; set => _typeColours = value; }
        public static Dictionary<string, Color> WallColours { get => _wallColours; set => _wallColours = value; }
        public static Dictionary<string, int> SideLengths { get => _sideLengths; set => _sideLengths = value; }
        public static Dictionary<string, Color> ChairColours { get => _chairColours; set => _chairColours = value; }
        public static Dictionary<string, Color> DeskColours { get => _deskColours; set => _deskColours = value; }
        public static Dictionary<string, Color> TeacherChairColours { get => _teacherChairColours; set => _teacherChairColours = value; }
        public static Dictionary<string, Color> TeacherDeskColours { get => _teacherDeskColours; set => _teacherDeskColours = value; }
        public static Dictionary<string, Color> CupboardColours { get => _cupboardColours; set => _cupboardColours = value; }
        public static Dictionary<string, Color> SubjDeskColours { get => _subjDeskColours; set => _subjDeskColours = value; }

        //dictionary of dictionary?
        //or....




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
                {"hall", Color.SaddleBrown },
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
                {"service", 75},
                {"staff", 100 }
            };

            //TODO: fix these
            _deskColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.RosyBrown },
                {"maths", Color.RosyBrown},
                {"science", Color.DarkTurquoise },
                {"religious education", Color.RosyBrown},
                {"languages", Color.RosyBrown },
                {"computer science", Color.MediumSlateBlue },
                {"art", Color.DarkSeaGreen },
                {"design technology", Color.DarkOrchid }, //todo: find out difference between YellowGreen and GreenYellow
                {"music", Color.Navy}
            };

            _chairColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.Pink },
                {"maths", Color.Pink},
                {"science", Color.DodgerBlue },
                {"religious education", Color.Pink},
                {"languages", Color.Pink },
                {"computer science", Color.CadetBlue },
                {"art", Color.GreenYellow },
                {"design technology", Color.YellowGreen },
                {"music", Color.PaleVioletRed}
            };

            _teacherChairColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.BurlyWood },
                {"maths", Color.BurlyWood},
                {"science", Color.Gray },
                {"religious education", Color.BurlyWood},
                {"languages", Color.BurlyWood },
                {"computer science", Color.CornflowerBlue },
                {"art", Color.SeaGreen },
                {"design technology", Color.DarkSalmon },
                {"music", Color.Pink}
            };

            _teacherDeskColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.Chocolate },
                {"maths", Color.Chocolate},
                {"science", Color.LightSeaGreen },
                {"religious education", Color.Chocolate},
                {"languages", Color.Chocolate },
                {"computer science", Color.DarkCyan },
                {"art", Color.DarkOliveGreen },
                {"design technology", Color.DarkMagenta },
                {"music", Color.RosyBrown}
            };

            _cupboardColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.BurlyWood },
                {"maths", Color.BurlyWood},
                {"science", Color.LightCyan },
                {"religious education", Color.BurlyWood},
                {"languages", Color.BurlyWood },
                {"computer science", Color.DarkTurquoise },
                {"art", Color.LightGreen },
                {"design technology", Color.Firebrick },
                {"music", Color.Tomato}
            };

            _subjDeskColours = new Dictionary<string, Color>
            {
                {"hall", Color.Brown },
                {"gym", Color.PowderBlue },
                {"office", Color.Silver},
                {"canteen", Color.RosyBrown},
                {"staffroom", Color.DarkGoldenrod },
                {"toilets", Color.HotPink },
                {"english", Color.SandyBrown },
                {"maths", Color.SandyBrown},
                {"science", Color.DimGray },
                {"religious education", Color.SandyBrown},
                {"languages", Color.SandyBrown},
                {"computer science", Color.DeepSkyBlue },
                {"art", Color.LimeGreen },
                {"design technology", Color.Honeydew },
                {"music", Color.Plum}
            };
        }


    }
}
