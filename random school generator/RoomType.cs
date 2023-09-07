using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class RoomType
    {
        private string _type;
        public static Dictionary<string, Color> TypeColours;
        public static Dictionary<string, Color> WallColours;
        public string Type { get => _type; set => _type = value; }

        public RoomType(string type)
        {
            _type = type;
        }

        public static void SetTypeColours()
        {
            //setting colours for each type of room

            TypeColours = new Dictionary<string, Color>
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

            WallColours = new Dictionary<string, Color>
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

        }

    }
}
