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
        private List<string> _allSubjectOptions;
        private List<MenuOption> _menuOptions;
        private List<InputOption> _inputOptions, _subjectInputOptions;
        private int _selected, _inputSelected;
        private SpriteFont _consolas, _consolasBold;
        public SettingsScreen(List<string> allSubjects)
        {
            _selected = 0;
            _inputSelected = -1;
            _menuOptions = new List<MenuOption>();
            _inputOptions = new List<InputOption>();
            _subjectInputOptions = new List<InputOption>();
            _allSubjectOptions = new List<string>();
            _allSubjectOptions = allSubjects; 
        }

        public void CreateSettingsScreen()
        {
            //creating all menu options
            _menuOptions.Add(new MenuOption("> average floor size", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> number of floors", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> subject focus 1", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> subject focus 2", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> subject focus 3", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> floor irregularity", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> watch school generation", Color.DeepSkyBlue, Color.White));
            _menuOptions.Add(new MenuOption("> generate school", Color.Green, Color.MediumSeaGreen));

            //creating all input options
            _inputOptions.Add(new InputOption(validationType: "int", minNum: 75000, maxNum: 200000));
            _inputOptions.Add(new InputOption(validationType: "int", minNum: 2, maxNum: 10));
            _inputOptions.Add(new InputOption("dropdown", _allSubjectOptions));
            _inputOptions.Add(new InputOption("dropdown", _allSubjectOptions));
            _inputOptions.Add(new InputOption("dropdown", _allSubjectOptions));
            _inputOptions.Add(new InputOption("dropdown", new List<string> { "high", "medium", "low", "random"}));
            _inputOptions.Add(new InputOption("dropdown", new List<string> { "yes", "no"}));

            _subjectInputOptions.Add(_inputOptions[2]);
            _subjectInputOptions.Add(_inputOptions[3]);
            _subjectInputOptions.Add(_inputOptions[4]);

            for (int i = 0; i < _inputOptions.Count(); i++)
            {              
                _inputOptions[i].Position = new Vector2(250, 50 * (i + 1)); //set position of each input option
            }
        }

        public void LoadSettingsScreenContent(SpriteFont consolasBold, SpriteFont consolas)
        {
            _consolasBold = consolasBold;
            _consolas = consolas;

            //pass fonts to each input option for their use
            for (int i = 0; i < _inputOptions.Count(); i++)
            {
                _inputOptions[i].LoadInputOptionContent(_consolas, _consolasBold);
            }
        }

        public bool UpdateSettingsScreen(KeyboardState previousKeyboardState, KeyboardState currentKeyboardState)
        {
            //if no input options are currently selected
            if (_inputSelected < 0) 
            {
                //and user has pressed enter
                if (previousKeyboardState.IsKeyDown(Keys.Enter) && currentKeyboardState.IsKeyUp(Keys.Enter)) 
                {
                    if (_selected != _menuOptions.Count - 1)
                    {
                        //if not on final option (run generator), select input box corresponding to option
                        _inputSelected = _selected; 
                    } else if (!_menuOptions[_menuOptions.Count() - 1].IsDisabled) 
                    {
                        //if the user has selected "run generator" and the option isn't disabled, select it
                        _menuOptions[_menuOptions.Count() - 1].IsSelected = true;
                        return true; //so simulation knows this screen has ended
                    }
                } else
                {
                    //toggle selection via arrow keys
                    if (previousKeyboardState.IsKeyDown(Keys.Down) && currentKeyboardState.IsKeyUp(Keys.Down) && _selected < _menuOptions.Count() - 1)
                    {
                        _selected++;
                    }
                    else if (previousKeyboardState.IsKeyDown(Keys.Up) && currentKeyboardState.IsKeyUp(Keys.Up) && _selected > 0)
                    {
                        _selected--;
                    }

                    //update option status based on selection
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
                //if an input is currently selected, set its IsSelected value appropriately
                _inputOptions[_selected].IsSelected = true;
            }

            //updating each input option based on keys pressed
            foreach (InputOption i in _inputOptions)
            {             
                if (!i.UpdateInputOption(previousKeyboardState, currentKeyboardState)) //returns true when an input option has just been deselected
                {                  
                    _inputSelected = -1; //sets value to show no inputs are currently selected
                    if (_selected < _menuOptions.Count - 1)
                    {
                        _selected++; //moves selection down unless at final option
                    }

                }
            }
            //disables a subject option for a subject input if it has been selected in either of the other two subject inputs 
            foreach (InputOption i in _subjectInputOptions)
            {
                i.RefreshDropdownOptions(_subjectInputOptions[0].TextInBox, _subjectInputOptions[1].TextInBox, _subjectInputOptions[2].TextInBox);
            }

            //"run generator" option is only available once all options are given valid inputs
            if (CheckForEnd())
            {
                _menuOptions[_menuOptions.Count() - 1].IsDisabled = false;
            } else
            {
                _menuOptions[_menuOptions.Count() - 1].IsDisabled = true;
            }
            return false; //shows simulation that the settings screen has not ended
        }
        private bool CheckForEnd()
        {
            //returns true only if all options have a valid input
            foreach (InputOption i in _inputOptions)
            {
                if (!i.ValidInput) return false;
            }
            return true;
        }

        public void DrawSettingsScreen(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            InputOption _currentlySelectedInputOption = null;
            graphicsDevice.Clear(Color.Black);

            //draw title
            spriteBatch.DrawString(_consolasBold, "settings", new Vector2(5, 5), Color.White);

            //drawing each menu option, each below the last
            for (int i = 0; i < _menuOptions.Count(); i++)
            {
                spriteBatch.DrawString(_consolas, _menuOptions[i].Text, new Vector2(5, 50 * (i + 1)), _menuOptions[i].GetColour());
            }

            //drawing each input box, each below the last
            for (int i = 0; i < _inputOptions.Count(); i++)
            {
                if (_inputOptions[i].IsSelected)
                {
                    //doesn't draw currently selected input yet
                    _currentlySelectedInputOption = _inputOptions[i];
                } else
                {
                    //draws all unselected inputs in line w/ corresponding option
                    _inputOptions[i].DrawInputOption(spriteBatch, graphicsDevice);
                }          
            }

            //display additional messages guiding the user
            spriteBatch.DrawString(_consolas, "> NOTE: for options accepting integer input, enter 0 to make the number random.", new Vector2(5, 500), Color.Gray);
            spriteBatch.DrawString(_consolas, "> WARNING: large inputs may make the generator slow.", new Vector2(5, 525), Color.DeepPink);

            //draw selected input after everything else to prevent anything overlapping with its dropdown box
            if (_currentlySelectedInputOption is not null)
            {
                _currentlySelectedInputOption.DrawInputOption(spriteBatch, graphicsDevice);
            }    
            
        }

        public void ReturnValues(ref int floorSize, ref int numOfFloors, ref string subjectOne, ref string subjectTwo, ref string subjectThree, ref string floorIrregularity, ref string watchGeneration)
        {
            //passes on all values to simulation once user has exited this screen
            floorSize = Convert.ToInt32(_inputOptions[0].ReturnInput());
            numOfFloors = Convert.ToInt32(_inputOptions[1].ReturnInput());
            //make if all picked random - make sure not the same

            
            subjectOne = _inputOptions[2].ReturnInput();
            subjectTwo = _inputOptions[3].ReturnInput(subjectOne);
            subjectThree = _inputOptions[4].ReturnInput(subjectOne, subjectTwo);
            floorIrregularity = _inputOptions[5].ReturnInput();
            watchGeneration = _inputOptions[6].ReturnInput();
        }
    }
}
