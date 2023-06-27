using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata;

namespace random_school_generator
{

    internal class TitleScreen
    {
        private SpriteFont _consolasBold, _consolas;
        private List<MenuOption> _menuOptions;
        private int _selected;
        public TitleScreen()
        {
            _menuOptions = new List<MenuOption>();
            _selected = 0;
        }

        public void LoadTitleScreenContent(SpriteFont consolasBold, SpriteFont consolas)
        {
            _consolasBold = consolasBold;
            _consolas = consolas;
        }

        public void CreateTitleScreen()
        {
            _menuOptions.Add(new MenuOption("> run generator", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> exit", Color.DeepSkyBlue, Color.White));
        }

        public int UpdateTitleScreen(KeyboardState previousKeyboardState, KeyboardState currentKeyboardState)
        {
            if (previousKeyboardState.IsKeyDown(Keys.Enter) && currentKeyboardState.IsKeyUp(Keys.Enter))
            {
                return _selected;
            }
            if (previousKeyboardState.IsKeyDown(Keys.Down) && currentKeyboardState.IsKeyUp(Keys.Down) && _selected == 0)
            {
                _selected++;
            } else if (previousKeyboardState.IsKeyDown(Keys.Up) && currentKeyboardState.IsKeyUp(Keys.Up) && _selected == 1)
            {
                _selected--;
            }
            foreach (MenuOption m in _menuOptions)
            {
                if (_menuOptions.IndexOf(m) == _selected)
                {
                    m.IsSelected = true;
                } else
                {
                    m.IsSelected = false;
                }
            }
            return -1;
        }

       public void DrawTitleScreen(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            spriteBatch.DrawString(_consolasBold, "random school generator", new Vector2(5, 5), Color.White);
            for (int i = 0; i < _menuOptions.Count; i++)
            {
                spriteBatch.DrawString(_consolas, _menuOptions[i].Text, new Vector2(5, 50 * (i + 1)), _menuOptions[i].GetColour());
            }
            spriteBatch.End();
        }
    }
    
}
