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
        private bool _isSelected;
        private Color _selectedColour, _deselectedColour;
        public MenuOption(string name, Color selectedColour, Color deselectedColour)
        {
            _text = name;
            _selectedColour = selectedColour;
            _deselectedColour = deselectedColour;
        }
        public string Text { get => _text; set => _text = value; }
        public bool IsSelected { get => _isSelected; set => _isSelected = value; }
        public Color GetColour()
        {
            if (_isSelected)
            {
                return _selectedColour;
            }
            return _deselectedColour;
        }
    }
}
