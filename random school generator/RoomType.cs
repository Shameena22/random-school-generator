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
        public RoomType(string type)
        {
            _type = type;
        }
        //TODO: add colours for a type!
        //darker variations of each type?

        public static void SetTypeColours()
        {
            TypeColours = new Dictionary<string, Color>
            {
                {"hall", Color.Ivory },
                {"gym", Color.CornflowerBlue },
                {"office", Color.Brown},
                {"canteen", Color.Chocolate },
                {"staffroom", Color.BurlyWood },
                {"toilets", Color.SeaShell },
                {"english", Color.LightGoldenrodYellow },
                {"maths", Color.Orchid},
                {"science", Color.Aqua },
                {"religious education", Color.MediumPurple},
                {"languages", Color.PeachPuff },
                {"computer science", Color.MidnightBlue },
                {"art", Color.DarkKhaki },
                {"design technology", Color.Olive },
                {"music", Color.Plum}
            };

        }

    }
}
