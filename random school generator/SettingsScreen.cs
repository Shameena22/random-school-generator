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
    internal class SettingsScreen
    {
        private List<MenuOption> _menuOptions;
        private List<InputOption> _inputOptions;
        private int _selected, _inputSelected;
        private SpriteFont _consolas, _consolasBold;
        private bool _justDeselectedInput;
        public SettingsScreen()
        {
            _selected = 0;
            _inputSelected = -1;
            _menuOptions = new List<MenuOption>();
            _inputOptions = new List<InputOption>();
            _justDeselectedInput = false;
        }
        public void CreateSettingsScreen()
        {
            _menuOptions.Add(new MenuOption("> average floor size", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> number of floors", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> subject focus 1", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> subject focus 2", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> subject focus 3", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> floor complexity", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> watch school generation", Color.DeepSkyBlue, Color.White));

            //TODO: add input options
            _inputOptions.Add(new InputOption(validationType: "int", minNum: 50, maxNum: 500));
            _inputOptions.Add(new InputOption(validationType: "int", minNum: 1, maxNum: 10));
        }

        public void LoadSettingsScreenContent(SpriteFont consolasBold, SpriteFont consolas)
        {
            _consolasBold = consolasBold;
            _consolas = consolas;

            for (int i = 0; i < _inputOptions.Count(); i++)
            {
                _inputOptions[i].LoadInputOptionContent(_consolas, _consolasBold);
                _inputOptions[i].Position = new Vector2(250, 50 * (i + 1));
            }

        }

        public void UpdateSettingsScreen(KeyboardState previousKeyboardState, KeyboardState currentKeyboardState)
        {
            if (_inputSelected < 0) //if no input options are currently selected
            {
                if (previousKeyboardState.IsKeyDown(Keys.Enter) && currentKeyboardState.IsKeyUp(Keys.Enter))
                {
                    _inputSelected = _selected;
                } else
                {
                    if (previousKeyboardState.IsKeyDown(Keys.Down) && currentKeyboardState.IsKeyUp(Keys.Down) && _selected < _menuOptions.Count() - 1)
                    {
                        _selected++;
                    }
                    else if (previousKeyboardState.IsKeyDown(Keys.Up) && currentKeyboardState.IsKeyUp(Keys.Up) && _selected > 0)
                    {
                        _selected--;
                    }
                    foreach (MenuOption m in _menuOptions)
                    {
                        if (_menuOptions.IndexOf(m) == _selected)
                        {
                            m.IsSelected = true;
                        }
                        else
                        {
                            m.IsSelected = false;
                        }
                    }
                }
                
            } else
            {
                _inputOptions[_selected].IsSelected = true;
            }
            foreach (InputOption i in _inputOptions)
            {
                if (!i.UpdateInputOption(previousKeyboardState, currentKeyboardState))
                {
                    
                    _inputSelected = -1;
                    _selected++;
                    
                
                }
            }
           
        }

        public void DrawSettingsScreen(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            spriteBatch.DrawString(_consolasBold, "settings", new Vector2(5, 5), Color.White);
            for (int i = 0; i < _menuOptions.Count(); i++)
            {
                spriteBatch.DrawString(_consolas, _menuOptions[i].Text, new Vector2(5, 50 * (i + 1)), _menuOptions[i].GetColour());
            }
            for (int i = 0; i < _inputOptions.Count(); i++)
            {
                _inputOptions[i].DrawInputOption(spriteBatch);
            }
            spriteBatch.DrawString(_consolas, "> NOTE: for options accepting integer input, enter 0 to make the number random.", new Vector2(5, 500), Color.Gray);
            spriteBatch.End();
        }
    }
}
