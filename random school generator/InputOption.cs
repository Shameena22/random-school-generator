using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace random_school_generator
{
    internal class InputOption
    {
        private List<string> _dropDownOptions;
        private List<MenuOption> _menuOptions;
        private bool _isSelected, _validInput;
        private int _selected, _maxNum, _minNum, _dropdownBoxWidth, _dropdownBoxHeight;
        private string _textInBox, _validationType;
        private SpriteFont _consolas, _consolasBold;
        private Vector2 _position;
        private Color _colour;
        private Color[] _colourData;
        private Texture2D _dropdownRect;

        public Vector2 Position { get => _position; set => _position = value; }
        public bool IsSelected { get => _isSelected; set => _isSelected = value; }
        public bool ValidInput { get => _validInput; }
        public string TextInBox { get => _textInBox;}
        public bool IsSelected1 { get => _isSelected; set => _isSelected = value; }

        public InputOption(string validationType, List<string> dropDownOptions = null, int minNum = -1, int maxNum = -1, int dropdownBoxWidth = 300)
        {
            _validationType = validationType;
            _dropDownOptions = dropDownOptions;
            _selected = 0;
            _isSelected = false;
            _maxNum = maxNum;
            _minNum = minNum;
            _menuOptions = new List<MenuOption>();
            _colour = Color.Red;
          
            switch (_validationType)
            {
                //set initial text depending on input type
                case "int":
                    _textInBox = $" - enter an integer between {_minNum} and {_maxNum} - ";
                    break;
                case "dropdown":
                    _textInBox = $" - select an option - ";

                    //creates a list of options based on list passed from _settingsScreen
                    foreach (string s in _dropDownOptions)
                    {
                        _menuOptions.Add(new MenuOption("> "+ s, Color.White, Color.Black));
                    }

                    //sets width and height of dropdown box depending on how many options there are
                    _dropdownBoxWidth = dropdownBoxWidth;
                    _dropdownBoxHeight = _menuOptions.Count() * 50;

                    //sets data to draw the rectangle grey
                    _colourData = new Color[_dropdownBoxHeight * _dropdownBoxWidth];
                    for (int i = 0; i < _colourData.Count(); i++)
                    {
                        _colourData[i] = Color.Gray;
                    }
                    break;
            }
        }
        
        // - loading data -
        public void LoadInputOptionContent(SpriteFont consolas, SpriteFont consolasBold)
        {
            _consolas = consolas;
            _consolasBold = consolasBold;
        }
        
        // - updating -
        public bool UpdateInputOption(KeyboardState previousKeyboardState, KeyboardState currentKeyboardState)
        {
            //updates what the input option contains depending on its type and the user's most recent input

            if (_isSelected)
            {
                //if user has pressed the enter key
                if (previousKeyboardState.IsKeyDown(Keys.Enter) && currentKeyboardState.IsKeyUp(Keys.Enter))
                {
                    //if the user hasn't selected a disabled dropddown option
                    if (!(_validationType == "dropdown" && _menuOptions[_selected].IsDisabled))
                    {
                        //automatically accepts input if it was from a dropdown list  
                        if (_validationType == "dropdown") _validInput = true;

                        //resets selection
                        _isSelected = false;

                        //indicates to _settingsScreen that the button is no longer selected
                        return false;
                    }
                   
                } else
                {
                    switch (_validationType)
                    {
                        case "int":

                            //clears the input text if it is not a number
                            if (_textInBox != "" && _textInBox[0] == ' ') 
                            {
                                _textInBox = "";
                            }

                            //sets colour to selection colour (blue)
                            _colour = Color.Blue;

                            //if the same key has been pushed and released
                            if (previousKeyboardState.GetPressedKeys().Count() > 0)
                            {
                                if (currentKeyboardState.IsKeyUp(previousKeyboardState.GetPressedKeys()[0]))
                                {
                                    //temp variable for number input
                                    int numericValue = -100;

                                    //store pressed key
                                    Keys key = previousKeyboardState.GetPressedKeys()[0];

                                    //try to convert key to a 1-digit number; will be negative if failed
                                    //only works if the user has pressed a number key
                                    try
                                    {
                                        numericValue = (int)Char.GetNumericValue(Convert.ToChar(key));
                                    }
                                    catch { };

                                    //if attempt successful, add 1-digit number to input
                                    if (numericValue >= 0)
                                    {
                                        _textInBox += Convert.ToString(numericValue);
                                    }
                                } 

                                //if the user has pressed the backspace key, remove the last digit
                                if (previousKeyboardState.IsKeyDown(Keys.Back) && currentKeyboardState.IsKeyUp(Keys.Back))
                                {
                                    if (_textInBox != "")
                                    {
                                        _textInBox = _textInBox.Substring(0, _textInBox.Count() - 1);
                                    }
                                }
                            }
                            break;

                        case "dropdown":
 
                            //toggle selection of dropdown menu via arrow keys
                            if (previousKeyboardState.IsKeyDown(Keys.Up) && currentKeyboardState.IsKeyUp(Keys.Up) && _selected > 0)
                            {
                                _selected--;
                            } else if (previousKeyboardState.IsKeyDown(Keys.Down) && currentKeyboardState.IsKeyUp(Keys.Down) && _selected < _menuOptions.Count - 1)
                            {
                                _selected++;
                            }

                            //update each dropdown option's status based on selection
                            foreach (MenuOption m in _menuOptions)
                            {
                                //selecs option if it hasn't been disabled
                                if (_menuOptions.IndexOf(m) == _selected && !m.IsDisabled)
                                {
                                    m.IsSelected = true;
                                    _colour = Color.Green;

                                    //remove "> " at start of option
                                    _textInBox = m.Text.Substring(2);

                                } else
                                {
                                    m.IsSelected = false;
                                }
                            }
                            break;
                    }
                    
                }
            } else if (_validationType == "int")
            {
                //sets colour based on validity of integer input
                if (ValidateInput())
                {
                    _validInput = true;
                    _colour = Color.Green;
                } else
                {
                    _validInput = false;
                    _colour = Color.Red;
                }
            }

            return true;
        }
        private bool ValidateInput()
        {
            //attempt to convert text input to an integer
            bool valid = Int32.TryParse(_textInBox, out int tempInput);

            //if successful, checks if it is within the boundaries set (or if set to 0 for random)
            return valid && (tempInput >= _minNum && tempInput <= _maxNum || tempInput == 0);
        }
        public void RefreshDropdownOptions(string selectedOption1, string selectedOption2, string selectedOption3)
        {
            //ensures that subject options that are currently selected are disabled, and otherwise enabled
            //this sub is only used for subject options

            string text;

            foreach (MenuOption m in _menuOptions)
            {
                //remove "> " at start of each option to compare
                text = m.Text.Substring(2);

                if (text != _textInBox && text != "random")
                {
                    //disables option if it is selected in either of the other two subject inputs
                    if (text == selectedOption1 || text == selectedOption2 || text == selectedOption3)
                    {
                        m.IsDisabled = true;
                    } else
                    {
                        m.IsDisabled = false;
                    }

                } else
                {
                    //if the option hasn't been selected in any of the other inputs, it is available to use
                    m.IsDisabled = false;
                }
            }
        }
        
        // - display + finish -
        public void DrawInputOption(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            if (!_isSelected || _validationType == "int")
            {
                //draw option text if it isn't currently showing a dropdown box
                spriteBatch.DrawString(_consolas, _textInBox, _position, _colour);

            } else
            {
                //create box rectangle and set its colour
                _dropdownRect = new Texture2D(graphicsDevice, _dropdownBoxWidth, _dropdownBoxHeight );
                _dropdownRect.SetData(_colourData);

                //draw the rectangle close to the set position
                //position is based on amount of dropdown options
                spriteBatch.Draw(_dropdownRect, new Rectangle((int)_position.X, (int)_position.Y - (15 * _menuOptions.Count()), _dropdownBoxWidth, _dropdownBoxHeight), Color.White);

                //draw each dropdown option within the box, each below the other
                for (int i = 0; i < _menuOptions.Count(); i++)
                {
                    spriteBatch.DrawString(_consolas, _menuOptions[i].Text, new Vector2(_position.X + 10, (_position.Y - (15 * _menuOptions.Count()) + 10 + 50*i)), _menuOptions[i].GetColour());
                }
            }
        }     
        public string ReturnInput(string chosenOption1 = "", string chosenOption2 = "")
        {
            //returns final input to _settingsScreen
            Random r = new Random();

            switch (_validationType) 
            {
                case "int":       
                    
                    //choose a random number within the boundaries if the user selected 0 for random
                    if (_textInBox == "0")
                    {
                        return $"{r.Next(_minNum, _maxNum + 1)}";
                    }

                    //if not random, return inputted text
                    return _textInBox;

                case "dropdown":

                    //choose a random option that hasn't been disabled / already selected if the user selected "random"
                    if (_textInBox == "random")
                    {
                        List<string> enabledOptions = new List<string>();
                        foreach (MenuOption m in _menuOptions)
                        {
                            if (!m.IsDisabled && chosenOption1 != m.Text.Substring(2) && chosenOption2 != m.Text.Substring(2))
                            {
                                enabledOptions.Add(m.Text);
                            }
                        }
                        return enabledOptions[r.Next(0, enabledOptions.Count())].Substring(2);
                    }

                    //if not random, return inputted text
                    return _textInBox;

                default:
                    //option has been incorrectly made if it isn't an int or dropdown type, so return an empty string
                    return "";
            }

        }
    }
}
