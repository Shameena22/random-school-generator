using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace random_school_generator
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private string _gameState;
        private int _gameStateIndex;
        private List<string> _gameStates;
        private Random _random;

        //keyboard states
        private KeyboardState _currentKeyboardState, _previousKeyboardState;

        //fonts
        private SpriteFont _consolasBold, _consolas;

        //option screens
        private TitleScreen _titleScreen;
        private SettingsScreen _settingsScreen;

        //data
        private int _floorSize, _numOfFloors, _floorIrregularity, _screenWidth, _screenHeight;
        private string _subjectOne, _subjectTwo, _subjectThree, _irregularityText, _watchGeneration;
        private List<string> _allSubjectOptions;
        private DateTime _previousUpdateTime;

        //drawing + display
        private int _scrollX, _scrollY;
        private int _currentFloorIndex, _currentZoneIndex, _currentRoomIndex;
        private Queue<string> _displayMessages;
        private int _timeBetweenDisplayChange;

        //building
        private List<Floor> _allFloors;

        //TODO: create constants..

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.ApplyChanges();
            _gameState = "menu";

            //TODO: check if code below fits here
            _allSubjectOptions = new List<string> { "english", "maths", "science", "religious education", "languages", "computer science", "art", "design technology", "music", "random" };
            _gameStates = new List<string> { "menu", "settings", "create floors", "create graphs", "create stairs", "grow rectangular zones", "create corridors", "create rooms" };
            _gameStateIndex = 0;
            _currentZoneIndex = 0;
            _titleScreen = new TitleScreen();
            _settingsScreen = new SettingsScreen(_allSubjectOptions);
            _scrollX = 0;
            _scrollY = 0;
            _currentFloorIndex = 0;
            _previousUpdateTime = new DateTime();
            _displayMessages = new Queue<string>();
            _screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _random = new Random();
            _timeBetweenDisplayChange = 500;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _titleScreen.CreateTitleScreen();
            _settingsScreen.CreateSettingsScreen();
            Floor.SetComponentColours();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            //load fonts for game
            _consolasBold = Content.Load<SpriteFont>("ConsolasBold");
            _consolas = Content.Load<SpriteFont>("ConsolasNormal");

            //pass fonts to other screens for their use
            _titleScreen.LoadTitleScreenContent(_consolasBold, _consolas);
            _settingsScreen.LoadSettingsScreenContent(_consolasBold, _consolas);

            //load pixel data for the Floor class
            Grid.LoadGridPixelData(GraphicsDevice);
            ZoneType.LoadZoneRules();
            ZoneType.SetTypeColours();
        }

        protected override void Update(GameTime gameTime)
        {
            string currentMessage = "";
            _currentKeyboardState = Keyboard.GetState();
            // TODO: Add your update logic here

            //return to previous menu / exit if esc pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (_previousKeyboardState.IsKeyDown(Keys.Escape) && _currentKeyboardState.IsKeyUp(Keys.Escape)))
            {
                if (_gameStateIndex > 0)
                {
                    ResetValues();
                    if (_gameStateIndex == 1)
                    {
                        //return to title screen if user presses esc at settings screen
                        _gameStateIndex--;
                    }
                    else
                    {
                        //return to settings screen if user presses esc during the school generation
                        _gameStateIndex = 1;
                    }
                }
                else
                {
                    //exit if user presses esc at title screen
                    Exit();
                }
            }

            //set game state text
            _gameState = _gameStates[_gameStateIndex];

            //call appropriate subroutine based on game state
            switch (_gameState)
            {
                case "menu":
                    UpdateMenu();
                    break;
                case "settings":
                    UpdateSettings();
                    break;
                case "create floors":
                    UpdateScroll();
                    currentMessage = UpdateFloorCreation();
                    break;
                case "create graphs":
                    currentMessage = UpdateFloorGraphs();
                    break;
                case "create stairs":
                    UpdateScroll();
                    currentMessage = UpdateStairCreation();
                    break;
                case "grow rectangular zones":
                    UpdateScroll();
                    currentMessage = UpdateZoneGrowth();
                    break;
                case "create corridors":
                    UpdateScroll();
                    currentMessage = UpdateCorridorCreation();
                    break;
                case "create rooms":
                    UpdateScroll();
                    currentMessage = UpdateRoomGrowth();
                    break;
            }

            //update message queue with the message stored
            UpdateMessageQueue(currentMessage);

            //store keyboard state for use in the next call, necessary to check if a button has been pressed and released
            _previousKeyboardState = _currentKeyboardState;
            base.Update(gameTime);
        }

        // - update menu -
        private void UpdateMenu()
        {
            //fetches option selected by user at title screen
            int selectionOption = _titleScreen.UpdateTitleScreen(_previousKeyboardState, _currentKeyboardState);

            //transfers to next game state depending on user selection
            if (selectionOption == 0)
            {
                _gameStateIndex++;
            }
            else if (selectionOption == 1)
            {
                Exit();
            }
        }

        // - update settings -
        private void UpdateSettings()
        {
            //returns true once the user has exited the settings screen
            if (_settingsScreen.UpdateSettingsScreen(_previousKeyboardState, _currentKeyboardState))
            {
                //get results of all inputs from settings screen
                _settingsScreen.ReturnValues(ref _floorSize, ref _numOfFloors, ref _subjectOne, ref _subjectTwo, ref _subjectThree, ref _irregularityText, ref _watchGeneration);
                _floorIrregularity = SetComplexity();

                //switch to next game state
                if (_watchGeneration == "yes")
                {
                    _gameStateIndex++;
                }
                else
                {
                    //TODO: go straight to "view school" mode
                }
            }
        }
        private int SetComplexity()
        {
            switch (_irregularityText)
            {
                case "high":
                    return 20;
                case "medium":
                    return 10;
                case "low":
                    return 5;
                default:
                    return 0;
            }
        }

        // - update floor generation -
        private string UpdateFloorCreation()
        {
            //if no floors have been created yet, create them + set update time
            if (_allFloors is null)
            {
                CreateFloors();
                _previousUpdateTime = DateTime.Now;
            }

            //update to change which floor should be displayed
            UpdateFloorDisplay();

            //move to next game state if final floor has been displayed
            if (_currentFloorIndex == _allFloors.Count)
            {
                FindAllFloorEdgePoints();
                _gameStateIndex++;
                return "";
            }
            else
            {
                //return message about creating floor
                return $"> created floor {_currentFloorIndex + 1}";
            }
        }
        private void CreateFloors()
        {
            List<Floor> floorsBelow = new List<Floor>();
            _allFloors = new List<Floor>();

            //set max grid size
            int gridDimensions = (int)(Math.Sqrt(_floorSize) * 1.75);

            for (int i = 0; i < _numOfFloors; i++)
            {
                //create each floor object
                _allFloors.Add(new Floor(i, _floorSize, gridDimensions));

                //keep a list of all the floors below the current one...
                if (i > 0)
                {
                    floorsBelow.Add(_allFloors[i - 1]);
                }
                //...and pass this to create the floor's grid
                _allFloors[i].CreateFloorGrid(_floorIrregularity, floorsBelow);
            }
        }
        private void UpdateFloorDisplay()
        {
            //if enough time has passed between now and the last floor display update
            if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange))
            {
                //change the floor to be displayed
                _previousUpdateTime = DateTime.Now;
                _currentFloorIndex++;
            }
        }

        // - update floor graph generation -
        private string UpdateFloorGraphs()
        {
            //create graphs for each floor
            CreateFloorGraphs();

            //order all zones by size ascending...will be useful when growing zone shapes
            foreach (Floor f in _allFloors)
            {
                f.Zones = f.Zones.OrderBy(z => z.IdealSize).ToList(); 
            }

            //move onto next state
            _gameStateIndex++;
            return "> created floor hierarchy graphs";
        }
        private void CreateFloorGraphs()
        {
            List<string> randomAvailableZones, chosenZones, availableZoneTypes = new List<string> { "english", "maths", "science", "religious education", "languages", "computer science", "art", "design technology", "music", "staffroom", "office" };
            List<float> topThreeChances;
            int numOfZones, count = 0, i = 0, tempIdealSize, tempNumOfRooms, staffRoomFloor, zoneLimit;
            float tempChance, lowerZoneLimitDivider, upperZoneLimitDivider, lowerRoomLimitDivider, upperRoomLimitDivider;

            //tweak these values to improve zone list generation:
            // - max num of zones on one floor
            zoneLimit = 8;
            // - max area of a zone (loose estimate)
            lowerZoneLimitDivider = 100000f;
            // - min area of a zone (loose estimate)
            upperZoneLimitDivider = 50000f;
            // - max area of a room (loose estimate)
            lowerRoomLimitDivider = 15000f;
            // - min area of a room (loose estimate)
            upperRoomLimitDivider = 13000f;

            //storing probability of choosing a zone alongside its name
            Dictionary<string, float> zoneGraphChances = new Dictionary<string, float>(), zoneSizeProportions;

            //add each zone type to the dictionary; initial values are 0
            foreach (string s in availableZoneTypes)
            {
                zoneGraphChances.Add(s, 0);
            }
            //special rooms are given an initial value of 1 so they appear at the top of the list of chosen zones once it has been sorted
            zoneGraphChances.Add("gym", 1);
            zoneGraphChances.Add("hall", 1);
            zoneGraphChances.Add("canteen", 1);
            //similarly, toilets are given a lower value so they appear lower in the chosen zones list
            zoneGraphChances.Add("toilets", 0.2f);
            //and the staffroom won't be too large / too small
            zoneGraphChances["staffroom"] = 0.5f;
            //zone types closer to the front will be allocated larger areas

            //allocate a floor to have a staff room, ensuring that the building has at least one staffroom area
            staffRoomFloor = _random.Next(0, _allFloors.Count);

            foreach (Floor f in _allFloors)
            {
                count = 0;
                i = 0;
                chosenZones = new List<string>();

                //the top 3 subjects will be given higher probabilities
                topThreeChances = new List<float> { _random.Next(75, 100) / 100f, _random.Next(65, 100) / 100f, _random.Next(55, 100) / 100f };

                //allocating highest -> lowest values from this list to highest -> lowest subject priorities
                topThreeChances.Sort();
                zoneGraphChances[_subjectOne] = topThreeChances[0];
                zoneGraphChances[_subjectTwo] = topThreeChances[1];
                zoneGraphChances[_subjectThree] = topThreeChances[2];

                //allocate probabilities to all the other zone types, ensuring that they can't be too high
                foreach (string s in availableZoneTypes)
                {
                    if (zoneGraphChances[s] == 0)
                    {
                        zoneGraphChances[s] = _random.Next(0, 75) / 100f;
                    }
                }

                //shuffling the list of zone types
                randomAvailableZones = availableZoneTypes.OrderBy(x => _random.Next()).ToList();

                //generate a random number of zones
                //boundaries based on total room area
                numOfZones = _random.Next((int)Math.Ceiling(f.TotalArea / lowerZoneLimitDivider), (int)Math.Ceiling(f.TotalArea / upperZoneLimitDivider));
                numOfZones = Math.Min(numOfZones, zoneLimit);
                do
                {
                    //random number between 0 and 1 - its size determines if the zone will be added to the floor
                    tempChance = _random.Next(0, 100) / 100f;

                    //add zone to floor if it hasn't already been added and tempChance is less than the zone's set probability
                    if (!chosenZones.Contains(randomAvailableZones[i]) && tempChance < zoneGraphChances[randomAvailableZones[i]])
                    {
                        chosenZones.Add(randomAvailableZones[i]);
                        count++;
                    }

                    //increment i to iterate though each zone type; reset to 0 once it has done a full pass
                    i++;
                    if (i == randomAvailableZones.Count)
                    {
                        i = 0;
                    }
                    //continue until the quota of zones has been reached
                } while (count < numOfZones);

                //adding these zones to the ground floor only
                if (f.FloorID == 0)
                {
                    chosenZones.Add("gym");
                    chosenZones.Add("hall");
                    chosenZones.Add("canteen");
                }
                else if (f.FloorID == staffRoomFloor && !chosenZones.Contains("staffroom"))
                {
                    chosenZones.Add("staffroom");
                }
                //each floor is given a toilet zone
                chosenZones.Add("toilets");

                //calculate the proportions of the total floor size that each zone will take
                zoneSizeProportions = CreateZoneSizeProportions(chosenZones, zoneGraphChances);

                foreach (KeyValuePair<string, float> kvp in zoneSizeProportions)
                {
                    //set the ideal size using the floor's total size and the zone's proportion value
                    tempIdealSize = (int)(kvp.Value * f.TotalArea);

                    if (tempIdealSize > 0)
                    {
                        //calculate the desired number of rooms based on the allocated sizes
                        tempNumOfRooms = SetNumberOfRooms(kvp.Key, tempIdealSize, lowerRoomLimitDivider, upperRoomLimitDivider);

                        //add the zone to the floor's list
                        f.Zones.Add(new Zone(tempNumOfRooms, tempIdealSize, kvp.Key, chosenZones.IndexOf(kvp.Key)));
                    }

                }
            }
        }
        private Dictionary<string, float> CreateZoneSizeProportions(List<string> chosenZones, Dictionary<string, float> zoneChances)
        {
            Dictionary<string, float> zoneSizeProportions = new Dictionary<string, float>();
            List<float> sizeProportions = new List<float>();
            float total = 1, temp;

            //generating a list of decimals which all add up to 1
            for (int i = 0; i < chosenZones.Count - 1; i++)
            {
                temp = _random.Next(0, (int)(total * 75)) / 100f;
                sizeProportions.Add(temp);
                total -= temp;
            }
            sizeProportions.Add(total);

            //sort the list of decimals in ascending order
            sizeProportions.Sort();
            //sort the list of zones in ascending order based on their probability value
            chosenZones = chosenZones.OrderBy(x => zoneChances[x]).ToList();

            //store both lists' values aligned with each other as entries in a dictionary and return that
            for (int i = 0; i < chosenZones.Count; i++)
            {
                if (sizeProportions[i] > 0.15 || chosenZones[i] == "toilets")
                zoneSizeProportions.Add(chosenZones[i], sizeProportions[i]);
            }
            return zoneSizeProportions;
        }
        private int SetNumberOfRooms(string s, int idealSize, float lowerRoomLimitDivider, float upperRoomLimitDivider)
        {
            if (s == "gym" || s == "hall" || s == "canteen")
            {
                //gyms, halls, and canteens are not split into separate rooms
                return 1;
            }
            else if (s == "toilets" || s == "office")
            {
                //preventing too many toilets from being generated in a floor by restricting their number from 1 to 6
                return _random.Next(1, 6);
            }
            else

            {
                //for normal zone types, calculate the number of rooms based on the zone's allocated area
                int tempNumOfRooms = _random.Next((int)(idealSize / lowerRoomLimitDivider), (int)(idealSize / upperRoomLimitDivider));

                //the number of rooms should be between 1 and 8
                tempNumOfRooms = Math.Max(1, tempNumOfRooms);
                tempNumOfRooms = Math.Min(tempNumOfRooms, 8);

                return tempNumOfRooms;
            }
        }

        // - update stair generation -
        private string UpdateStairCreation()
        {
            int stairWidth, stairLength;
            List<Rectangle> possibleStairRectangles = new List<Rectangle>(), sharedStairEdgeRectangles;
            Rectangle chosenRect;
            bool horizontalEdge, verticalEdge;

            //change these values to alter the size of the stair block
            stairWidth = 20;
            stairLength = 25;

            //move onto the next floor if enough time has passed since the last floor update 
            if (_currentFloorIndex == _allFloors.Count || DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange))
            {
                _previousUpdateTime = DateTime.Now;
                _currentFloorIndex--;
            }

            //only add stairs if this floor doesn't already have enough (1 block for top and ground floors, 2 for the rest)
            if (_currentFloorIndex > 0 && ((_currentFloorIndex < _allFloors.Count - 1 && _allFloors[_currentFloorIndex].StairPoints.Count <= 1) || (_currentFloorIndex == _allFloors.Count - 1 && _allFloors[_currentFloorIndex].StairPoints.Count == 0)))
            {
                Floor f = _allFloors[_currentFloorIndex];

                //iterate through every edge point on the floor
                foreach (Point p in f.Edgepoints)
                {
                    horizontalEdge = true;
                    verticalEdge = true;

                    //check if the point could be the corner of a stair block, and what direction if so
                    CheckEdgeAlignments(p, stairLength, stairWidth, f, ref horizontalEdge, ref verticalEdge);

                    //add every possible stair rectangle for this point to the list possibleStairPoints
                    if (horizontalEdge)
                    {
                        AddPossibleRectangles(ref possibleStairRectangles, "horizontal", stairLength, stairWidth, p, f);
                    }
                    if (verticalEdge)
                    {
                        AddPossibleRectangles(ref possibleStairRectangles, "vertical", stairLength, stairWidth, p, f);
                    }

                }

                //find all the rectangles that are also at edges to the floor below
                sharedStairEdgeRectangles = FindSharedStairPoints(possibleStairRectangles);

                //if there are any shared rectangles, choose a random one of them
                if (sharedStairEdgeRectangles.Count > 0)
                {
                    chosenRect = sharedStairEdgeRectangles[_random.Next(0, sharedStairEdgeRectangles.Count)];
                }
                //if not, choose a random rectangle from the list of possible rectangles
                else if (possibleStairRectangles.Count > 0)
                {
                    chosenRect = possibleStairRectangles[_random.Next(0, possibleStairRectangles.Count)];
                }
                else
                {
                    //as a last resort, just choose a position that ensures the stairs don't go outside the floor
                    chosenRect = GetLastResortRectangles(f, stairWidth, stairLength);
                }
                
                //add the rectangle to the floor and the floor below
                _allFloors[_currentFloorIndex].AddStairs(chosenRect);
                _allFloors[_currentFloorIndex - 1].AddStairs(chosenRect);

                //reset timer so the display shows the floor for long enough
                _previousUpdateTime = DateTime.Now;
            } 
            //create the entrance to the ground floor if it hasn't already been made
            else if (_currentFloorIndex == 0 && _allFloors[0].Entrance.Width == 0)
            {
                //change these values to alter the size of the entrance block
                int entranceLength = 5;
                int entranceWidth = 20;

                CreateEntrance(entranceLength, entranceWidth);
                return "> created school entrance";
            }

            //move onto the next game state if all floors have been updated
            if (_currentFloorIndex == -1)
            {
                _gameStateIndex++;
                _currentFloorIndex = 0;
                return "";
            } 
            //don't return a message if at ground floor and entrance has already been made
            else if (_currentFloorIndex == 0)
            {
                return "";
            }
            //if on any other floor, return a message about creating the stairs
            else
            {
                return $"> created stairs: floor {_currentFloorIndex}";
            }          
        }
        private Rectangle GetLastResortRectangles(Floor f, int stairWidth, int stairLength)
        {
            bool validPoint;

            //iterate through each point in the grid
            for (int x = 0; x <= f.GetGrid.GetUpperBound(0) - stairWidth; x++)
            {
                for (int y = 0; y <= f.GetGrid.GetUpperBound(1) - stairLength; y++)
                {
                    validPoint = true;

                    //iterate through the length and width of the stair to see if it fits
                    for (int i = 0; i < stairWidth; i++)
                    {
                        for (int j = 0; j < stairWidth; j++)
                        {
                            if (f.GetGrid[x + i, y + j] != 'X')
                            {
                                //point isn't valid if the stairs would go over an occupied / outside space
                                validPoint = false;
                            }
                        }
                    }

                    //returns the position if it can contain the stairs
                    if (validPoint)
                    {
                        return new Rectangle(x, y, stairWidth, stairLength);
                    }
                }
            }

            //returns nothing if no space at all (impossible)
            return new Rectangle(0, 0, 0, 0);
        }
        private void FindAllFloorEdgePoints()
        {
            //call each floor to store its edgepoints
            foreach (Floor f in _allFloors)
            {
                f.FindAllEdgePoints('X');
            }
        }
        private void CheckEdgeAlignments(Point p, int length, int width, Floor f, ref bool horizontalEdge, ref bool verticalEdge)
        {
            //check if the edge is horizontal and update horizontalEdge accordingly
            if (p.X + width <= f.GetGrid.GetUpperBound(0) && p.Y + length <= f.GetGrid.GetUpperBound(0))
            {
                for (int x = p.X; x < p.X + width; x++)
                {
                    if (!f.Edgepoints.Contains(new Point(x, p.Y))) 
                    {
                        horizontalEdge = false;
                    }
                }
            }
            else horizontalEdge = false;

            //check if the edge is vertical and update verticalEdge accordingly
            if (p.Y + width <= f.GetGrid.GetUpperBound(0) && p.X + length <= f.GetGrid.GetUpperBound(0))
            {
                for (int y = p.Y; y < p.Y + width; y++)
                {
                    if (!f.Edgepoints.Contains(new Point(p.X, y)))
                    {
                        verticalEdge = false;
                    }
                }
            }
            else verticalEdge = false;
        }
        private void AddPossibleRectangles(ref List<Rectangle> stairRects, string alignment, int stairLength, int stairWidth, Point p, Floor f)
        {
            //two possible rectngles for each type of edge:
            // - if horizontal: rectangle can either be above or below the edge
            // - if vertical: rectangle can either be to the left or right of the edge
            //validAlignment1 and 2 represent each of these two possibilities
            bool validAlignment1 = true, validAlignment2 = true;

            switch (alignment)
            {
                case "horizontal":           
                    for (int x = p.X; x < p.X + stairWidth; x++)
                    {
                        //check if a stair block would fit below the edge
                        for (int y = p.Y; y < p.Y + stairLength; y++)
                        {
                            if (y > f.GetGrid.GetUpperBound(1) || f.GetGrid[x, y] != 'X')
                            {
                                validAlignment1 = false;
                                break;
                            }
                        }

                        //check of a stair block would fit above the edge
                        for(int y = p.Y; y > p.Y - stairLength; y--)
                        {
                            if (y < 0 || f.GetGrid[x, y] != 'X')
                            {
                                validAlignment2 = false;
                                break;
                            }
                        }
                    }

                    //if any of these rectangles fit, add them to the list
                    if (validAlignment1)
                    {
                        stairRects.Add(new Rectangle(p.X, p.Y, stairWidth, stairLength));
                    }
                    if (validAlignment2)
                    {
                        stairRects.Add(new Rectangle(p.X, p.Y - stairLength + 1, stairWidth, stairLength));
                    }
                    break;
                case "vertical":                   
                    for (int y = p.Y; y < p.Y + stairWidth; y++)
                    {
                        //check if a stair block would fit to the right of the edge
                        for (int x = p.X; x < p.X + stairLength; x++)
                        {
                            if (x > f.GetGrid.GetUpperBound(0) || f.GetGrid[x, y] != 'X')
                            {
                                validAlignment1 = false;
                                break;
                            }
                        }

                        //check if a stair block would fit to the left of the edge
                        for (int x = p.X; x > p.X - stairLength; x--)
                        {
                            if (x < 0 || f.GetGrid[x, y] != 'X')
                            {
                                validAlignment2 = false;
                                break;
                            }
                        }
                    }

                    //add these rectangles to the list if they would fit
                    if (validAlignment1)
                    {
                        stairRects.Add(new Rectangle(p.X, p.Y, stairLength, stairWidth));
                    }
                    if (validAlignment2)
                    {
                        stairRects.Add(new Rectangle(p.X - stairLength + 1, p.Y, stairLength, stairWidth));
                    }
                    break;
            }
        }
        private List<Rectangle> FindSharedStairPoints(List<Rectangle> stairRects)
        {
            List<Rectangle> sharedRectangles = new List<Rectangle>();
            bool sharedPoint;
            //store the edgepoints of the floor below the current one
            List<Point> lowerFloorEdges = _allFloors[_currentFloorIndex - 1].Edgepoints;

            //iterate through all of the rectangles in the stairRects list
            foreach (Rectangle r in stairRects)
            {
                sharedPoint = false;
                
                //look through all the edge points of the rectangle and check if the same point is at an edge on the floor below
                for (int x = r.X; x < r.X + r.Width; x++)
                {
                    if (lowerFloorEdges.Contains(new Point(x, r.Y)))
                    {
                        sharedPoint = true;
                        break;
                    } else if (lowerFloorEdges.Contains(new Point(x, r.Y + r.Height - 1))) {
                        sharedPoint = true;
                        break;
                    }
                }
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if (lowerFloorEdges.Contains(new Point(r.X, y)))
                    {
                        sharedPoint = true;
                        break;
                    } else if (lowerFloorEdges.Contains(new Point(r.X + r.Width - 1, y)))
                    {
                        sharedPoint = true;
                        break;
                    }
                }

                //if the rectangle contains a shared edgepoint, add it to the list
                if (sharedPoint)
                {
                    sharedRectangles.Add(r);
                }
            }
            return sharedRectangles;
        }
        private void CreateEntrance(int entranceLength, int entranceWidth)
        {
            List<Rectangle> possibleEntrancePoints = new List<Rectangle>();
            bool horizontal, vertical;
            Rectangle chosenRect;

            //iterate through each edgepoint on the ground floor
            foreach (Point p in _allFloors[0].Edgepoints)
            {
                horizontal = true;
                vertical = true;

                //check if the point is part of a horizontal or vertical edge
                CheckEdgeAlignments(p, entranceLength, entranceWidth, _allFloors[0], ref horizontal, ref vertical);

                //add the corresponding rectangles based on the alignment of the point's edgs
                if (horizontal)
                {
                    AddPossibleRectangles(ref possibleEntrancePoints, "horizontal", entranceLength, entranceWidth, p, _allFloors[0]);
                }
                if (vertical)
                {
                    AddPossibleRectangles(ref possibleEntrancePoints, "vertical", entranceLength, entranceWidth, p, _allFloors[0]);
                }
            }

            //choose a random rectangle to be the entrance
            chosenRect = possibleEntrancePoints[_random.Next(0, possibleEntrancePoints.Count)];
            //add it to the ground floor
            _allFloors[0].AddEntrance(chosenRect);
        }

        // - update zone growth -
        private string UpdateZoneGrowth()
        {
            //if a floor's zone growth has been completed (or if this is the first call of the subroutine), move onto the next floor
            if (_currentFloorIndex == -1 || _currentZoneIndex >= _allFloors[_currentFloorIndex].Zones.Count || (_allFloors[_currentFloorIndex].FinishedFirstZoneGrowth && _allFloors[_currentFloorIndex].FinishedSecondZoneGrowth && _allFloors[_currentFloorIndex].FinishedThirdZoneGrowth))
            {
                _currentFloorIndex++;
                _currentZoneIndex = 0;
            }

            if (_currentFloorIndex < _allFloors.Count)
            {
                Floor currentFloor = _allFloors[_currentFloorIndex];

                //if the current floor hasn't finished the first growth stage
                if (!currentFloor.FinishedFirstZoneGrowth)
                {
                    if (currentFloor.Zones[_currentZoneIndex].FirstGrown)
                    {
                        //mark first stage as done if all zones in the floor have been grown
                        if (_currentZoneIndex == currentFloor.Zones.Count - 1)
                        {            
                            currentFloor.FinishedFirstZoneGrowth = true;
                        }
                        //move onto next zone if current zone has finished the first growth stage
                        else
                        {
                            _currentZoneIndex++;
                        }
                    }
                    //if the current zone hasn't been grown
                    else
                    {
                        //grow the zones (and store whether it has finished the first stage or not)
                        currentFloor.Zones[_currentZoneIndex].FirstGrown = GrowZone(currentFloor.Zones[_currentZoneIndex], currentFloor);
                        
                        //return a message about growing the zone
                        return $"> growing zone: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                    }
                } 

                //look at floor's second growth stage if not completed
                else if (!currentFloor.FinishedSecondZoneGrowth)
                {

                    //if the current zone has finished the second growth
                    if (currentFloor.Zones[_currentZoneIndex].SecondGrown)
                    {
                        //marks second stage as done if all zones have grown a second time
                        if (_currentZoneIndex == 0)
                        {
                            _currentZoneIndex = currentFloor.Zones.Count - 1;
                            currentFloor.FinishedSecondZoneGrowth = true;
                        }

                        //if not, move onto the next zone to grow
                        else
                        {
                            _currentZoneIndex--;
                        }
                    }
                    else
                    {
                        //grow the zone a second time if it hasn't finished this stage
                        currentFloor.Zones[_currentZoneIndex].SecondGrown = GrowZone(currentFloor.Zones[_currentZoneIndex], currentFloor);
                        
                        return $"> expanding zone: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                    }

                }
                //look at third stage if the floor hasn't completed it
                else if (!currentFloor.FinishedThirdZoneGrowth)
                {
                    
                    //if the current zone has already finished the third stage
                    if (currentFloor.Zones[_currentZoneIndex].ThirdGrown)
                    {
                        //mark the third stage as done if all zones in the floor have completed it
                        if (_currentZoneIndex == 0)
                        {
                            currentFloor.FinishedThirdZoneGrowth = true;
                        }
                        //if not, move onto the next zone
                        else
                        {
                            _currentZoneIndex--;
                        }
                    }
                    else
                    {
                        //finish the zone's growth if this hasn't already been done
                        currentFloor.Zones[_currentZoneIndex].ThirdGrown = FinishZoneGrowth(currentFloor.Zones[_currentZoneIndex], currentFloor);
                        
                        return $"> final zone growth: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                    }
                }
                return "";
            }
            else
            {
                //store zone data once each zone has been grown fully
                CopyFloorDataToZones();

                //move onto next game stage
                _gameStateIndex++;
                return "> finished zone growth";
            }           
        }
        private bool GrowZone(Zone z, Floor f)
        {
            //if the zone's growth point hasn't been chosen, choose it
            if (z.WeightedGrid is null)
            {
                SetZoneGrowthPoint(z, f);
                _previousUpdateTime = DateTime.Now;
            }

            //only updates after crtain (small) amount of time has passed between previous update 
            else if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange / 50))
            {
                bool left = false, right = false, up = false, down = false;

                //step = how many growths are done in one frame
                int step = 5;

                GrowGrid(ref left, ref right, ref up, ref down, z, f, step, CheckValidZoneGrowth);

                //delete the zone if the shape is too narrow
                if (((!left && !right && z.RectWidth < 75) || (!up && !down && z.RectHeight < 75)) && !z.FirstGrown)
                {
                    f.Zones.Remove(z);
                    _currentZoneIndex--;
                    f.RemoveFromGrid((char)('0' | z.ID));
                }


                //if no more growth available (or required area reached on first growth), finish
                else if ((!left && !right && !up && !down) || (!z.FirstGrown && z.RectWidth * z.RectHeight >= z.IdealSize))
                {
                    if (z.FirstGrown && (z.RectHeight >= z.RectWidth * 4.5 || z.RectWidth >= z.RectHeight * 4.5))
                    {
                        f.Zones.Remove(z);
                        //_currentZoneIndex--;
                        f.RemoveFromGrid((char)('0' | z.ID));
                    } else
                    {
                        SetZoneRect(z, f);
                        return true;
                    }
                }

                _previousUpdateTime = DateTime.Now;
            }
            return false;
        }
        private void GrowGrid(ref bool left, ref bool right, ref bool up, ref bool down, GrowableArea p, Grid g, int step, Func<int, int, int, int, char, char[,], char, bool> CheckValidGrowth, char c = ' ')
        {
            // bool left = false, right = false, up = false, down = false;
            for (int i = 0; i < step; i++)
            {

                //grow in each direction if possible
                //update the location of the zone's top-left corner and grid dimensions if grown in a certain direction

                //grow left if possible
                left = CheckValidGrowth(p.GrowthTopLeft.X, p.GrowthTopLeft.Y, -1, p.RectHeight, 'x', g.GetGrid, c);
                if (left)
                {
                    p.RectWidth++;
                    p.GrowthTopLeft = new Point(p.GrowthTopLeft.X - 1, p.GrowthTopLeft.Y);
                }

                //grow right if possible
                right = CheckValidGrowth(p.GrowthTopLeft.X + p.RectWidth - 1, p.GrowthTopLeft.Y, 1, p.RectHeight, 'x', g.GetGrid, c);
                if (right)
                {
                    p.RectWidth++;
                }

                //grow up if possible
                up = CheckValidGrowth(p.GrowthTopLeft.X, p.GrowthTopLeft.Y, -1, p.RectWidth, 'y', g.GetGrid, c);
                if (up)
                {
                    p.GrowthTopLeft = new Point(p.GrowthTopLeft.X, p.GrowthTopLeft.Y - 1);
                    p.RectHeight++;
                }

                //grow down if possible
                down = CheckValidGrowth(p.GrowthTopLeft.X, p.GrowthTopLeft.Y + p.RectHeight - 1, 1, p.RectWidth, 'y', g.GetGrid, c);
                if (down)
                {
                    p.RectHeight++;
                }
            }


            //update the zone's base rectangle based on the new top left position and grid dimensions
            p.UpdateBaseRect(p.GrowthTopLeft.X, p.GrowthTopLeft.Y, p.RectWidth, p.RectHeight);
        }
        private void SetZoneGrowthPoint(Zone z, Floor f)
        {
            //make the weighted grid
            z.WeightedGrid = MakeZoneWeightedGrid(z, f);

            //choose a point based on the grid
            z.GrowthPoint = ChooseGrowthPoint(z.WeightedGrid);

            //update zone variables appropriately
            f.GetGrid[z.GrowthPoint.X, z.GrowthPoint.Y] = (char)('0' | z.ID);
            z.GrowthTopLeft = z.GrowthPoint;
            z.RectWidth = 1;
            z.RectHeight = 1;
        }
        private int[,] MakeZoneWeightedGrid(Zone z, Floor f)
        {
            int[,] weightedGrid = new int[f.GetGrid.GetLength(0), f.GetGrid.GetLength(1)];
            int adjacencyRule, distance = (int)Math.Sqrt(z.IdealSize);

            //set all locations outside the floor to a very negative value to discourage growth points from forming there
            for (int x = 0; x < weightedGrid.GetUpperBound(0); x++)
            {
                for (int y = 0; y < weightedGrid.GetUpperBound(1); y++)
                {
                    if (f.GetGrid[x, y] == ' ' || Char.IsDigit(f.GetGrid[x, y]))
                    {
                        weightedGrid[x, y] = -100;
                    }
                    else
                    {
                        weightedGrid[x, y] = 0;
                    }
                }
            }

            //update weights around floor's edge points
            foreach (Point p in f.Edgepoints)
            {
                //add a positive weight to the points a certain distance away from each edge
                //this increases the likelihood of a zone growing to be adjacent to an edge while preventing it from becoming a bad shape
                UpdateFarPoints(ref weightedGrid, p, distance, 10);
            }

            //update weights around other zones
            foreach (Zone otherZone in f.Zones)
            {
                //only take a zone into account if it has grown already
                if (!Zone.ReferenceEquals(otherZone, z) && otherZone.FirstGrown)
                {
                    //get adjacency rule between the two zones (whether they are encouraged / discouraged to be adjacent)
                    adjacencyRule = ZoneType.GetAdjacencyRule(otherZone.ZoneType, z.ZoneType);

                    //if the zones shouldn't be adjacent, 
                    if (adjacencyRule < 0)
                    {
                        foreach (Point p in otherZone.Edgepoints)
                        {
                            //negative weights will be given around the zone rectangle
                            UpdateClosePoints(ref weightedGrid, p, (int)(distance / 2), adjacencyRule);

                            //negative weights will also be given to points a certain distance away around the rectangle
                            //(leads to more interesting zone shapes)
                            //UpdateFarPoints(ref weightedGrid, p, distance, adjacencyRule);
                        }
                    }
                    //if the zones should be adjacent
                    else
                    {
                        //positive weights will be added a certain distance away from the zone's rectangle
                        //this reduces the chance of strange shapes growing while increasing the likelihood that both zones are adjacent
                        foreach (Point p in otherZone.Edgepoints)
                        {
                            UpdateFarPoints(ref weightedGrid, p, distance, adjacencyRule);
                        }
                    }
                }
            }

            //return the weighted grid created
            return weightedGrid;
        }
        private void UpdateClosePoints(ref int[,] grid, Point p, int distance, int addWeight)
        {
            //give weights to points within a certain distance away from the point
            for (int x = p.X - (int)(distance / 2); x < p.X + distance / 2; x++)
            {
                for (int y = p.Y - (int)(distance / 2); y < p.Y + distance / 2; y++)
                {
                    if (WithinBounds(x, y, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[x, y] == 0)
                    {
                        grid[x, y] += addWeight;
                    }
                }
            }
        }
        private void UpdateFarPoints(ref int[,] grid, Point p, int distance, int addWeight)
        {
            int d = (int)(distance / 2);

            //add weights to points that are a specific distance away from the given point
            //doesn't include points within that distance away from the point

            for (int x = p.X - d; x < p.X + d; x++)
            {
                if (WithinBounds(x, p.Y - d, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[x, p.Y - d] >= 0 && grid[x, p.Y - 1] >= 0)
                {
                    grid[x, p.Y - d] += addWeight;
                }
                if (WithinBounds(x, p.Y + d, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[x, p.Y + d] >= 0 && grid[x, p.Y + 1] >= 0)
                {
                    grid[x, p.Y + d] += addWeight;
                }
            }
            for (int y = p.Y - d; y < p.Y + d; y++)
            {
                if (WithinBounds(p.X - d, y, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[p.X - d, y] >= 0 && grid[p.X - 1, y] >= 0)
                {
                    grid[p.X - d, y] += addWeight;
                }
                if (WithinBounds(p.X + d, y, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[p.X + d, y] >= 0 && grid[p.X + 1, y] >= 0)
                {
                    grid[p.X + d, y] = addWeight;
                }
            }
        }
        private bool FinishZoneGrowth(Zone z, Floor f)
        {
            //adds "unclaimed" points adjacent to a zone to its rectangle
            //doesn't grow in a strict rectangular shape

            if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange / 50))
            {
                //step = how many growth iterations done in one frame
                int step = 5;

                bool left = false, right = false, up = false, down = false;
                List<Point> pointsToAdd = new List<Point>(), tempPointsToAdd1 = new List<Point>(), tempPointsToAdd2 = new List<Point>();

                for (int j = 0; j < step; j++)
                {
                    left = false;
                    right = false;
                    up = false;
                    down = false;
                    pointsToAdd.Clear();

                    //the tempPointsToAdd lists are for each direction in a pair: left and right, or up and down
                    tempPointsToAdd1.Clear();
                    tempPointsToAdd2.Clear();

                    //check if points to left / right aren't in any zones yet + add points if so
                    for (int i = z.GrowthTopLeft.Y; i < z.GrowthTopLeft.Y + z.RectHeight; i++)
                    {
                        if (WithinBounds(z.GrowthTopLeft.X - 1, i, f.GetGrid.GetUpperBound(0), f.GetGrid.GetUpperBound(1)) && f.GetGrid[z.GrowthTopLeft.X - 1, i] == 'X')
                        {
                            tempPointsToAdd1.Add(new Point(z.GrowthTopLeft.X - 1, i));
                        }
                        if (WithinBounds(z.GrowthTopLeft.X + z.RectWidth, i, f.GetGrid.GetUpperBound(0), f.GetGrid.GetUpperBound(1)) && f.GetGrid[z.GrowthTopLeft.X + z.RectWidth, i] == 'X')
                        {
                            tempPointsToAdd2.Add(new Point(z.GrowthTopLeft.X + z.RectWidth, i));
                        }
                    }

                    //only add points left/right if the amount is large enough relative to the height (not width!) of the zone's rectangle
                    //if adding points, update zone's top left location + dimensions if necessary
                    if (tempPointsToAdd1.Count > z.RectHeight / 3)
                    {
                        left = true;
                        z.RectWidth++;
                        z.GrowthTopLeft = new Point(z.GrowthTopLeft.X - 1, z.GrowthTopLeft.Y);
                        pointsToAdd.AddRange(tempPointsToAdd1);
                    }
                    if (tempPointsToAdd2.Count > z.RectHeight / 3)
                    {
                        right = true;
                        z.RectWidth++;
                        pointsToAdd.AddRange(tempPointsToAdd2);
                    }

                    tempPointsToAdd1.Clear();
                    tempPointsToAdd2.Clear();

                    //check if points to up / down aren't in any zones yet + add points if so
                    for (int i = z.GrowthTopLeft.X; i < z.GrowthTopLeft.X + z.RectWidth; i++)
                    {
                        if (WithinBounds(i, z.GrowthTopLeft.Y - 1, f.GetGrid.GetUpperBound(0), f.GetGrid.GetUpperBound(1)) && f.GetGrid[i, z.GrowthTopLeft.Y - 1] == 'X')
                        {
                            tempPointsToAdd1.Add(new Point(i, z.GrowthTopLeft.Y - 1));
                        }
                        if (WithinBounds(i, z.GrowthTopLeft.Y + z.RectHeight, f.GetGrid.GetUpperBound(0), f.GetGrid.GetUpperBound(1)) && f.GetGrid[i, z.GrowthTopLeft.Y + z.RectHeight] == 'X')
                        {
                            tempPointsToAdd2.Add(new Point(i, z.GrowthTopLeft.Y + z.RectHeight));
                        }
                    }

                    //similarly, only add points up / down if there are enough relative to the zone's width (not height!)
                    if (tempPointsToAdd1.Count > z.RectWidth / 3)
                    {
                        up = true;
                        z.GrowthTopLeft = new Point(z.GrowthTopLeft.X, z.GrowthTopLeft.Y - 1);
                        z.RectHeight++;
                        pointsToAdd.AddRange(tempPointsToAdd1);
                    }
                    if (tempPointsToAdd2.Count > z.RectWidth / 3)
                    {
                        down = true;
                        z.RectHeight++;
                        pointsToAdd.AddRange(tempPointsToAdd2);
                    }

                    //add each chosen point to the zone
                    foreach (Point p in pointsToAdd)
                    {
                        f.GetGrid[p.X, p.Y] = (char)('0' | z.ID);
                        z.FloorRectangles.Add(new Rectangle(p.X, p.Y, 1, 1));
                    }
                }

                //update zone grid with new locations and dimensions
                z.ResetGrid(z.RectWidth, z.RectHeight, 'Z');

                //return true if no more growth possible
                _previousUpdateTime = DateTime.Now;
                return (!left && !right && !up && !down);
            }
            return false;
        }
        private bool WithinBounds(int x, int y, int xUpperBound, int yUpperBound)
        {
            return x >= 0 && x <= xUpperBound && y >= 0 && y <= yUpperBound;
        }
        private Point ChooseGrowthPoint(int[,] weightedGrid)
        {
            Dictionary<int, List<Point>> pointsByWeight = new Dictionary<int, List<Point>>();
            int highestNum = -10000000;

            //iterate through each point in the weighted grid, adding them to a dictionary with their weight as a key
            //also updating the highest weight as it goes
            for (int x = 0; x <= weightedGrid.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= weightedGrid.GetUpperBound(1); y++)
                {
                    if (pointsByWeight.ContainsKey(weightedGrid[x, y]))
                    {
                        pointsByWeight[weightedGrid[x, y]].Add(new Point(x, y));
                    } else
                    {
                        highestNum = Math.Max(weightedGrid[x, y], highestNum);
                        pointsByWeight.Add(weightedGrid[x, y], new List<Point> { new Point(x, y) });
                    }
                }
            }

            //chooses a random point which has the highest weight
            return pointsByWeight[highestNum][_random.Next(0, pointsByWeight[highestNum].Count)];
        }
        private bool CheckValidZoneGrowth(int x, int y, int step, int length, char direction, char[,] grid, char c = ' ')
        {
            //iterate along a zone's edge to check if growth in the specified direction + step is valid
            //returns false if any of the points in the growth direction are unavailable
            //Char.IsDigit(grid[x + step, i]) checks if the point is already claimed by another zone (as the point would hold its digit)

            //STEP OVER THIS
            return CheckValidGrowth(x, y, step, length, direction, grid, (grid, x, y) => grid[x, y] == ' ' || Char.IsDigit(grid[x, y]));
        }
        private bool CheckValidGrowth(int x, int y, int step, int length, char direction, char[,] grid, Func<char[,], int, int, bool> endCriteria)
        {
            //iterate along a zone's edge to check if growth in the specified direction + step is valid
            //returns false if any of the points in the growth direction are unavailable

            if (direction == 'x')
            {
                for (int i = y; i < y + length; i++)
                {
                    if (!WithinBounds(x + step, i, grid.GetUpperBound(0), grid.GetUpperBound(1)) || endCriteria(grid, x + step, i))
                    {
                        return false;
                    }
                }

            }
            else if (direction == 'y')
            {
                for (int i = x; i < x + length; i++)
                {
                    if (!WithinBounds(i, y + step, grid.GetUpperBound(0), grid.GetUpperBound(1)) || endCriteria(grid, i, y + step))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private void SetZoneRect(Zone z, Floor f)
        {
            //updates the zone's grid with new dimensions
            z.ResetGrid(z.RectWidth, z.RectHeight, 'Z');

            //adds the zone's rectangle to the floor grid
            f.AddRectToGrid(new Rectangle(z.GrowthTopLeft, new Point(z.RectWidth, z.RectHeight)), (char)('0' | z.ID), false);
            
            //set all edgepoints of the zone
            z.FindAllEdgePoints('Z');
        }
        private void CopyFloorDataToZones()
        {
            //for each zone in all floors, copy the grid data from the floor to the zone
            foreach (Floor f in _allFloors)
            {
                foreach (Zone z in f.Zones)
                {            
                    for (int x = z.GrowthTopLeft.X; x < z.GrowthTopLeft.X + z.RectWidth; x++)
                    {
                        for (int y = z.GrowthTopLeft.Y; y < z.GrowthTopLeft.Y + z.RectHeight; y++)
                        {
                            z.GetGrid[x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y] = f.GetGrid[x, y];
                        }
                    }

                    //update all edgepoints of the zone
                    z.FindAllEdgePoints((char)('0' | z.ID));
                    z.UpdateArea();
                }
            }
        }


        // - update corridor generation -
        private string UpdateCorridorCreation()
        {
            //update floor to be displayed + altered if enough time has passed
            if (_currentFloorIndex == _allFloors.Count || DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange))
            {
                _currentFloorIndex--;
            }

            if (_currentFloorIndex > -1)
            {
                //if corridors haven't already been made for the current floor
                if (_allFloors[_currentFloorIndex].CorridorStartingPoints.Count == 0)
                {
                    Floor f = _allFloors[_currentFloorIndex];

                    //collect each zone growth point to base the corridors on
                    foreach (Zone z in f.Zones)
                    {
                        f.CorridorStartingPoints.Add(new Rectangle(z.GrowthPoint, new Point(1, 1)));
                    }

                    //create the corridors connecting stairs / entrance / points
                    CreateCorridors(f);

                    //update display time
                    _previousUpdateTime = DateTime.Now;
                    return $"> created corridors: floor {_currentFloorIndex}";
                }
                return "";
            }
            //if corridors have been made for all floors, move onto next game state
            else
            {
                _gameStateIndex++;
                return "";
            }
        }
        private void CreateCorridors(Floor f)
        {
            List<Point> allStartingPoints = new List<Point>(), corridorPoints = new List<Point>();
            Point stairPoint1, closestPoint;
            (Point, Point) tempPoints;
            bool alreadyConnected;
            int connectionIndex = -1;
            List<List<Point>> pointConnections = new List<List<Point>>();

            //adding stairs, starting points, and entrance (if applicable) to the list of points to connect
            foreach (Rectangle r in f.StairPoints)
            {
                allStartingPoints.Add(new Point(r.X, r.Y));
            }
            foreach (Rectangle r in f.CorridorStartingPoints)
            {
                allStartingPoints.Add(new Point(r.X, r.Y));
            }
            if (f.FloorID == 0)
            {
                allStartingPoints.Add(new Point(f.Entrance.X, f.Entrance.Y));
            }

            //store the position of stairs as a point
            stairPoint1 = new Point(f.StairPoints[0].X, f.StairPoints[0].Y);

            //iterate through each starting point
            foreach (Point p in allStartingPoints)
            {
                connectionIndex = -1;
                alreadyConnected = false;

                //find the closest other point to this point
                closestPoint = FindClosestStairPoint(p, allStartingPoints);

                //check all previously made connections
                for (int i = 0; i < pointConnections.Count; i++)
                {
                    //check if this specific connection has already been made
                    if (pointConnections[i].Contains(p) && pointConnections[i].Contains(closestPoint))
                    {
                        alreadyConnected = true;
                    }
                    //if not, check if either of the points are already in a connection list
                    else if (pointConnections[i].Contains(p) || pointConnections[i].Contains(closestPoint))
                    {
                        connectionIndex = i;
                    }
                }

                //only add a path if the points haven't already been connected to each other
                if (!alreadyConnected)
                {
                    //collect points on the shortest path between two points
                    corridorPoints.AddRange(FindShortestPath(p, closestPoint, (char[,])f.GetGrid.Clone()));

                    //if either of the points is already in one of the connection lists..
                    if (connectionIndex != -1)
                    {
                        //add both to that list and remove duplicates
                        pointConnections[connectionIndex].Add(closestPoint);
                        pointConnections[connectionIndex].Add(p);
                        pointConnections[connectionIndex] = pointConnections[connectionIndex].Distinct().ToList();
                    }
                    //if both points aren't in any connection lists, make a new one with both of them
                    else
                    {
                        pointConnections.Add(new List<Point> { p, closestPoint });
                    }
                }
            }

            //ensuring that each point is accessible from each other point (directly or indirectly)
            //check each list of connections
            foreach (List<Point> connections in pointConnections)
            {
                //if the points aren't connected to (one of) the stairs
                if (!connections.Contains(stairPoint1))
                {
                    //find the closest pair of points between this list and the first list
                    //the first list is guaranteed to contain the stairs because stairs are added first to the list of points to connect
                    tempPoints = FindClosestStairPoint(pointConnections[0], connections);

                    //add the shortest path between these two points to connect the lists
                    corridorPoints.AddRange(FindShortestPath(tempPoints.Item1, tempPoints.Item2, (char[,])f.GetGrid.Clone()));
                }
            }

            //call the floor to make a corridor based on all the paths created
            f.AddCorridor(corridorPoints);
        }
        private List<Point> FindShortestPath(Point start, Point end, char[,] grid)
        {
            Queue<Point> nextPoints = new Queue<Point>();
            Point next = start;
            Dictionary<Point, Point> previousPoints = new Dictionary<Point, Point>();
            List<Point> corridorList = new List<Point>();

            //using a BFS algorithm to find the shortest path between two points on a graph

            nextPoints.Enqueue(start);

            while (nextPoints.Count > 0 && next != end)
            {
                next = nextPoints.Dequeue();
                //mark current point as visited
                grid[next.X, next.Y] = 'V';

                //find neighbours + add to queue
                if (next.X > 0 && ValidNextPoint(new Point(next.X - 1, next.Y), grid) && !nextPoints.Contains(new Point(next.X - 1, next.Y)))
                {
                    nextPoints.Enqueue(new Point(next.X - 1, next.Y));
                    previousPoints.Add(new Point(next.X - 1, next.Y), next);
                }
                if (next.X < grid.GetUpperBound(0) && ValidNextPoint(new Point(next.X + 1, next.Y), grid) && !nextPoints.Contains(new Point(next.X + 1, next.Y)))
                {
                    nextPoints.Enqueue(new Point(next.X + 1, next.Y));
                    previousPoints.Add(new Point(next.X + 1, next.Y), next);
                }
                if (next.Y > 0 && ValidNextPoint(new Point(next.X, next.Y - 1), grid) && !nextPoints.Contains(new Point(next.X, next.Y - 1)))
                {
                    nextPoints.Enqueue(new Point(next.X, next.Y - 1));
                    previousPoints.Add(new Point(next.X, next.Y - 1), next);
                }
                if (next.Y < grid.GetUpperBound(1) && ValidNextPoint(new Point(next.X, next.Y + 1), grid) && !nextPoints.Contains(new Point(next.X, next.Y + 1)))
                {
                    nextPoints.Enqueue(new Point(next.X, next.Y + 1));
                    previousPoints.Add(new Point(next.X, next.Y + 1), next);
                }
            }

            //trace back shortest path from end to start
            next = end;
            do
            {
                corridorList.Add(next);
                next = previousPoints[next];
            } while (previousPoints[next] != start);
            corridorList.Add(start);

            return corridorList;
        }
        private bool ValidNextPoint(Point p, char[,] grid)
        {
            //used in the BFS subroutine to check if a point is within the bounds of the building and has not been visited yet
            char c = grid[p.X, p.Y];

            // return (c == 'X' || c == 'S' || c == 'E' || c == 'C');

            //C can't be V or ' '
            return (c != 'V' && c != ' ');
        }
        private Point FindClosestStairPoint(Point start, List<Point> allDestinations)
        {
            Point closestPoint = allDestinations[0];

            //iterate through each point in the list to find the shortest distance, and the point corresponding to that
            foreach (Point p in allDestinations)
            {
                if (p != start && (Math.Pow(p.X - start.X, 2) + Math.Pow(p.Y - start.Y, 2) < Math.Pow(closestPoint.X - start.X, 2) + Math.Pow(closestPoint.Y - start.Y, 2) || start == closestPoint))
                {
                    closestPoint = p;
                }
            }

            //return this point as the closest point to the start
            return closestPoint;
        }
        private (Point, Point) FindClosestStairPoint(List<Point> startPoints, List<Point> destinationPoints)
        {
            Point start = startPoints[0], end = destinationPoints[0];
            int currentDistance = 1000000000;

            //iterate through each point combination from both lists
            foreach (Point p1 in startPoints)
            {
                foreach (Point p2 in destinationPoints)
                {
                    //check if the distance between this pair is less than the last recorded distance
                    if (p1 != p2 && Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) < currentDistance)
                    {
                        //if so, set the distance, start, and end points to this pair
                        currentDistance = (int)(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
                        start = p1;
                        end = p2;
                    }
                }
            }

            //return the pair of points as the shortest pair between the two lists
            return (start, end);
        }

        // - update floor generation - TODO

        //make zones bigger? so they don't end up too small (done?)
        //make rooms first...but grow with space between! <--- how?!
        //assign entrances to each room
        //make corridors go between entrances and the corridor entrance point given by the ones generated before
        //erase corridors + only add connections for new corridors to fit?

        //make rooms grow same way as zones.... 
        // TODO: fix weird issues here...

        private string UpdateRoomGrowth()
        {
            if (_currentFloorIndex == -1)
            {
                SetUpRooms();
                //initially...set zone and currentfloor
                _currentFloorIndex = 0;
                _currentZoneIndex = 0;
                _currentRoomIndex = 0;

                //also construct the rooms!
            }
            else if (_currentFloorIndex < _allFloors.Count)
            {
                //for each zone
                Floor currentFloor = _allFloors[_currentFloorIndex];

                if (_currentZoneIndex == currentFloor.Zones.Count)
                {
                    _currentFloorIndex++;
                    _currentZoneIndex = 0;
                } 
                else
                {
                    Zone currentZone = currentFloor.Zones[_currentZoneIndex];

                    if (_currentRoomIndex == currentZone.Rooms.Count)
                    {
                        _currentZoneIndex++;
                        _currentRoomIndex = 0;
                    }
                    else
                    {
                        //do some actual growing... <--- TODO

                        if (GrowRoom(currentZone.Rooms[_currentRoomIndex], currentZone))
                        {
                            currentZone.Rooms[_currentRoomIndex].Grown = true;
                            _currentRoomIndex++;

                            //TODO: add floor to grid and edgepoints and kfjgearfj ojgme
                            //how to add???

                        }

                       
                    }

                }
                
            }
            return $"> growing rooms: floor {_currentFloorIndex}, zone {_currentZoneIndex}, room {_currentRoomIndex}";
        }

        private void SetUpRooms()
        {
            foreach (Floor f in _allFloors)
            {
                foreach (Zone z in f.Zones)
                {
                    //z.Rooms = new List<Room>();
                    for (int i = 0; i < z.NumberOfRooms; i++)
                    {
                        z.Rooms.Add(new Room(i, z.ZoneType.Type));
                    }
                }
            }
        }

        private bool GrowRoom(Room r, Zone z) 
        {
            if (r.WeightedGrid is null)
            {
                //MakeRoomWeightedGrid(r, z);
                //make the growth point!
                SetRoomGrowthPoint(r, z);
                _previousUpdateTime = DateTime.Now;
            } else if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange))
            {
                bool left = false, right = false, up = false, down = false;
                int step = 1;

                GrowGrid(ref left, ref right, ref up, ref down, r, z, step, CheckValidRoomGrowth, (char)('0' | z.ID));

                r.UpdateBaseRect(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectWidth, r.RectHeight);
                //TODO: when to stop growing?? <----
                //also, how to display?

                //stop growing when...
                //can't grow anymore?

                if ((!left && !right && r.RectWidth < 50) || (!up && !down && r.RectHeight < 50))
                {
                    z.Rooms.Remove(r);
                    z.BadGrowthPoints.Add(r.GrowthPoint);
                    //TODO: force room to be made if none created
                    //_currentRoomIndex--;
                    //return true;
                   // r.RemoveFromGrid((char)('0' | z.ID));
                }


                //if no more growth available (or required area reached on first growth), finish
                else if ((!left && !right && !up && !down) || (r.RectWidth * r.RectHeight >= (z.Area / z.Rooms.Count)))
                {
                    if (r.RectHeight >= r.RectWidth * 4.5 || r.RectWidth >= r.RectHeight * 4.5)
                    {
                        z.Rooms.Remove(r);
                        z.BadGrowthPoints.Add(new Point(r.GrowthPoint.X, r.GrowthPoint.Y));
                       
                    }
                    else
                    {
                        SetRoomRect(r);
                        //TODO: set zone rect...
                        //what if room ID is same as zone ID?
                        //also weird zone "shadows" appear....investigate

                        char tempID;
                        if (z.ID == r.ID)
                        {
                            //hmmmmmm
                            //will only happen once per zone...so make it R? or U for unassigned
                            tempID = 'U';
                        }
                        else
                        {
                            tempID = (char)('0' | r.ID);
                        }

                        z.AddRectToGrid(new Rectangle(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight), tempID, false, (char)('0' | z.ID), false);
                        return true;
                    }
                   
                }
            }
            return false;
        
        }
        private void SetRoomGrowthPoint(Room r, Zone z)
        {
            r.WeightedGrid = MakeRoomWeightedGrid(r, z);
            r.GrowthPoint = ChooseGrowthPoint(r.WeightedGrid);

            if (r.WeightedGrid[r.GrowthPoint.X, r.GrowthPoint.Y] < 0)
            {
                z.Rooms.Remove(r);
                _currentRoomIndex--;
            } else
            {
                r.RectWidth = 1;
                r.RectHeight = 1;
                r.FloorRectangles.Add(new Rectangle(0, 0, 0, 0));
                r.GrowthTopLeft = r.GrowthPoint;
                r.GrowthFloorPoint = new Point(r.GrowthPoint.X + z.GrowthTopLeft.X, r.GrowthPoint.Y + z.GrowthTopLeft.Y);
            }

        }
        private int[,] MakeRoomWeightedGrid(Room r, Zone z) 
        {
            
            int estArea = (int)(z.Area / z.Rooms.Count);
            //make grid dimensions
            int[,] weightedGrid = new int[z.RectWidth, z.RectHeight];
            //prevent points from outside

            for (int x = 0; x < z.RectWidth; x++)
            {
                for (int y = 0; y < z.RectHeight; y++)
                {
                    if (z.GetGrid[x, y] == (char)('0' | z.ID))
                    {
                        weightedGrid[x, y] = 0;
                    }
                    else
                    {
                        weightedGrid[x, y] = -1000;
                    }
                }
            }

            //a certain distance from other rooms

            foreach (Point p in z.BadGrowthPoints)
            {
                UpdateClosePoints(ref weightedGrid, p, (int)(Math.Sqrt(estArea)), -10);
            }

            foreach (Room room in z.Rooms)
            {
                if (!Room.ReferenceEquals(r, room) && room.Grown)
                {
                    //not too close to the edgepoints...but adjacency is nice actually
                    foreach (Point p in room.Edgepoints)
                    {
                        if (weightedGrid[p.X, p.Y] > 0)
                        {
                            UpdateFarPoints(ref weightedGrid, p, (int)(Math.Sqrt(estArea)), 5);
                        }
                      
                    }
                }
            }

            //a distance away from the edge...
            //equally divide distance
            //remake corridors????
            return weightedGrid;
        }
        private bool CheckValidRoomGrowth(int x, int y, int step, int length, char direction, char[,] grid, char zoneID)
        {
            //STEP OVER THIS
            return CheckValidGrowth(x, y, step, length, direction, grid, (grid, x, y) => !(grid[x, y] == zoneID));
        }
        private void SetRoomRect(Room r)
        {
            //updates the zone's grid with new dimensions
            r.ResetGrid(r.RectWidth, r.RectHeight, 'R');

            //adds the zone's rectangle to the floor grid
            //f.AddRectToGrid(new Rectangle(z.GrowthTopLeft, new Point(z.RectWidth, z.RectHeight)), (char)('0' | z.ID), false);

            //r.FloorRectangles.Add(new Rectangle(r.GrowthTopLeft, new Point(r.RectWidth, r.RectHeight)));
           // r.UpdateBaseRect(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight);
            //set all edgepoints of the zone
            r.FindAllEdgePoints('R');

            //how to update zone...?
            //set to room ID?
            
        }

        // - update message queue -
        private void UpdateMessageQueue(string s)
        {
            if (s != "")
            {
                //remove oldest message if the queue has grown too large
                if (_displayMessages.Count >= 5)
                {
                    _displayMessages.Dequeue();
                }

                //if the message is new, add it to the queue
                if (!_displayMessages.Contains(s))
                {
                    _displayMessages.Enqueue(s);
                }
            }
        }

        // - update scroll -
        private void UpdateScroll()
        {
            //allows user to scroll based on WASD keys pressed
            if (_currentKeyboardState.IsKeyDown(Keys.W))
            {
                _scrollY -= 10;
            }
            else if (_currentKeyboardState.IsKeyDown(Keys.A))
            {
                _scrollX -= 10;
            }
            else if (_currentKeyboardState.IsKeyDown(Keys.S))
            {
                _scrollY += 10;
            }
            else if (_currentKeyboardState.IsKeyDown(Keys.D))
            {
                _scrollX += 10;
            }
        }

        // - reset data -
        private void ResetValues()
        {
            //clear data if user returns to settings screen
            _allFloors = null;
            _displayMessages = new Queue<string>();
            _currentFloorIndex = 0;
            _currentZoneIndex = 0;
        }

        // - display -
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here

            //call appropriate draw subroutine depending on game state
            _spriteBatch.Begin();

            if (_gameState == "menu")
            {
                _titleScreen.DrawTitleScreen(_spriteBatch, GraphicsDevice);
            }
            else if (_gameState == "settings")
            {
                _settingsScreen.DrawSettingsScreen(_spriteBatch, GraphicsDevice);
            }
            else if (_gameState == "create floors" || _gameState == "create stairs" || _gameState == "create corridors" || _gameState == "grow rectangular zones" || _gameState == "create rooms")
            {
                DrawFloorUpdates();
            }

            //draw message queue over current display
            DrawMessageQueue();

            _spriteBatch.End();
            base.Draw(gameTime);
        }
        private void DrawFloorUpdates()
        {
            //draw the current floor that is being updated
            if (_currentFloorIndex > -1 && _currentFloorIndex < _allFloors.Count)
            {
                _allFloors[_currentFloorIndex].DrawFloor(_spriteBatch, _scrollX, _scrollY);
                
            }
        }
        private void DrawMessageQueue()
        {
            //draws message at bottom right corner of screen, most recent closest to the top
            for (int i = 0; i < _displayMessages.Count; i++)
            {
                _spriteBatch.DrawString(_consolas, _displayMessages.ToList()[i], new Vector2(_screenWidth - 350, _screenHeight - 100 - (20 * (i + 1))), Color.White);
            }
        }
    }
}