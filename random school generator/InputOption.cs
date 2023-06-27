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
        private string _validationType;
        private List<string> _dropDownOptions;
        private bool _isSelected, _validInput;
        private int _selected, _maxNum, _minNum;
        private string _textInBox;
        private SpriteFont _consolas, _consolasBold;
        private Vector2 _position;
        private Color _colour;

        public Vector2 Position { get => _position; set => _position = value; }
        public bool IsSelected { get => _isSelected; set => _isSelected = value; }
        public bool ValidInput { get => _validInput; set => _validInput = value; }

        public InputOption(string validationType, List<string> dropDownOptions = null, int minNum = -1, int maxNum = -1 )
        {
            _validationType = validationType;
            _dropDownOptions = dropDownOptions;
            _selected = 0;
            _isSelected = false;
            _maxNum = maxNum;
            _minNum = minNum;

            switch (_validationType) 
            {
                case "int":
                    _textInBox = $" - enter an integer between {minNum} and {maxNum} - ";
                    break;
                case "dropdown":
                    _textInBox = $" - select an option - ";
                    break;
            }

        }

        public void LoadInputOptionContent(SpriteFont consolas, SpriteFont consolasBold)
        {
            _consolas = consolas;
            _consolasBold = consolasBold;
        }

        public bool UpdateInputOption(KeyboardState previousKeyboardState, KeyboardState currentKeyboardState)
        {
            if (_isSelected)
            {
                if (previousKeyboardState.IsKeyDown(Keys.Enter) && currentKeyboardState.IsKeyUp(Keys.Enter))
                {
                    _isSelected = false;
                    return false;
                } else
                {
                    switch (_validationType)
                    {
                        case "int":
                            if (_textInBox != "" && _textInBox[0] == ' ')
                            {
                                _textInBox = "";
                            }
                            _colour = Color.Blue;
                            if (previousKeyboardState.GetPressedKeys().Count() > 0)
                            {
                                if (currentKeyboardState.IsKeyUp(previousKeyboardState.GetPressedKeys()[0]))
                                {
                                    int numericValue = -100;
                                    Keys key = previousKeyboardState.GetPressedKeys()[0];
                                    try
                                    {
                                        numericValue = (int)Char.GetNumericValue(Convert.ToChar(key));
                                    }
                                    catch { };
                                    if (numericValue >= 0)
                                    {
                                        _textInBox += Convert.ToString(numericValue);
                                    }

                                } 
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
                            //ahhh
                            break;
                    }
                    
                }
                //nightmare...
            } else if (_validationType == "int")
            {
                if (ValidateInput())
                {
                    _colour = Color.Green;
                } else
                {
                    _colour = Color.Red;
                }
            }
            return true;
        }

        private bool ValidateInput()
        {
            bool valid = Int32.TryParse(_textInBox, out int tempInput);
            return valid && (tempInput >= _minNum && tempInput <= _maxNum || tempInput == 0);
        }

        public void DrawInputOption(SpriteBatch spriteBatch)
        {
            if (!_isSelected || _validationType == "int")
            {
               // spriteBatch.Begin();
                spriteBatch.DrawString(_consolas, _textInBox, _position, _colour);
                // spriteBatch.End();
            } else
            {

            }
        }
    }
}
