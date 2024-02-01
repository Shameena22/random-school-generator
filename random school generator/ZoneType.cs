using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class ZoneType
    {
        private string _type, _secondaryType;
        public static List<(List<string>, string, int)> ZoneRules;
        public static Dictionary<string, List<string>> RoomTypes;
        private static int _adjacencyEncouragement, _adjacencyDiscouragement;
        public static Dictionary<string, Color> TypeColours;
        public string Type { get => _type; set => _type = value; }
        public string SecondaryType { get => _secondaryType; set => _secondaryType = value; }

        public ZoneType(string type)
        {
            _type = type;
            _secondaryType = SetSecondaryType();
        }

        // - load data -
        public static void LoadZoneRules()
        {
            //these values will later be used in each zone's weighted grid to help choose its growth position
            _adjacencyDiscouragement = -3;
            _adjacencyEncouragement = 3;

            //set secondary types for each room type
            RoomTypes = new Dictionary<string, List<string>>
            {
                { "classroom", new List<string> { "english", "maths", "religious education", "science", "languages", "computer science", "art", "design technology", "music" }},
                { "service", new List<string> {"toilets"}},
                { "staff", new List<string> { "staffroom", "office"} },
                { "large", new List<string> {"gym", "hall", "canteen"} }
            };

            //set rules for each zone (whether they should be adjacent or not)
            ZoneRules = new List<(List<string>, string, int)>
            {
                (new List<string> {"service, staff, large"}, "classroom", _adjacencyDiscouragement),
                (new List<string> {"languages", "religious education"}, "english", _adjacencyEncouragement),
                (new List<string> {"computer science", "science"}, "maths", _adjacencyEncouragement),
                (new List<string> {"art", "design technology"}, "music", _adjacencyEncouragement)
            };
        }
        public static void SetTypeColours()
        {
            //setting the base colours for each type of zone
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
                {"art", Color.Khaki },
                {"design technology", Color.Olive },
                {"music", Color.Plum}
            };

        }
        private string SetSecondaryType()
        {
            //sets the secondary type of the ZoneType based on its main type
            foreach (KeyValuePair<string, List<string>> kvp in RoomTypes)
            {
                if (kvp.Value.Contains(_type)) return kvp.Key;
            }
            return null;
        }

        public static int GetAdjacencyRule(ZoneType z1, ZoneType z2)
        {
            //returns the highest priority adjacency rule between two given zones

            //iterate through each stored rule and returns the encouragement / discouragement value if a rule applies to both zones given
            foreach ((List<string>, string, int) rule in ZoneRules)
            {

                //for discouragements, each type of room in the rule's key is only discouraged from the rule's value
                if (rule.Item3 == _adjacencyDiscouragement)
                {
                    //checks if one zone type is in the rule's key and the other zone type is in the rule's value
                    if ((rule.Item2 == z1.Type || rule.Item2 == z2.Type) && (rule.Item1.Contains(z1.Type) || rule.Item1.Contains(z2.Type)))
                    {
                        return _adjacencyDiscouragement;
                    }

                    //if not, checks the same with each zone's secondary type
                    if ((rule.Item2 == z1.SecondaryType || rule.Item2 == z2.SecondaryType) && (rule.Item1.Contains(z1.SecondaryType) || rule.Item1.Contains(z2.SecondaryType)))
                    {
                        return _adjacencyDiscouragement;
                    }
                }
                //for encouragements, each type of zone specified in the rule is encouraged to be with the others
                else
                {
                    //checks if both zones are present in the rule
                    if (rule.Item2 == z1.Type || rule.Item2 == z2.Type)
                    {
                        if (rule.Item1.Contains(z1.Type) || rule.Item1.Contains(z2.Type))
                        {
                            return _adjacencyEncouragement;
                        }
                    }
                    if (rule.Item1.Contains(z1.Type) && rule.Item1.Contains(z2.Type))
                    {
                        return _adjacencyEncouragement;
                    }
                }
            }

            //if no rules match and the zones are of the same secondary type, return some encouragement
            if (z1.SecondaryType == z2.SecondaryType)
            {
                return 1;
            }

            //if not, return no encouragement / discouragement
            return 0;
        }

    }
}
