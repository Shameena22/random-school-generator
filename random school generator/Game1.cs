using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace random_school_generator
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private string _gameState;

        //keyboard states
        private KeyboardState _currentKeyboardState, _previousKeyboardState;

        //fonts
        private SpriteFont _consolasBold, _consolas;

        //option screens
        private TitleScreen _titleScreen;
        private SettingsScreen _settingsScreen;


        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width; 
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height ;
            _graphics.ApplyChanges();
            _gameState = "menu";

            //TODO: check if code below fits here
            _titleScreen = new TitleScreen();
            _settingsScreen = new SettingsScreen();

        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _titleScreen.CreateTitleScreen();
            _settingsScreen.CreateSettingsScreen();
;            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _consolasBold = Content.Load<SpriteFont>("ConsolasBold");
            _consolas = Content.Load<SpriteFont>("ConsolasNormal");

            _titleScreen.LoadTitleScreenContent(_consolasBold, _consolas);
            _settingsScreen.LoadSettingsScreenContent(_consolasBold, _consolas);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            int selectionOption;
            _currentKeyboardState = Keyboard.GetState();
            // TODO: Add your update logic here
            switch (_gameState) { 
                case "menu":
                    selectionOption = _titleScreen.UpdateTitleScreen(_previousKeyboardState, _currentKeyboardState);

                    if (selectionOption == 0)
                    {
                        _gameState = "settings";
                    } else if (selectionOption == 1)
                    {
                        Exit();
                    }
                    break;
                case "settings":
                    _settingsScreen.UpdateSettingsScreen(_previousKeyboardState, _currentKeyboardState);
                    break;
            }
            _previousKeyboardState = _currentKeyboardState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            switch (_gameState) {
                case "menu":
                    _titleScreen.DrawTitleScreen(_spriteBatch, GraphicsDevice);
                    break;
                case "settings":
                    _settingsScreen.DrawSettingsScreen(_spriteBatch, GraphicsDevice);
                    break;
            }

            base.Draw(gameTime);
        }
       
    }
}