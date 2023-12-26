using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class MenuOption
    {
        private string _text;
        private bool _isSelected, _isDisabled;
        private Color _selectedColour, _deselectedColour;
        public MenuOption(string name, Color selectedColour, Color deselectedColour)
        {
            _text = name;
            _selectedColour = selectedColour;
            _deselectedColour = deselectedColour;
            _isDisabled = false;
        }
        public string Text { get => _text; set => _text = value; }
        public bool IsSelected { get => _isSelected; set => _isSelected = value; }
        public bool IsSelected1 { get => _isSelected; set => _isSelected = value; }
        public bool IsDisabled { get => _isDisabled; set => _isDisabled = value; }

        public Color GetColour()
        {
            //returns colour of option depending if it is currently selected or not
            if (_isDisabled)
            {
                return Color.DarkGray;
            }
            if (_isSelected)
            {
                return _selectedColour;
            }
            return _deselectedColour;
        }
    }
}
