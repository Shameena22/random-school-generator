using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace random_school_generator
{
    //20/11/23, making gym mats
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
        private int _scrollX, _scrollY, _growthSpeed;
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
            _gameStates = new List<string> { "menu", "settings", "create floors", "create graphs", "create stairs", "grow rectangular zones", "create corridors", "create rooms", "create furniture" };
            _gameStateIndex = 0;
            _currentZoneIndex = 0;
            _titleScreen = new TitleScreen();
            _settingsScreen = new SettingsScreen(_allSubjectOptions);
            _currentFloorIndex = 0;
            _previousUpdateTime = new DateTime();
            _displayMessages = new Queue<string>();
            _screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _random = new Random();
            _timeBetweenDisplayChange = 500;
            _scrollX = -_screenWidth / 4;
            _scrollY = -_screenHeight / 8;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _titleScreen.CreateTitleScreen();
            _settingsScreen.CreateSettingsScreen();
            Floor.SetComponentColours();
            Room.SetComponentColours();
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
            RoomType.LoadData();
        }

        protected override void Update(GameTime gameTime)
        {
            string currentMessage = "";
            _currentKeyboardState = Keyboard.GetState();

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
                case "create furniture":
                    UpdateScroll();
                    currentMessage = UpdateFurnitureCreation();
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
                SetGrowthSpeed();
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
        private void SetGrowthSpeed()
        {
            //sets drawing speed of zone / room growth depending on floor size

            if (_floorSize < 150000)
            {
                _growthSpeed = 7;
            } else
            {
                _growthSpeed = 10;
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
                return $"> created floor {_currentFloorIndex}";
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
            zoneGraphChances.Add("toilets", 0.7f);
            //and the staffroom won't be too large / too small
            zoneGraphChances["staffroom"] = 0.5f;
            zoneGraphChances["offices"] = 0.65f;
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
                temp = _random.Next(0, Math.Min(50, (int)(total * 100))) / 100f;
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
                if (sizeProportions[i] > 0.1 || ((chosenZones[i] == "toilets" || chosenZones[i] == "office") && sizeProportions[i] > 0))
                {
                    zoneSizeProportions.Add(chosenZones[i], sizeProportions[i]);
                }
               
            }

            //if (zoneSizeProportions.Count == 1)
            //{

            //}

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
                //preventing too many toilets from being generated in a floor by restricting their number from 1 to 3
                return _random.Next(1, 3);
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
            if (_currentFloorIndex == -1 || (_allFloors[_currentFloorIndex].FinishedFirstZoneGrowth && _allFloors[_currentFloorIndex].FinishedSecondZoneGrowth && _allFloors[_currentFloorIndex].FinishedThirdZoneGrowth))
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
                    //and if the zone index is bigger than the amount of rooms present...only happens when zones has been deleted
                    if (_currentZoneIndex == currentFloor.Zones.Count)
                    {
                        currentFloor.FinishedFirstZoneGrowth = true;
                        _currentZoneIndex -= 1;
                    }

                    else if (currentFloor.Zones[_currentZoneIndex].FirstGrown)
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
                    if (_currentZoneIndex < currentFloor.Zones.Count )
                    {
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
                        return "";
                    } else
                    {
                        _currentZoneIndex = currentFloor.Zones.Count - 1;
                        currentFloor.FinishedSecondZoneGrowth = true;
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

                //move onto next game state
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

            //only updates after certain (small) amount of time has passed between previous update 
            else if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange / 50))
            {
                bool left = false, right = false, up = false, down = false;

                //grow the grid in available directions
                GrowGrid(ref left, ref right, ref up, ref down, z, f, _growthSpeed, CheckValidZoneGrowth);

                //update the zone's base rectangle based on the new top left position and grid dimensions
                z.UpdateBaseRect(z.GrowthTopLeft.X, z.GrowthTopLeft.Y, z.RectWidth, z.RectHeight);

                //delete the zone if the shape is too narrow
                if (((!left && !right && z.RectWidth < RoomType.SideLengths[z.ZoneType.SecondaryType]) || (!up && !down && z.RectHeight < RoomType.SideLengths[z.ZoneType.SecondaryType])) && !z.FirstGrown)
                {
                    f.Zones.Remove(z);
                   // _currentZoneIndex--;
                    f.RemoveFromGrid((char)('0' | z.ID));
                }

                //if no more growth available (or required area reached on first growth), finish
                else if ((!left && !right && !up && !down) || (!z.FirstGrown && z.RectWidth * z.RectHeight >= z.IdealSize))
                {
                    if (z.FirstGrown && (z.RectHeight >= z.RectWidth * 4.5 || z.RectWidth >= z.RectHeight * 4.5))
                    {
                        f.Zones.Remove(z);
                        ///_currentZoneIndex--;
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
            for (int i = 0; i < step; i++)
            {

                //grow in each direction if possible
                //update the location of the zone / room's top-left corner and grid dimensions if grown in a certain direction

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

                bool left = false, right = false, up = false, down = false;
                List<Point> pointsToAdd = new List<Point>(), tempPointsToAdd1 = new List<Point>(), tempPointsToAdd2 = new List<Point>();

                for (int j = 0; j < _growthSpeed; j++)
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
            //iterate along a zone / room's edge to check if growth in the specified direction + step is valid
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
                            
                            //large rooms will ignore corridors as they get in the way of growth
                            if (z.ZoneType.SecondaryType == "large" && f.GetGrid[x, y] == 'C')
                            {
                                z.GetGrid[x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y] = (char)('0' | z.ID);
                            }
                            else
                            {
                                z.GetGrid[x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y] = f.GetGrid[x, y];
                            }
                          
                        }
                        
                    }

                    //if the entrance and stairs are in the grid...might have been overwritten by C; add them in again
                    if (f.FloorID == 0)
                    {
                        UpdateStairsOrCorridorsToZone(f.Entrance, z, 'E');
                    }
                    foreach (Rectangle r in f.StairPoints)
                    {
                        UpdateStairsOrCorridorsToZone(r, z, 'S');
                    }

                    //update all edgepoints of the zone
                    z.FindAllEdgePoints((char)('0' | z.ID));
                    z.UpdateArea();
                }
            }
        }
        private void UpdateStairsOrCorridorsToZone(Rectangle r, Zone z, char c)
        {
            //transfers information from the floor grid into its relative position on the given zone grid
            for (int x = r.X; x < r.X + r.Width; x++)
            {
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if (WithinBounds(x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y, z.RectWidth - 1, z.RectHeight - 1))
                    {
                        z.GetGrid[x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y] = c;
                    }
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
                if (_allFloors[_currentFloorIndex].CorridorStartingRects.Count == 0)
                {
                    Floor f = _allFloors[_currentFloorIndex];

                    //collect each zone growth point to base the corridors on
                    foreach (Zone z in f.Zones)
                    {
                        f.CorridorStartingRects.Add(new Rectangle(z.GrowthPoint, new Point(1, 1)));
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
            foreach (Rectangle r in f.CorridorStartingRects)
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
                    corridorPoints.AddRange(FindShortestPath(p, closestPoint, (char[,])f.GetGrid.Clone(), (p, grid) => grid[p.X, p.Y] != 'V' && grid[p.X, p.Y] != ' ' ));

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
                    corridorPoints.AddRange(FindShortestPath(tempPoints.Item1, tempPoints.Item2, (char[,])f.GetGrid.Clone(), (p, grid) => grid[p.X, p.Y] != 'V' && grid[p.X, p.Y] != ' '));
                }
            }

            //call the floor to make a corridor based on all the paths created
            f.AddCorridor(corridorPoints);
        }
        private List<Point> FindShortestPath(Point start, Point end, char[,] grid, Func<Point, char[,], bool> ValidNextPoint)
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

            //there may be no path found, especially when this function is used to choose door positions
            if (nextPoints.Count != 0)
            {
                //trace back shortest path from end to start
                next = end;
                do
                {
                    corridorList.Add(next);
                    next = previousPoints[next];
                } while (next != start && previousPoints[next] != start);
                corridorList.Add(start);
            }

            return corridorList;
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

        // - update room growth - 
        private string UpdateRoomGrowth()
        {
            if (_currentFloorIndex == -1)
            {
                //set inital values required if this is the first call of this function
                CopyFloorDataToZones();
                SetUpRooms();
                _currentFloorIndex = 0;
                _currentZoneIndex = 0;
                _currentRoomIndex = 0;
                return "> initialising room growth";
            }
            else if (_currentFloorIndex < _allFloors.Count)
            {
                Floor currentFloor = _allFloors[_currentFloorIndex];

                //if all zones on this floor have had rooms grown, move onto the next floor
                if (_currentZoneIndex == currentFloor.Zones.Count)
                {
                    _currentFloorIndex++;
                    _currentZoneIndex = 0;
                } 
                else
                {
                    Zone currentZone = currentFloor.Zones[_currentZoneIndex];

                    //if all the rooms on this zone have been grown...
                    if (_currentRoomIndex == currentZone.Rooms.Count)
                    {
                        //retries room growth if no rooms were successfully grown
                        if (currentZone.Rooms.Count == 0)
                        {
                            //allows 3 retries before grid is changed
                            if (currentZone.RoomGrowthRetries < 3)
                            {
                                currentZone.RoomGrowthRetries++;
                                for (int i = 0; i < currentZone.NumberOfRooms; i++)
                                {
                                    currentZone.Rooms.Add(new Room(i, currentZone.ZoneType.Type, currentZone.GrowthTopLeft));
                                    _currentRoomIndex = 0;                                
                                }
                                return $"> retrying room creation: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                            }
                            //if 3 retries done, "remove" corridors so rooms have more space to grow
                            else if (!currentZone.RoomGrowthFailed)
                            {
                                currentZone.RemoveFromGrid('C', (char)('0' | currentZone.ID));
                                //change the grid to not include C
                                for (int i = 0; i < currentZone.NumberOfRooms; i++)
                                {
                                    currentZone.Rooms.Add(new Room(i, currentZone.ZoneType.Type, currentZone.GrowthTopLeft));
                                    _currentRoomIndex = 0;                             
                                }
                                currentZone.RoomGrowthFailed = true;
                                return $"> retrying room creation: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                            } 
                            //if removing corridors doesn't help, delete the zone as it is too small
                            else {
                                currentFloor.RemoveFromGrid((char)('0' | currentZone.ID));
                                currentFloor.Zones.Remove(currentZone);
                                _currentRoomIndex = 0;
                                //_currentZoneIndex++;
                                return $"> deleting zone {_currentZoneIndex}";
                            }     
                        }
                        //if rooms have been grown successfully, move onto the next zone
                        else if (!currentZone.Rooms[0].Grown)
                        {
                            _currentRoomIndex = 0;
                        }
                        else
                        {
                            //TODO: room splitting here
                            SplitZoneRooms(currentZone);
                            _currentZoneIndex++;
                            _currentRoomIndex = 0;
                            return $"> splitting rooms: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                        }
                    }
                    //if not all rooms have been grown yet
                    else
                    {
                        if (_currentRoomIndex != -1)
                        {
                            //grows the current room and moves onto the next once it is done
                            if (GrowRoom(currentZone.Rooms[_currentRoomIndex], currentZone, currentFloor))
                            {
                                //currentZone.Rooms[_currentRoomIndex].Grown = true;
                                //currentZone.Rooms[_currentRoomIndex].FirstGrown = true;
                                if (!currentZone.Rooms[_currentRoomIndex].FirstGrown)
                                {
                                    currentZone.Rooms[_currentRoomIndex].FirstGrown = true;
                                } else
                                {
                                    currentZone.Rooms[_currentRoomIndex].Grown = true;
                                    //here, do splitting? TODO
                                }
                                _currentRoomIndex++;
                            }
                        }

                        //_currentRoomIndex is only -1 when no good growth points can be found for any room
                        //so we just delete the zone in that case
                        else
                        {
                            currentFloor.RemoveFromGrid((char)('0' | currentZone.ID));
                            currentFloor.Zones.Remove(currentZone);
                        }

                    }
                }
                return $"> growing rooms: floor {_currentFloorIndex}, zone {_currentZoneIndex}, room {_currentRoomIndex}";
            }
            else
            {
                _currentFloorIndex = -1;
                _gameStateIndex++;
                return "> finished room growth";
            }

        }
        private void SetUpRooms()
        {
            //creates the desired number of Room objects for each zone in the school
            foreach (Floor f in _allFloors)
            {
                foreach (Zone z in f.Zones)
                {
                    for (int i = 0; i < z.NumberOfRooms; i++)
                    {
                        z.Rooms.Add(new Room(i, z.ZoneType.Type, z.GrowthTopLeft));
                    }
                }
            }
        }
        private bool GrowRoom(Room r, Zone z, Floor f) 
        {
            //if the room doesn't have a growth point yet, make it
            if (r.WeightedGrid is null)
            {
                SetRoomGrowthPoint(r, z, f.Entrance);
                _previousUpdateTime = DateTime.Now;
            } 

            else if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange)) {

                bool left = false, right = false, up = false, down = false;

                //grow the room in available directions
                GrowGrid(ref left, ref right, ref up, ref down, r, z, _growthSpeed, CheckValidRoomGrowth, (char)('0' | z.ID));

                //update the base rectangle to match new position / dimensions
                r.UpdateBaseRect(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectWidth, r.RectHeight);

                //if the room is too narrow, remove it
                if (((!left && !right && r.RectWidth < RoomType.SideLengths[z.ZoneType.SecondaryType]) || (!up && !down && r.RectHeight < RoomType.SideLengths[z.ZoneType.SecondaryType])) && !r.FirstGrown)
                {
                    z.AddRectToGrid(new Rectangle(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight), (char)('0' | z.ID), true, addRect: false);
                    z.Rooms.Remove(r);
                    z.BadGrowthPoints.Add(r.GrowthPoint);
                }

                //if no more growth available (or required area reached on first growth), finish growth
                //&& r.RectWidth > RoomType.SideLengths[z.ZoneType.SecondaryType] && r.RectHeight > RoomType.SideLengths[z.ZoneType.SecondaryType]
                else if ((!left && !right && !up && !down) || (r.RectWidth * r.RectHeight >= r.IdealSize && !r.FirstGrown ))
                {

                    //remove the room if it is too long
                    //|| r.RectWidth < RoomType.SideLengths[z.ZoneType.SecondaryType] || r.RectHeight < RoomType.SideLengths[z.ZoneType.SecondaryType]
                    if (r.FirstGrown && (r.RectHeight >= r.RectWidth * 3 || r.RectWidth >= r.RectHeight * 3))
                    {
                        z.AddRectToGrid(new Rectangle(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight), (char)('0' | z.ID), true, addRect: false);
                        z.Rooms.Remove(r);
                        z.BadGrowthPoints.Add(new Point(r.GrowthPoint.X, r.GrowthPoint.Y));                
                    }

                    else {
                        SetRoomRect(r, z);
                        return true;
                    }
                    ////delete the zone if the shape is too narrow
                    //if (((!left && !right && z.RectWidth < 75) || (!up && !down && z.RectHeight < 75)) && !z.FirstGrown)
                    //{
                    //    f.Zones.Remove(z);
                    //    // _currentZoneIndex--;
                    //    f.RemoveFromGrid((char)('0' | z.ID));
                    //}

                    ////if no more growth available (or required area reached on first growth), finish
                    //else if ((!left && !right && !up && !down) || (!z.FirstGrown && z.RectWidth * z.RectHeight >= z.IdealSize))
                    //{
                    //    if (z.FirstGrown && (z.RectHeight >= z.RectWidth * 4.5 || z.RectWidth >= z.RectHeight * 4.5))
                    //    {
                    //        f.Zones.Remove(z);
                    //        ///_currentZoneIndex--;
                    //        f.RemoveFromGrid((char)('0' | z.ID));
                    //    }
                    //    else
                    //    {
                    //        SetZoneRect(z, f);
                    //        return true;
                    //    }
                    //}
                }
                

            }
            return false;
        
        }
        private void SetRoomGrowthPoint(Room r, Zone z, Rectangle entrance)
        {
            //create the room's weighted grid and growth point
            r.WeightedGrid = MakeRoomWeightedGrid(r, z, entrance);
            r.GrowthPoint = ChooseGrowthPoint(r.WeightedGrid);

            //if the room's growth point is in a bad position - e.g. outside building - better to delete it
            if (r.WeightedGrid[r.GrowthPoint.X, r.GrowthPoint.Y] < 0)
            {
                z.Rooms.Remove(r);
            } else
            {
                //setting up room properties
                r.RectWidth = 1;
                r.RectHeight = 1;
                r.FloorRectangles.Add(new Rectangle(0, 0, 0, 0));
                r.GrowthTopLeft = r.GrowthPoint;
                r.GrowthFloorPoint = new Point(r.GrowthPoint.X + z.GrowthTopLeft.X, r.GrowthPoint.Y + z.GrowthTopLeft.Y);
            }

        }
        private int[,] MakeRoomWeightedGrid(Room r, Zone z, Rectangle entrance) 
        {
            //75000
            //floorSize / 15
            int minArea = (int)Math.Pow(RoomType.SideLengths[z.ZoneType.SecondaryType], 2) ; //TODO: tweak
            //floorSize / 250 and 40 (could be 50 but i think thats too small
            int maxArea = _floorSize / 4;

            //max of those two...absolute min is the min area
            int estArea = Math.Max((z.Area / z.Rooms.Count), minArea);
            if (z.ZoneType.SecondaryType != "large")
            {
                estArea = Math.Min(estArea, maxArea);
            }
            r.IdealSize = estArea;
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

            //discourage growth near the entrance if there is one
            if (entrance.Width != 0)
            {
                UpdateClosePoints(ref weightedGrid, new Point(entrance.X - z.GrowthTopLeft.X, entrance.Y - z.GrowthTopLeft.Y), (int)(Math.Sqrt(estArea)), -10);
            }

            //encourage a certain distance from other rooms
            foreach (Point p in z.BadGrowthPoints)
            {
                UpdateClosePoints(ref weightedGrid, p, (int)(Math.Sqrt(estArea)), -10);
            }

            //encourage growth in a place such that the room is likely to grow adjacent to other rooms
            foreach (Room room in z.Rooms)
            {
                if (!Room.ReferenceEquals(r, room) && room.Grown)
                {
                    foreach (Point p in room.Edgepoints)
                    {
                        if (weightedGrid[p.X, p.Y] > 0)
                        {
                            UpdateFarPoints(ref weightedGrid, p, (int)(Math.Sqrt(estArea)), 5);
                        }
                      
                    }
                }
            }

            //encourage a distance away from the edge to prevent unrealistic shapes being formed
            foreach (Point p in z.Edgepoints)
            {
                if (weightedGrid[p.X, p.Y] > 0)
                {
                    UpdateFarPoints(ref weightedGrid, p, (int)(Math.Sqrt(estArea)), 5);
                }
            }

            return weightedGrid;
        }
        private bool CheckValidRoomGrowth(int x, int y, int step, int length, char direction, char[,] grid, char zoneID)
        {
            //STEP OVER THIS
            return CheckValidGrowth(x, y, step, length, direction, grid, (grid, x, y) => !(grid[x, y] == zoneID));
        }
        private void SetRoomRect(Room r, Zone z, bool overwrite = false, bool addRect = false)
        {
            char tempID;

            //updates the zone's grid with new dimensions
            r.ResetGrid(r.RectWidth, r.RectHeight, 'R');

            //set all edgepoints of the zone
            r.FindAllEdgePoints('R');

            //sets an ID for the room (can't have the room ID be the same as the zone ID)
            if (z.ID == r.ID)
            {
                //TODO: a problem for future me <3
                tempID = 'U';
            }
            else
            {
                tempID = (char)('0' | r.ID);
            }

            //mark the zone's grid with the completed room shape
            z.AddRectToGrid(new Rectangle(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight), tempID, overwrite, (char)('0' | z.ID), false);
            //adding rect to room
            if (addRect)
            {
                r.FloorRectangles.Add(new Rectangle(r.GrowthTopLeft.X + r.ZoneTopLeft.X, r.GrowthTopLeft.Y + r.ZoneTopLeft.Y, r.RectWidth, r.RectHeight));
            }
        }
        private void SplitZoneRooms(Zone z)
        {
            for (int i = 0; i < z.Rooms.Count; i++)
            {
                SplitRoom(z, i);
            }
        }
        private void SplitRoom(Zone z, int roomIndex)
        {
            //check room + if 75 more than the given type
            //create two rooms
            //delete the first room
            //and split them
            //add them to the end

            Room originalRoom = z.Rooms[roomIndex], room1, room2;
            //make 2 rooms from room if large
            //then delete the room from the index
            //add the two rooms on and run them with their index
            //remember to SetRoomRect at the end too

            if (z.ZoneType.SecondaryType != "large" && (originalRoom.RectWidth > RoomType.SideLengths[z.ZoneType.SecondaryType] * 2.15 || originalRoom.RectHeight > RoomType.SideLengths[z.ZoneType.SecondaryType] * 2.15))
            {
                //split....
                if (originalRoom.RectWidth > originalRoom.RectHeight)
                {
                    //TODO: issue with allocating IDs to new rooms...
                    //get the highest ID in the zone + add 1??
                    room1 = new Room(z.Rooms.Count + 1, z.ZoneType.Type, z.GrowthTopLeft, (int)Math.Floor(originalRoom.RectWidth / 2f) + 1, originalRoom.RectHeight);
                    room1.GrowthTopLeft = originalRoom.GrowthTopLeft;

                    room2 = new Room(z.Rooms.Count + 2, z.ZoneType.Type, z.GrowthTopLeft, (int)Math.Ceiling(originalRoom.RectWidth / 2f) - 1, originalRoom.RectHeight);
                    room2.GrowthTopLeft = new Point(room1.GrowthTopLeft.X + room1.RectWidth, room1.GrowthTopLeft.Y);

                    if (room2.GrowthTopLeft.X + room2.RectWidth < originalRoom.GrowthTopLeft.X + originalRoom.RectWidth)
                    {
                        room2.RectWidth += 1;
                    }

                } else
                {
                    room1 = new Room(z.Rooms.Count + 1, z.ZoneType.Type, z.GrowthTopLeft, originalRoom.RectWidth, (int)Math.Floor( originalRoom.RectHeight / 2f) + 1);
                    room1.GrowthTopLeft = originalRoom.GrowthTopLeft;

                    room2 = new Room(z.Rooms.Count + 2, z.ZoneType.Type, z.GrowthTopLeft, originalRoom.RectWidth, (int)Math.Floor(originalRoom.RectHeight / 2f) - 1);
                    room2.GrowthTopLeft = new Point(room1.GrowthTopLeft.X, room1.GrowthTopLeft.Y + room1.RectHeight);

                    if (room2.GrowthTopLeft.Y + room2.RectHeight < originalRoom.GrowthTopLeft.Y + originalRoom.RectHeight)
                    {
                        room2.RectHeight += 1;
                    }
                }

                //remove the 1st room
                z.Rooms.RemoveAt(roomIndex);

                //add the 1st room and recurse
                z.Rooms.Add(room1);
                SetRoomRect(z.Rooms[z.Rooms.Count - 1], z, true, true);
                SplitRoom(z, z.Rooms.Count - 1);

                z.Rooms.Add(room2);
                SetRoomRect(z.Rooms[z.Rooms.Count - 1], z, true, true);
                SplitRoom(z, z.Rooms.Count - 1);
            }

        }

        // - create room details -
        private string UpdateFurnitureCreation()
        {
            if (_currentFloorIndex == -1)
            {
                //at the first call of this function, set up the rooms appropriately
                InitialiseFurnitureCreation();
                return "> initialising furniture creation";
            }

            //if a floor currently needs furniture added...
            else if (_currentFloorIndex < _allFloors.Count)
            {
                Floor currentFloor = _allFloors[_currentFloorIndex];

                //if all the zones in a floor have just had furniture and doors added, create the walls for every room
                //if (_currentZoneIndex == currentFloor.Zones.Count && !currentFloor.MadeWalls)
                //{
                //    AddWallsToFloor(currentFloor);
                //    _previousUpdateTime = DateTime.Now;
                //    return $"> added walls: floor {_currentFloorIndex}";
                //}

                //quickly add all floors and walls??
                if (_currentZoneIndex == 0 && !currentFloor.MadeWalls)
                {
                    foreach (Zone z in currentFloor.Zones)
                    {
                        AddDoors(currentFloor, z);
                    }
                    AddWallsToFloor(currentFloor);
                    currentFloor.MadeWalls = true;
                    return $"> added doors and walls: floor {_currentFloorIndex}";
                }

                //if everything has been added to a floor (and enough time has passed for display), move onto the next floor
                else if (_currentZoneIndex == currentFloor.Zones.Count && currentFloor.MadeWalls && DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange))
                {
                    _currentFloorIndex++;
                    _currentZoneIndex = 0;
                    _currentRoomIndex = 0;
                      return $"> finished furniture creation: floor {_currentFloorIndex - 1}";
                }

                //if zones in the floor still need furniture added...
                else if (_currentZoneIndex < currentFloor.Zones.Count)
                {

                    //if room index = 0 and no doors..
                    //else...

                    Zone currentZone = currentFloor.Zones[_currentZoneIndex];

                    //if (_currentRoomIndex == 0 && !currentZone.FinishedDoors)
                    //{
                    //    AddDoors(currentFloor, currentZone);
                    //    currentZone.FinishedDoors = true;
                    //    return $"> created doors: floor {_currentFloorIndex}, zone {_currentZoneIndex}";
                    //}

                    //if all rooms in a zone have furniture, add the doors and move onto the next zone
                    if (_currentRoomIndex == currentZone.Rooms.Count)
                    {
                        //AddDoors(currentFloor, currentZone);
                        _currentZoneIndex++;
                        _currentRoomIndex = 0;
                        return $"> finished creating furniture: floor {_currentFloorIndex}, zone {_currentZoneIndex - 1}";
                    } 

                    //if some rooms still need furniture (and enough time has passed between last update), create furniture <--- TODO
                    else if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange)) {
                        Room currentRoom = currentZone.Rooms[_currentRoomIndex];

                        //do stuff here
                        CreateFurniture(currentRoom);

                        _previousUpdateTime = DateTime.Now;
                        _currentRoomIndex++;

                        return $"> created furniture: floor {_currentFloorIndex}, zone {_currentZoneIndex}, room {_currentRoomIndex - 1}";
                    }
                }

                //don't return a message if nothing has been updated
                return "";
            }
            else
            {
                return "> finished creating furniture";
            }
        }
        private void InitialiseFurnitureCreation()
        {
            //set indexes
            _currentFloorIndex = 0;
            _currentZoneIndex = 0;
            _currentRoomIndex = 0;
            _previousUpdateTime = DateTime.Now;

            foreach (Floor f in _allFloors)
            {
                //set up the floor's "room grid" which only shows the position of rooms
                f.SetRoomGrid();

                foreach (Zone z in f.Zones)
                {
                    foreach (Room r in z.Rooms)
                    {
                        //add each room to the floor's room grid
                        f.AddToRoomGrid(new Rectangle(z.GrowthTopLeft.X + r.GrowthTopLeft.X, z.GrowthTopLeft.Y + r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight));
                        
                        //remove corridors that may have been left inside a room
                        RemoveCorridorsFromRoom(f, r, (char)('0' | z.ID));
                    }
                    //corridors may have been previously omitted from zone grid - add them now
                    AddCorridorsToZone(f, z);
                }
            }
        }
        private void RemoveCorridorsFromRoom(Floor f, Room r, char zoneID)
        {
            //iterate across whole room
            for (int x = 0; x < r.RectWidth; x++)
            {
                for (int y = 0; y < r.RectHeight; y++)
                {
                    //if the floor shows that a position is a corridor point, remove that corridor point
                    if (f.GetGrid[r.FloorRectangles[0].X + x, r.FloorRectangles[0].Y + y] == 'C')
                    {
                        f.RemoveCorridorPoint(r.FloorRectangles[0].X + x, r.FloorRectangles[0].Y + y, zoneID);
                    }
                }

            }

        }
        private void AddCorridorsToZone(Floor f, Zone z)
        {
            //iterate through zone grid and re-add corridors where appropriate
            for (int x = z.GrowthTopLeft.X; x < z.GrowthTopLeft.X + z.RectWidth; x++)
            {
                for (int y = z.GrowthTopLeft.Y; y < z.GrowthTopLeft.Y + z.RectHeight; y++)
                {
                    if (f.GetGrid[x, y] == 'C')
                    {
                        z.GetGrid[x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y] = 'C';
                    }
                }
            }
        }

        // - - creating furniture - - TODO: clean up this mess
        private void CreateFurniture(Room r)
        {
            switch (r.Type)
            {
                case "english":
                    MakeNormalClassroomFurniture(r);
                    break;
                case "maths":
                    MakeNormalClassroomFurniture(r);
                    break;
                case "religious education":
                    MakeNormalClassroomFurniture(r);
                    break;
                case "languages":
                    MakeNormalClassroomFurniture(r);
                    break;
                case "science":
                    MakeScienceClassroomFurniture(r);
                    break;
                case "computer science":
                    MakeComputerScienceClassroomFurniture(r);
                    break;
                case "art":
                    MakeArtClassroomFurniture(r);
                    break;
                case "design technology":
                    MakeArtClassroomFurniture(r);
                    break;
                case "music":
                    MakeMusicClassroomFurniture(r);
                    break;
                case "hall":
                    MakeHallFurniture(r);
                    break;
                case "gym":
                    MakeGymFurniture(r);
                    break;
                case "canteen":
                    MakeCanteenFurniture(r);
                    break;
                case "staffroom":
                    MakeStaffRoomFurniture(r);
                    break;
                case "toilets":
                    MakeToiletFurniture(r);
                    break;
                case "office":
                    MakeOfficeFurniture(r);
                    break;
                default:
                    break;

            }
            r.CopyChairAndTableDataToGrid();

        }

        // - - - making specific types of room - - -
        private void MakeNormalClassroomFurniture(Room r)
        {
            //adds default classroom furniture
            AddTeacherDesk(r);
            AddCupboard(r);
            AddSubjectDesks(r);
            MakeNormalTablesAndChairs(r);
        }
        private void MakeScienceClassroomFurniture(Room r)
        {
            //adds slightly larger desk + furniture
            AddTeacherDesk(r, 5, 40, 25, 15, 5, 30, 13, 5, 12);
            AddCupboard(r);
            AddSubjectDesks(r);
            AddScienceDesks(r);
        }
        private void MakeComputerScienceClassroomFurniture(Room r)
        {
            int gap = 30;
            //create slightly larger desk + additional furniture
            AddTeacherDesk(r, 5, 40, 25, 15, 5, 30, 13, 5, 12);
            AddCupboard(r);
            AddSubjectDesks(r);

            //used lined tables (more space efficient) if less room available; otherwise use science room arrangement
            if (r.RectHeight * r.RectWidth <= 20000)
            {
                MakeLinedTables(new char[r.RectWidth - gap, r.RectHeight - gap], r, gap);
            }
            else
            {
                AddOuterTables(r, gap);
            }
        }
        private void MakeArtClassroomFurniture(Room r)
        {
            int innerGap = 30;

            //creating larger desk and other furniture
            AddTeacherDesk(r, 5, 40, 25, 15, 5, 30, 13, 5, 12);
            AddCupboard(r);
            AddSubjectDesks(r);

            //if room isn't wide enough, use lined tables; only use large grouped tables if there is enough room
            if (r.RectWidth < innerGap + 65 && r.RectHeight < innerGap + 65)
            {
                MakeLinedTables(new char[r.RectWidth - innerGap, r.RectHeight - innerGap], r, innerGap);
            } else
            {
                MakeGroupedTables(new char[r.RectWidth - innerGap - 10, r.RectHeight - innerGap - 10], r, innerGap + 10);
            }
        }
        private void MakeMusicClassroomFurniture(Room r)
        {
            AddTeacherDesk(r, 5, 40, 25, 15, 5, 30, 13, 5, 12);
            AddCupboard(r);
            AddSubjectDesks(r);
            AddMusicTables(r);
        }
        private void MakeHallFurniture(Room r)
        {
            AddHallStage(r);
            AddHallChairs(r);
        }
        private void MakeGymFurniture(Room r, int innerGap = 40)
        {
            char[,] innerGrid = new char[r.RectWidth - innerGap, r.RectHeight - innerGap]; 
            //adding benches
            AddSubjectDesks(r, 75, 15, 10);
            //adding mats
            MakeGroupedTables(innerGrid, r, innerGap, true);
        }
        private void MakeCanteenFurniture(Room r)
        {
            int gap = 30, shiftX = 0, shiftY = 0, tableWidth = 15, tableGap = 5;
            int stallWidth = tableWidth + 5;
            Rectangle kitchenSpace;

            //the "stage" will act as room for the kitchen staff
            AddHallStage(r);
            kitchenSpace = r.EquipmentDesks[0];

            //add "wrap around" tables and front stall table depending on how the kitchen area is facing the rest of the room
            //shiftX andf shiftY used to move canteen tables so they don't overlap with the kitchen area
            switch (r.FacingTowards) {
                case "left":
                    shiftX = kitchenSpace.Width;
                    MakeGroupedTables(new char[r.RectWidth - gap - kitchenSpace.Width, r.RectHeight - gap], r, gap);
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X + tableGap, kitchenSpace.Y + tableGap, kitchenSpace.Width - 2 * tableGap, tableWidth));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X + tableGap, kitchenSpace.Bottom - tableWidth - tableGap, kitchenSpace.Width - 2 * tableGap, tableWidth));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X + tableGap, kitchenSpace.Y + tableGap, tableWidth, kitchenSpace.Height - 2 * tableGap));
                    r.ExtraFurnitureList2.Add(new Rectangle(kitchenSpace.Right - stallWidth, kitchenSpace.Y + stallWidth, stallWidth, kitchenSpace.Height - 2 * stallWidth));
                    break;
                case "right":
                    shiftX = -kitchenSpace.Width;
                    MakeGroupedTables(new char[r.RectWidth - gap - kitchenSpace.Width, r.RectHeight - gap], r, gap);
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X + tableGap, kitchenSpace.Y + tableGap, kitchenSpace.Width - 2 * tableGap, tableWidth));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X + tableGap, kitchenSpace.Bottom - tableWidth - tableGap, kitchenSpace.Width - 2 * tableGap, tableWidth));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.Bottom - tableWidth - tableGap, kitchenSpace.Y, tableWidth, kitchenSpace.Height));
                    r.ExtraFurnitureList2.Add(new Rectangle(kitchenSpace.X, kitchenSpace.Y + stallWidth, stallWidth, kitchenSpace.Height - 2 * stallWidth));
                    break;
                case "up":
                    shiftY = kitchenSpace.Height;
                    MakeGroupedTables(new char[r.RectWidth - gap, r.RectHeight - gap - kitchenSpace.Height], r, gap);
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X, kitchenSpace.Y, tableWidth, kitchenSpace.Height));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.Width - tableWidth, kitchenSpace.Y, tableWidth, kitchenSpace.Height));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X, kitchenSpace.Y, kitchenSpace.Width, tableWidth));
                    r.ExtraFurnitureList2.Add(new Rectangle(kitchenSpace.X + stallWidth, kitchenSpace.Height - stallWidth, kitchenSpace.Width - 2 * stallWidth, stallWidth));
                    break;
                case "down":
                    shiftY = -kitchenSpace.Height;
                    MakeGroupedTables(new char[r.RectWidth - gap, r.RectHeight - gap - kitchenSpace.Height], r, gap);
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X, kitchenSpace.Y, tableWidth, kitchenSpace.Height));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.Width - tableWidth, kitchenSpace.Y, tableWidth, kitchenSpace.Height));
                    r.ExtraFurnitureList1.Add(new Rectangle(kitchenSpace.X, kitchenSpace.Width - tableWidth, kitchenSpace.Width, tableWidth));
                    r.ExtraFurnitureList2.Add(new Rectangle(kitchenSpace.X + stallWidth, kitchenSpace.Y, kitchenSpace.Width - 2 * stallWidth, stallWidth));
                    break;
            }

            //add grouped tables and chairs, shifted away from the kitchen
            r.Tables = r.Tables.Select(i => new Rectangle(i.X + shiftX, i.Y + shiftY, i.Width, i.Height)).ToList();
            r.Chairs = r.Chairs.Select(i => new Rectangle(i.X + shiftX, i.Y + shiftY, i.Width, i.Height)).ToList();
        }
        private void MakeStaffRoomFurniture(Room r)
        {
            //normal classroom furniture without a teacher desk
            int gap = 25;
            AddSubjectDesks(r, 30, 15, 6);
            AddCupboard(r);
            MakeGroupedTables(new char[r.RectWidth - gap, r.RectHeight - gap], r, gap);
        }
        private void MakeOfficeFurniture(Room r)
        {
            //adding an especially long desk for office staff
            AddTeacherDesk(r, 5, (int)(Math.Min(r.RectWidth, r.RectHeight) * 0.75), 20, 15, 5, (int)(Math.Min(r.RectWidth, r.RectHeight) * 0.75));
            AddCupboard(r);
            AddSubjectDesks(r, upperLimit: 6);
        }

        // - - - making specific pieces of furniture - - -
        private void AddScienceDesks(Room r)
        {
            //ensures gap of 30 pixels from furniture to each wall of room
            int innerGap = 30;

            //randomly choose what types of tables to create; skewed towards "wrap around" tables
            double choice = _random.NextDouble();
            if (choice <= 0.75)
            {
                AddOuterTables(r, innerGap);
            } 
            else
            {
                //increase the gap because grouped tables are wider
                char[,] innerGrid = new char[r.RectWidth - innerGap - 10, r.RectHeight - innerGap - 10];
                MakeGroupedTables(innerGrid, r, innerGap + 10);
            }
        }
        private void AddMusicTables(Room r)
        {
            int innerGap = 30;
            char[,] innerGrid = new char[r.RectWidth - innerGap - 20, r.RectHeight - innerGap - 20];
            double choice = _random.NextDouble();

            //randomly choose between wrap-around tables and normal lined tables...slightly skewed towards the former
            if (choice < 0.6)
            {
                AddOuterTables(r, innerGap);
            } else
            {
                MakeLinedTables(innerGrid, r, innerGap + 20);
            }
        }
        private void AddHallStage(Room r, int wallWidth = 5)
        {
            int stageHeight = 0, stageWidth = 0, stageHeightCount = 0, stageWidthCount = 0;
            bool valid = false;
            List<Point> validStagePoints = new List<Point>();
            Rectangle stage = new Rectangle(0, 0, 0, 0);

            //finding the best width + height of stage that doesn't block any doors
            do
            {
                stageHeightCount = 0;
                do
                {
                    //attempt to make stage on left and right
                    stageWidth = (r.RectWidth - 10) / 5 - 5 * stageWidthCount;
                    //only do so if the current stage width is large enough

                    if (stageWidth > (r.RectWidth - 10) / 7)
                    {
                        //choose a height that decreases with each iteration
                        stageHeight = r.RectHeight - 10 - 5 * stageHeightCount;
                        //only continue if the chosen height is large enough

                        if (stageHeight >= (r.RectHeight - 10) * 0.8)
                        {
                            //attempt to find valid positions using the selected stage width and height
                            validStagePoints = FindEdgeRectPositions(stageHeight, stageWidth, r, wallWidth).Where(i => ( i.X == 5 || i.X == r.RectWidth - wallWidth - 1) && i.Y != r.RectHeight - wallWidth - 1 ).ToList();
                            
                            //if there are valid points...
                            if (validStagePoints.Count > 0)
                            {
                                //add the stage to the room
                                valid = true;
                                stage = GetEdgeRectFromPoint(r, validStagePoints[_random.Next(0, validStagePoints.Count)], stageHeight, stageWidth, wallWidth, true, false);
                                
                                //set the direction that the stage is facing towards
                                if (stage.X == wallWidth)
                                {
                                    r.FacingTowards = "left";
                                }
                                else
                                {
                                    r.FacingTowards = "right";
                                }
                                break;
                            }
                        }
                        
                    }

                    //attempt to add stage on top and bottom of room

                    //select a stage width, only continue if it is large enough
                    stageWidth = (r.RectHeight - 10) / 5 - 5 * stageWidthCount;

                    if (stageWidth > (r.RectHeight - 10) / 10)
                    {
                        //select a stage height that decreases with each iteration, only continue if it is large enough
                        stageHeight = r.RectWidth - 10 - 5 * stageHeightCount;

                        if (stageHeight >= (r.RectWidth - 10) * 0.8)
                        {
                            //try to find valid positions for the stage
                            validStagePoints = FindEdgeRectPositions(stageHeight, stageWidth, r, wallWidth).Where(i => (i.Y == wallWidth || i.Y == r.RectHeight - wallWidth - 1) && i.X != r.RectWidth - wallWidth - 1).ToList();

                            //if there are possible positions...
                            if (validStagePoints.Count > 0)
                            {
                                valid = true;

                                //create the stage and set the room's direction
                                stage = GetEdgeRectFromPoint(r, validStagePoints[_random.Next(0, validStagePoints.Count)], stageHeight, stageWidth, wallWidth, false, true);
                                if (stage.Y == wallWidth)
                                {
                                    r.FacingTowards = "up";
                                } else
                                {
                                    r.FacingTowards = "down";
                                }
                                break;
                            }
                        }
                        stageHeightCount++;
                    }
                    //stop if the stage's height becomes too small
                } while (stageHeight >= (Math.Min(r.RectWidth - 10, r.RectHeight - 10) * 0.8) && !valid);
                stageWidthCount++;
                //stop if the stage's width becomes too small
            } while (stageWidth > (Math.Min(r.RectWidth - 10, r.RectHeight - 10) / 10) && !valid);

            //if no stage position has been found, add one that allows for a gap between all walls
            if (!valid)
            {
                if (r.RectWidth > r.RectHeight)
                {
                    stage = new Rectangle(15, 15, r.RectWidth - 2 * wallWidth - 20, (r.RectHeight - 10) / 5);
                } else
                {
                    stage = new Rectangle(15, 15, r.RectHeight - 2 * wallWidth - 20, (r.RectWidth - 10) / 5);
                }
            }

            //add the stage to the room
            r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(stage)); 
        }
        private void AddHallChairs(Room r, int wallWidth = 5)
        {
            //can change chair dimensions here
            int chairLength = 15, gapBetweenChairs = 10, innerGridWidth, innerGridHeight, extraX, extraY;
            List<Rectangle> outerRects;        

            //choose how to shift the chairs so they don't overlap woth the stage - depends where the stage is
            if (r.FacingTowards == "left" || r.FacingTowards == "right")
            {
                //the inner grid is the space in the room free for chairs
                innerGridWidth = r.RectWidth - 2 * wallWidth - r.EquipmentDesks[0].Width - gapBetweenChairs;
                innerGridHeight = r.RectHeight - 2 * wallWidth - gapBetweenChairs;

                //extraY used to shift chairs down, prevent overlap with walls
                extraY = wallWidth + gapBetweenChairs;

                //extraX is used to shift chairs left / right; set to prevent overlap with stage
                if (r.FacingTowards == "left")
                {
                    extraX = r.EquipmentDesks[0].Width + gapBetweenChairs + wallWidth;
                   
                } else
                {
                    extraX = gapBetweenChairs + wallWidth;
                }
            }
            else
            {
                //set innerGrid dimensions
                innerGridWidth = r.RectWidth - 2 * wallWidth - gapBetweenChairs;
                innerGridHeight = r.RectHeight - 2 * wallWidth - r.EquipmentDesks[0].Height - gapBetweenChairs;

                //extraX prevents chair overlap with walls
                extraX = wallWidth + gapBetweenChairs;

                //set extraY to shift chairs up / down and prevent overlap with stage
                if (r.FacingTowards == "up")
                {
                    extraY = r.EquipmentDesks[0].Height + gapBetweenChairs + wallWidth;
                } else
                {
                    extraY = gapBetweenChairs + wallWidth;
                }
            }

            //make a list of smaller rectangkes for each individual chair to fit in
            outerRects = MakeEnclosingRectangles(chairLength + gapBetweenChairs, chairLength + gapBetweenChairs, innerGridHeight, innerGridWidth);

            //iterate through these rectangles, adding a chair in the middle of each one
            foreach (Rectangle rect in outerRects)
            {
                r.Chairs.Add(r.MakeRectPosRelativeToFloor(new Rectangle(rect.X + gapBetweenChairs, rect.Y + gapBetweenChairs, chairLength, chairLength), extraX, extraY));
            }
        }
        private void AddTeacherDesk(Room r, int wallWidth = 5, int length = 35, int width = 20, int deskOffset = 15, int deskGap = 5, int deskLength = 25, int deskWidth = 8, int chairOffset = 5, int chairLength = 10)
        {
            //enclosingRect contains space for the desk and the chair together
            Rectangle enclosingRect, desk, chair;
            //find a suitable position on the edge for the enclosing rectangle
            enclosingRect = AddEdgeRect(r, length, width);

            //if the position is on the left or right...
            if (enclosingRect.Width == width)
            {
                //if the rectangle is on the left of the room, make the chair to the left of the desk
                if (enclosingRect.X == wallWidth)
                {
                    desk = new Rectangle(deskOffset, enclosingRect.Y + deskGap, deskWidth, deskLength);
                    chair = new Rectangle(wallWidth, (deskLength / 2 - chairLength / 2) + enclosingRect.Y + deskGap, chairLength, chairLength);
                    r.FacingTowards = "left";
                }
                //if the rectangle is on the right, make the chair on the right of the desk
                else
                {                
                    chair = new Rectangle(enclosingRect.X + enclosingRect.Width - chairLength, (deskLength / 2 - chairLength / 2) + enclosingRect.Y + deskGap, chairLength, chairLength);
                    desk = new Rectangle(chair.X - deskWidth, enclosingRect.Y + deskGap, deskWidth, deskLength);
                    r.FacingTowards = "right";
                }
            }
            //if the rectangle's position is at the top or bottom of the room...
            else
            {
                //if the rectangle is at the top, add the chair above the desk
                if (enclosingRect.Y == wallWidth)
                {
                    desk = new Rectangle(enclosingRect.X + deskGap, deskOffset, deskLength, deskWidth);
                    chair = new Rectangle(deskLength / 2 - chairLength / 2 + enclosingRect.X + deskGap, chairOffset, chairLength, chairLength);
                    r.FacingTowards = "up";
                }
                //if the rectangle is at the bottom, add the chair below the desk
                else
                {
                    chair = new Rectangle(deskLength / 2 - chairLength / 2 + enclosingRect.X + deskGap, enclosingRect.Y + enclosingRect.Height - chairLength, chairLength, chairLength);
                    desk = new Rectangle(enclosingRect.X + deskGap, chair.Y - deskWidth, deskLength, deskWidth);
                    r.FacingTowards = "down";
                }
            }

            //add the chair and desk to the room, with appropriately shifted positions
            r.TeacherDesk = r.MakeRectPosRelativeToFloor(desk);
            r.TeacherChair = r.MakeRectPosRelativeToFloor(chair);
        }
        private void MakeToiletFurniture(Room r)
        {
            //clearPoints: positions in the room to avoid, e.g. in front of door, for this purpose we make them relative to the room instead of the floor
            List<Point> validPoints = new List<Point>(), clearPoints = r.InnerClearPoints.Select(i => new Point(i.X - r.GrowthTopLeft.X - r.ZoneTopLeft.X, i.Y - r.GrowthTopLeft.Y - r.ZoneTopLeft.Y)).ToList();
            bool leftFree = true, rightFree = true, upFree = true, downFree = true;

            //check for what sides of the room have doors; cubicles are set to avoid them
            foreach (Point p in clearPoints)
            {
                if (p.X == 5)
                {
                    leftFree = false;
                }
                else if (p.X == r.RectWidth - 5 - 1)
                {
                    rightFree = false;
                }
                if (p.Y == 5)
                {
                    upFree = false;
                }
                else if (p.Y == r.RectHeight - 5 - 1)
                {
                    downFree = false;
                }

            }

            //make cubicles and sinks, preferably opposite each other, cubicles should be made on a side without doors
            if (upFree && downFree)
            {
                MakeToiletCubicles(r, "up");
                MakeToiletSinks(r, "down");
            }
            else if (leftFree && rightFree)
            {
                MakeToiletCubicles(r, "left");
                MakeToiletSinks(r, "right");

            }
            else if (upFree)
            {
                MakeToiletCubicles(r, "up");
                MakeToiletSinks(r, "down");
            }
            else if (downFree)
            {
                MakeToiletCubicles(r, "down");
                MakeToiletSinks(r, "up");
            }
            else if (leftFree)
            {
                MakeToiletCubicles(r, "left");
                MakeToiletSinks(r, "right");
            }
            else if (rightFree)
            {
                MakeToiletCubicles(r, "right");
                MakeToiletSinks(r, "left");
            }
            else
            {
                //if each side has a door - very unlikely - add cubicles and sinks anyway
                if (r.RectWidth > r.RectHeight)
                {
                    MakeToiletCubicles(r, "down");
                    MakeToiletSinks(r, "up");
                } else
                {
                    MakeToiletCubicles(r, "right");
                    MakeToiletSinks(r, "left");
                }
            }
        }
        private void MakeToiletCubicles(Room r, string position, int wallWidth = 5)
        {
            Rectangle mask = new Rectangle(0, 0, 0, 0);
            int gap = 5, width, height, cublicleWidth = 45;
            List<Rectangle> cubicles;
            string doorPosition;
            
            if (position == "left" || position == "right")
            {
                //set the cubicle's width and height
                width = (r.RectWidth - 2 * wallWidth) / 2;
                height = r.RectHeight - 2 * wallWidth - 2 * gap;

                //create the outer rectangle (mask) enclosing all cubicles based on position parameter
                //and set position of door to be on the opposite side
                if (position == "left")
                {
                    mask = new Rectangle(wallWidth, wallWidth + gap, width, height);
                    doorPosition = "right";
                } else
                {
                    mask = new Rectangle(r.RectWidth - wallWidth - width, wallWidth + gap, width, height);
                    doorPosition = "left";
                }

                //make individual cubicle masks within the larger mask
                cubicles = MakeEnclosingRectangles(cublicleWidth, width, height, width).Select(i => new Rectangle(i.X + mask.X, i.Y + mask.Y, i.Width, i.Height)).ToList();

            } else
            {
                //set the cubicle's width and height
                width = r.RectWidth - 2 * wallWidth - 2 * gap;
                height = (r.RectHeight - 2 * wallWidth) / 2;

                //create the mask enclosing all cubicles based on position parameter and set position of door to be on the opposite side
                if (position == "up")
                {
                    mask = new Rectangle(wallWidth + gap, wallWidth, width, height);
                    doorPosition = "down";
                } else
                {
                    mask = new Rectangle(wallWidth + gap, r.RectHeight - wallWidth - height, width, height);
                    doorPosition = "up";
                }

                //make individual cubicle masks within the larger mask
                cubicles = MakeEnclosingRectangles(height, cublicleWidth, height, width).Select(i => new Rectangle(i.X + mask.X, i.Y + mask.Y, i.Width, i.Height)).ToList();
            }

            //iterate through each cubicle mask to add detail inside them
            foreach (Rectangle cubicle in cubicles)
            {
                MakeIndividualCubicles(r, cubicle, doorPosition);
            }
        }
        private void MakeIndividualCubicles(Room r, Rectangle mask, string doorPosition)
        {
            int innerWallWidth = 3, doorLength = 8, toiletBackLength = 15, toiletBackWidth = 10, toiletSeatLength = 10;
            Rectangle tempToiletBack;
            
            //if door is on the left / right
            if (doorPosition == "left" || doorPosition == "right")
            {
                //add walls at top + bottom of cubicle
                r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Y, mask.Width, innerWallWidth)));
                r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Bottom - innerWallWidth, mask.Width, innerWallWidth)));

                if (doorPosition == "left")
                {
                    //add wall and door on left
                    r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Y, innerWallWidth, mask.Height)));
                    r.ExtraFurnitureList1.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Y + mask.Height / 2 - 2, innerWallWidth, doorLength)));

                    //add both parts of toilet on right
                    tempToiletBack = new Rectangle(mask.Right - toiletBackWidth, mask.Y + mask.Height / 2 - toiletBackLength / 2, toiletBackWidth, toiletBackLength);
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(tempToiletBack));
                    r.Chairs.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempToiletBack.X - toiletSeatLength, mask.Y + mask.Height / 2 - toiletSeatLength / 2, toiletSeatLength, toiletSeatLength)));
                    
                } else
                {
                    //add wall and door on right
                    r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.Right - innerWallWidth, mask.Y, innerWallWidth, mask.Height)));
                    r.ExtraFurnitureList1.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.Right - innerWallWidth, mask.Y + mask.Height / 2 - 2, innerWallWidth, doorLength)));
                    
                    //add both parts of toilet on left
                    tempToiletBack = new Rectangle(mask.X, mask.Y + mask.Height / 2 - toiletBackLength / 2, toiletBackWidth, toiletBackLength);
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(tempToiletBack));
                    r.Chairs.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempToiletBack.Right, mask.Y + mask.Height / 2 - toiletSeatLength / 2, toiletSeatLength, toiletSeatLength)));
                }

            } else //if cubicles are facing up / down
            {
                //add walls left and right
                r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Y, innerWallWidth, mask.Height)));
                r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.Right - innerWallWidth, mask.Y, innerWallWidth, mask.Height)));

                if (doorPosition == "up")
                {
                    //add wall and door on top
                    r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Y, mask.Width, innerWallWidth)));
                    r.ExtraFurnitureList1.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X + mask.Width / 2 - 2, mask.Y, doorLength, innerWallWidth)));
                    
                    //add toilet on the bottom
                    tempToiletBack = new Rectangle(mask.X + mask.Width / 2 - toiletBackLength / 2, mask.Bottom - toiletBackWidth, toiletBackLength, toiletBackWidth);
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(tempToiletBack));
                    r.Chairs.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X + mask.Width / 2 - toiletSeatLength / 2, tempToiletBack.Y - toiletSeatLength, toiletSeatLength, toiletSeatLength)));
                } else
                {
                    //add wall and door on bottom
                    r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X, mask.Bottom - innerWallWidth, mask.Width, innerWallWidth)));
                    r.ExtraFurnitureList1.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X + mask.Width / 2 - 2, mask.Bottom - innerWallWidth, doorLength, innerWallWidth)));
                   
                    //add toilet at top
                    tempToiletBack = new Rectangle(mask.X + mask.Width / 2 - toiletBackLength / 2, mask.Y, toiletBackLength, toiletBackWidth);
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(tempToiletBack));
                    r.Chairs.Add(r.MakeRectPosRelativeToFloor(new Rectangle(mask.X + mask.Width / 2 - toiletSeatLength / 2, tempToiletBack.Bottom, toiletSeatLength, toiletSeatLength)));
                }
            }

        }
        private void MakeToiletSinks(Room r, string position)
        {
            int sinkLength = 17, sinkWidth = 13, gap = 7;
            List<Point> sinkPoints = new List<Point>(), clearPoints = new List<Point>();
            Rectangle sinkRect;

            do
            {
                //find possible positions of next sink on specified part of room
                switch (position)
                {
                    case "left":
                        sinkPoints = FindEdgeRectPositions(sinkLength + gap, sinkWidth, r, 5).Where(i => i.X == 5).ToList();
                        break;
                    case "right":
                        sinkPoints = FindEdgeRectPositions(sinkLength + gap, sinkWidth, r, 5).Where(i => i.X == r.RectWidth - 5 - 1).ToList();
                        break;
                    case "up":
                        sinkPoints = FindEdgeRectPositions(sinkLength + gap, sinkWidth, r, 5).Where(i => i.Y == 5).ToList();
                        break;
                    case "down":
                        sinkPoints = FindEdgeRectPositions(sinkLength + gap, sinkWidth, r, 5).Where(i => i.Y == r.RectHeight - 5 - 1).ToList();
                        break;
                }
                
                //if there is space for the next sink...
                if (sinkPoints.Count > 0)
                {
                    //create the sink's rectangle
                    sinkRect = GetEdgeRectFromPoint(r, sinkPoints[0], sinkLength + gap, sinkWidth, 5, (position == "left" || position == "right"), (position == "up" || position == "down"));

                    //add new points to avoid so future sinks won't overlap with this one
                    clearPoints = GetClearPointsFromRect(r.InnerEdgePoints, sinkRect);
                    r.InnerClearPoints.AddRange(clearPoints.Select(i => r.MakePointRelativeToFloor(i)));

                    //add sink to room, with adjusted position and dimensions
                    if (position == "left" || position == "right")
                    {
                        r.ExtraFurnitureList2.Add(r.MakeRectPosRelativeToFloor(new Rectangle(sinkRect.X, sinkRect.Y, sinkWidth, sinkLength)));
                    } else
                    {
                        r.ExtraFurnitureList2.Add(r.MakeRectPosRelativeToFloor(new Rectangle(sinkRect.X, sinkRect.Y, sinkLength, sinkWidth)));
                    }                   
                }
            } while (sinkPoints.Count > 0);
        }
        private void AddCupboard(Room r)
        {
            //adds a cupboard with fixed dimensions to a room
            Rectangle tempCupboard = AddEdgeRect(r, 28, 14);
            r.Cupboard = r.MakeRectPosRelativeToFloor(tempCupboard);
        }
        private void AddSubjectDesks(Room r, int width = 30, int height = 14, int upperLimit = 5)
        {
            Rectangle tempRect;

            //choose a random number of desks to have
            int j = _random.Next(0, upperLimit);

            //add this number of desks to walls of room
            for (int i = 0; i < j; i++)
            {
                tempRect = AddEdgeRect(r, width, height);
                r.EquipmentDesks.Add(r.MakeRectPosRelativeToFloor(tempRect));
            }
        }
        private void MakeNormalTablesAndChairs(Room r)
        {
            //adds chairs and tables in typical arrangement

            //using an inner grid within room guarantees space between walls and tables
            int innerGap = 40;
            char[,] innerGrid = new char[r.RectWidth - innerGap, r.RectHeight - innerGap];

            //used to choose what type of table to create
            int tableTypeChoice = _random.Next(0, 2);

            //if the room is too big, add grouped tables which are less space-efficient (added to prevent rooms with a ridiculous number of chairs)
            if (r.RectWidth * r.RectHeight >= 40000)
            {
                MakeGroupedTables(innerGrid, r, innerGap);
            }
            //otherwise create tables based on randomly selected choice
            else if (tableTypeChoice == 0)
            {
                MakeLinedTables(innerGrid, r, innerGap, 3); //tables have spaces between them
            }
            else
            {
                MakeLinedTables(innerGrid, r, innerGap, 0); //no gaps between tables
            };
        }
        private void MakeLinedTables(char[,] grid, Room r, int innerGap, int gapBetweenTables = 0)
        {
            //creates tables + chairs in a lined arrangement

            int outerWidth = 25, outerLength = 30;
            List<Rectangle> outerRects;
            List<(Rectangle, Rectangle)> tablesAndChairs = new List<(Rectangle, Rectangle)>();
            int extraX = 0, extraY = 0;

            if (r.FacingTowards == "left" || r.FacingTowards == "right")
            {
                //create rectangles that will each contain a chair + table space
                outerRects = MakeEnclosingRectangles(outerWidth, outerLength, grid.GetUpperBound(1), grid.GetUpperBound(0));

                //extraX is set to shift the x-pos of tables + chairs, preventing overlap with other furniture
                if (r.FacingTowards == "left")
                {
                    extraX = 10;
                }
                else
                {
                    extraX = -3;
                }
            }
            else
            {
                //create rectangles that will each contain a chair + table space
                outerRects = MakeEnclosingRectangles(outerLength, outerWidth, grid.GetUpperBound(1), grid.GetUpperBound(0));

                //extraY is set to shift the y-pos of tables + chairs, preventing overlap with other furniture
                if (r.FacingTowards == "up")
                {
                    extraY = 10;
                }
                else
                {
                    extraY = -3;
                }
            }

            //create a table and chair for each of the outer rectangles
            foreach (Rectangle outerRect in outerRects)
            {
                tablesAndChairs.Add(MakeTablesAndChairsFromRect(outerRect, r.FacingTowards, 15, 10, gapBetweenTables));
            }

            //add all tables + chairs to room, shifting position to prevent overlap with walls / other furniture
            foreach ((Rectangle, Rectangle) tableAndChairPair in tablesAndChairs)
            {
                r.Tables.Add(r.MakeRectPosRelativeToFloor(tableAndChairPair.Item1, innerGap / 2 + extraX, innerGap / 2 + extraY));
                r.Chairs.Add(r.MakeRectPosRelativeToFloor(tableAndChairPair.Item2, innerGap / 2 + extraX, innerGap / 2 + extraY));
            }
        }
        private void MakeGroupedTables(char[,] grid, Room r, int innerGap, bool mat = false)
        {
            //choose a number of tables to have
            int numOfTables = r.RectWidth * r.RectHeight / 6500;

            List<Rectangle> possibleRects = new List<Rectangle>(), enclosingRects;
            Rectangle currentRect, tempRect, matRect;
            (Rectangle, List<Rectangle>) tableAndChairs;
            int length = 65, width = 55, i = 0, extraX = 0, extraY = 0, encRectSide = (int)Math.Sqrt(6500);

            //break the large grid space into smaller rectangles
            enclosingRects = MakeEnclosingRectangles(encRectSide, encRectSide, grid.GetUpperBound(1), grid.GetUpperBound(0));

            //set values to shift x and y pos of tables by (prevents overlap)
            switch (r.FacingTowards)
            {
                case "left":
                    extraX = 5;
                    break;
                case "right":
                    extraX = -5;
                    break;
                case "up":
                    extraY = 5;
                    break;
                case "down":
                    extraY = -5;
                    break;
            }

            do
            {
                possibleRects.Clear();

                //iterate through each of the smaller rectangles
                foreach (Rectangle encRect in enclosingRects)
                {
                    //try to fit a table space into the rectangle; add if possible
                    tempRect = GetGroupedTableRectFromRegion(grid, length, width, new Rectangle(encRect.X + 5, encRect.Y, encRect.Width, encRect.Height));
                    if (tempRect.Height > 0)
                    {
                        possibleRects.Add(tempRect);
                    }
                    //try to fit the same table space, rotated 90 degrees; add if possible
                    tempRect = GetGroupedTableRectFromRegion(grid, width, length, new Rectangle(encRect.X, encRect.Y + 5, encRect.Width, encRect.Height));
                    if (tempRect.Height > 0)
                    {
                        possibleRects.Add(tempRect);
                    }
                }

                //if tables can be placed in the region...
                if (possibleRects.Count > 0)
                {
                    //choose a random table from the list
                    currentRect = possibleRects[_random.Next(0, possibleRects.Count)];

                    //if creating gym mats, just need to add a simple rectangle to the room
                    if (mat)
                    {
                        matRect = new Rectangle(currentRect.X + 5, currentRect.Y + 5, currentRect.Width - 5, currentRect.Height - 5);
                        r.Tables.Add(r.MakeRectPosRelativeToFloor(matRect, innerGap / 2, innerGap / 2));
                    }
                    //if making grouped tables...
                    else
                    {
                        //make the separate table and chair rectangles from the outer rectangle
                        tableAndChairs = MakeGroupedTable(currentRect, 35, 30, 10, 5, 5);

                        //add the table to the room
                        r.Tables.Add(r.MakeRectPosRelativeToFloor(tableAndChairs.Item1, innerGap / 2 + extraX, innerGap / 2 + extraY));

                        //add each chair to the room
                        foreach (Rectangle chair in tableAndChairs.Item2)
                        {
                            r.Chairs.Add(r.MakeRectPosRelativeToFloor(chair, innerGap / 2 + extraX, innerGap / 2 + extraY));
                        }
                    }

                    //add the table to the grid, so no overlapping tables will be created
                    AddGroupedTableToGrid(ref grid, new Rectangle(currentRect.X, currentRect.Y, currentRect.Width + 5, currentRect.Height + 5));
                    i++;
                }
                //continue until no more space for tables / reached chosen number of tables
            } while (possibleRects.Count > 0 && i < numOfTables);
        }
        private void AddOuterTables(Room r, int addedGap)
        {
            //adds chairs and tables in a U arrangement; also adds extra line in middle if there is enough space

            List<(List<Rectangle>, string)> outerRects = new List<(List<Rectangle>, string)>();
            int totalWidth = 25, chairLength = 10, tableLength = 15;
            List<(Rectangle, Rectangle)> tableAndChairPairs = new List<(Rectangle, Rectangle)>();
            List<Rectangle> tempRectList = new List<Rectangle>();
            Rectangle tempRect = new Rectangle(0, 0, 0, 0), extraLeftRect = new Rectangle(0, 0, 0, 0), extraRightRect = new Rectangle(0, 0, 0, 0);

            if (r.FacingTowards != "left")
            {
                //create a list of rectangles along left wall of room
                tempRectList = MakeEnclosingRectangles(totalWidth, totalWidth, r.RectHeight - 2 * addedGap - 2 * chairLength, totalWidth).Select(i => new Rectangle(addedGap, addedGap + i.Y + chairLength, i.Width, i.Height)).ToList();

                //if the final rectangle doesn't fully reach to bottom, add an extra one to compensate for the space
                if (tempRectList[tempRectList.Count - 1].Bottom < r.RectHeight - addedGap - chairLength)
                {
                    tempRect = tempRectList[tempRectList.Count - 1];
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(addedGap + chairLength, tempRect.Bottom, tableLength, r.RectHeight - addedGap - chairLength - tempRect.Bottom)));
                }

                outerRects.Add((tempRectList, "right"));
            }
            if (r.FacingTowards != "right")
            {
                //create a list of rectangles along right wall of room
                tempRectList = MakeEnclosingRectangles(totalWidth, totalWidth, r.RectHeight - 2 * addedGap - 2 * chairLength, totalWidth).Select(i => new Rectangle(r.RectWidth - addedGap - totalWidth, addedGap + i.Y + chairLength, i.Width, i.Height)).ToList();

                //if the final rectangle doesn't fully reach to bottom, add an extra one to compensate for the space
                if (tempRectList[tempRectList.Count - 1].Bottom < r.RectHeight - addedGap - chairLength)
                {
                    tempRect = tempRectList[tempRectList.Count - 1];
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempRect.X, tempRect.Bottom, tableLength, r.RectHeight - addedGap - chairLength - tempRect.Bottom)));
                }

                outerRects.Add((tempRectList, "left"));
            }
            if (r.FacingTowards != "up")
            {
                //create a list of rectangles along top wall of room
                tempRectList = MakeEnclosingRectangles(totalWidth, totalWidth, totalWidth, r.RectWidth - 2 * addedGap - 2 * chairLength).Select(i => new Rectangle(addedGap + i.X + chairLength, addedGap, i.Width, i.Height)).ToList();

                //if the final rectangle doesn't fully reach to the right of the room, add an extra one to compensate for the space
                if (tempRectList[tempRectList.Count - 1].Right < r.RectWidth - addedGap - chairLength)
                {
                    tempRect = tempRectList[tempRectList.Count - 1];
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempRect.Right, addedGap + chairLength, r.RectWidth - addedGap - tempRect.Right - chairLength, tableLength)));
                }

                outerRects.Add((tempRectList, "down"));
            }
            if (r.FacingTowards != "down")
            {
                //create a list of rectangles along bottom wall of room
                tempRectList = MakeEnclosingRectangles(totalWidth, totalWidth, totalWidth, r.RectWidth - 2 * addedGap - 2 * chairLength).Select(i => new Rectangle(addedGap + i.X + chairLength, r.RectHeight - addedGap - totalWidth, i.Width, i.Height)).ToList();

                //if the final rectangle doesn't fully reach to the right of the room, add an extra one to compensate for the space
                if (tempRectList[tempRectList.Count - 1].Right < r.RectWidth - addedGap - chairLength)
                {
                    tempRect = tempRectList[tempRectList.Count - 1];
                    r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempRect.Right, tempRect.Y, r.RectWidth - addedGap - tempRect.Right - chairLength, tableLength)));
                }

                outerRects.Add((tempRectList, "up"));
            }

            //add tables in the middle of the U if it fits
            AddMiddleTables(r, addedGap, outerRects, totalWidth, tableLength, ref tempRectList, ref tempRect);

            //iterate through each of the outer rectangles; make a table and chair in each of them
            foreach ((List<Rectangle>, string) line in outerRects)
            {
                foreach (Rectangle outerRect in line.Item1)
                {
                    tableAndChairPairs.Add(MakeTablesAndChairsFromRect(outerRect, line.Item2, 15, 10));
                }
            }

            //add each table and chair to the room
            foreach ((Rectangle, Rectangle) pair in tableAndChairPairs)
            {
                r.Tables.Add(r.MakeRectPosRelativeToFloor(pair.Item1));
                r.Chairs.Add(r.MakeRectPosRelativeToFloor(pair.Item2));
            }
        }

        // - - - other functions for creating furniture - - -
        private Rectangle GetEdgeRectFromPoint(Room r, Point chosenPoint, int length, int width, int wallWidth, bool forceLeftRight = false, bool forceUpDown = false)
        {
            //returns a valid rectangle adjacent to an edge based on a given point
            //longest edge will be adjacent to the wall (so furniture doesn't look like it's in a weird position)

            Rectangle enclosingRect = new Rectangle(0, 0, 0, 0);         

            //if the point is on the left wall...
            if (chosenPoint.X == wallWidth)
            {
                if (chosenPoint.Y + length < r.RectHeight && !forceUpDown)
                {
                    //make a rectangle facing left/right if it fits and hasn't been specified against
                    enclosingRect = new Rectangle(wallWidth, chosenPoint.Y, width, length);
                } else
                {
                    //make a rectangle facing up/down otherwise - will only happen if the point is one of the corners of the room
                    enclosingRect = new Rectangle(wallWidth, r.RectHeight - width - wallWidth, length, width);
                }
            }

            //if the point is on the right wall, make an appropriate rectangle facing left/right
            else if (chosenPoint.X == r.RectWidth - wallWidth - 1)
            {
                enclosingRect = new Rectangle(r.RectWidth - width - wallWidth, chosenPoint.Y, width, length);
            }

            //if the point is on the top wall...
            else if (chosenPoint.Y == wallWidth)
            {
                if (chosenPoint.X + length < r.RectWidth)
                {
                    //add a rectangle facing up/down unless it doesn't fit or specified against
                    enclosingRect = new Rectangle(chosenPoint.X, wallWidth, length, width);
                }
                else
                {
                    //otherwise add a rectangle facing left/right
                    enclosingRect = new Rectangle(r.RectWidth - width - wallWidth, wallWidth, width, length);
                }
            }

            //if the point is on the bottom, add appropriate rectangle facing up/down
            else if (chosenPoint.Y == r.RectHeight - wallWidth - 1)
            {
                enclosingRect = new Rectangle(chosenPoint.X, r.RectHeight - wallWidth - width, length, width);
            }

            return enclosingRect;
        }
        private List<Point> FindEdgeRectPositions(int length, int width, Room r, int wallWidth)
        {
            //returns a list of suitable positions against a wall for a rectangle

            List<Point> validPoints = new List<Point>(), clearPoints = r.InnerClearPoints.Select(i => new Point(i.X - r.GrowthTopLeft.X - r.ZoneTopLeft.X, i.Y - r.GrowthTopLeft.Y - r.ZoneTopLeft.Y)).ToList();
            bool validPoint = true;

            //checking against top/bottom walls
            for (int x = wallWidth; x <= r.RectWidth - length - wallWidth; x++)
            {
                //check if the rectangle can be adjacent to the top wall
                //without blocking doors / going out of bounds
                validPoint = true;
                for (int i = x; i < x + length; i++)
                {
                    for (int j = wallWidth; j < width + wallWidth; j++)
                    {
                        if (!WithinBounds(i, j, r.RectWidth, r.RectHeight) || clearPoints.Contains(new Point(i, j)))
                        {
                            validPoint = false;
                        }
                    }
                }
                //add rectangle if valid
                if (validPoint)
                {
                    validPoints.Add(new Point(x, wallWidth));
                }

                //check if the rectangle can be adjacent to the bottom wall
                validPoint = true;
                for(int i = x; i < x + length; i++)
                {
                    for (int j = r.RectHeight - wallWidth - width; j < r.RectHeight - wallWidth; j++)
                    {
                        if (!WithinBounds(i, j, r.RectWidth, r.RectHeight) || clearPoints.Contains(new Point(i, j)))
                        {
                            validPoint = false;
                        }
                    }
                }
                //add rectangle if valid
                if (validPoint)
                {
                    validPoints.Add(new Point(x, r.RectHeight - wallWidth - 1));
                }
            }

            //now check along left / right walls for possible spaces
            for (int y = wallWidth; y <= r.RectHeight - wallWidth - length; y++)
            {
                //check along left wall
                validPoint = true;
                for (int i = wallWidth; i < wallWidth + width; i++)
                {
                    for (int j = y; j < y + length; j++)
                    {
                        if (!WithinBounds(i, j, r.RectWidth, r.RectHeight) || clearPoints.Contains(new Point(i, j)))
                        {
                            validPoint = false;
                        }
                    }
                }
                //add point if valid
                if (validPoint)
                {
                    validPoints.Add(new Point(wallWidth, y));
                }

                //check along right wall
                validPoint = true;
                for (int i = r.RectWidth - width - wallWidth; i < r.RectWidth - wallWidth; i++)
                {
                    for (int j = y; j < y + length; j++)
                    {
                        if (!WithinBounds(i, j, r.RectWidth, r.RectHeight) || clearPoints.Contains(new Point(i, j)))
                        {
                            validPoint = false;
                        }
                    }
                }
                //add point to list if valid
                if (validPoint)
                {
                    validPoints.Add(new Point(r.RectWidth - wallWidth - 1, y));
                }
            }

            return validPoints;
        }
        private Rectangle AddEdgeRect(Room r, int length, int width, int wallWidth = 5)
        {
            //adds a given rectangle on one of the edges of a room

            List<Point> clearPoints = new List<Point>();
            Point p;
            Rectangle rect;

            //find possible positions for the rectangle to go
            List<Point> positions = FindEdgeRectPositions(length, width, r, wallWidth);

            //if there is space for the rectangle...
            if (positions.Count > 0)
            {    
                //choose a random position from the list and get the valid rectangle from that point
                p = positions[_random.Next(0, positions.Count)];
                rect = GetEdgeRectFromPoint(r, p, length, width, wallWidth);

                //add points to avoid based on new rectangle, add to room data
                clearPoints = GetClearPointsFromRect(r.InnerEdgePoints, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
                r.InnerClearPoints.AddRange(clearPoints.Select(i => r.MakePointRelativeToFloor(i)));

                return rect;
            }

            //returns nothing if no space for rectangle
            return new Rectangle(0, 0, 0, 0);
        }
        private (Rectangle, Rectangle) MakeTablesAndChairsFromRect(Rectangle rect, string facingTowards, int tableWidth, int chairLength, int tableGap = 0)
        {      
            //returns separate table and chair rectangles when given an overall space for both to be in
            //needs to be positioned according to where the chair should be facing
            Rectangle table, chair;
            
            switch (facingTowards)
            {
                case "left":
                    table = new Rectangle(rect.X, rect.Y + tableGap, tableWidth, rect.Height - tableGap * 2);
                    chair = new Rectangle(rect.X + tableWidth, rect.Y + tableGap + table.Height / 2 - chairLength / 2, chairLength, chairLength);
                    break;
                case "right":
                    table = new Rectangle(rect.X + rect.Width - tableWidth, rect.Y + tableGap, tableWidth, rect.Height - tableGap * 2);
                    chair = new Rectangle(table.X - chairLength, rect.Y + tableGap + table.Height / 2 - chairLength / 2, chairLength, chairLength);
                    break;
                case "up":
                    table = new Rectangle(rect.X + tableGap, rect.Y, rect.Width - tableGap * 2, tableWidth);
                    chair = new Rectangle(rect.X + tableGap + table.Width / 2 - chairLength / 2, rect.Y + tableWidth, chairLength, chairLength);
                    break;
                case "down":
                    table = new Rectangle(rect.X + tableGap, rect.Y + rect.Height - tableWidth, rect.Width - tableGap * 2, tableWidth);
                    chair = new Rectangle(rect.X + tableGap + table.Width / 2 - chairLength / 2, table.Y - chairLength, chairLength, chairLength);
                    break;
                default:
                    table = new Rectangle(0, 0, 0, 0);
                    chair = new Rectangle(0, 0, 0, 0);
                    break;
            }
            return (table, chair);     
        }
        private List<Rectangle> MakeEnclosingRectangles(int rectLength, int rectWidth, double gridLength, double gridWidth)
        {
            //breaks a large grid space into a list of smaller rectangles

            List<Rectangle> rects = new List<Rectangle>();
            for (int width = 0; width < Math.Floor(gridWidth / rectWidth); width++)
            {
                for (int length = 0; length < Math.Floor(gridLength / rectLength); length++)
                {
                    rects.Add(new Rectangle(rectWidth * width, rectLength * length, rectWidth, rectLength));
                }
            }
            return rects;
        }
        private void AddGroupedTableToGrid(ref char[,] grid, Rectangle r)
        {
            //change character in positions occupied by a rectangle to mark its occupation
            for (int x = r.X; x < r.X + r.Width; x++)
            {
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if (WithinBounds(x, y, grid.GetUpperBound(0), grid.GetUpperBound(1)))
                    grid[x, y] = 'X';
                }
            }
        }
        private Rectangle GetGroupedTableRectFromRegion(char[,] grid, int length, int width, Rectangle block)
        {
            bool valid;

            //iterate through positions in the region given
            for (int x = block.X; x < block.X + block.Width; x++)
            {
                for (int y = block.Y; y < block.Y + block.Height; y++)
                {
                    //for each position, see if a rectangle would fit without overlap w/ other parts of room
                    valid = true;
                    for (int rectX = 0; rectX < width; rectX++)
                    {
                        for (int rectY = 0; rectY < length; rectY++)
                        {
                            if (!WithinBounds(rectX + x, rectY + y, grid.GetUpperBound(0), grid.GetUpperBound(1)) || grid[rectX + x, rectY + y] == 'X')
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid)
                        {
                            break;
                        }
                    }

                    //return this position if a table can be added there
                    if (valid)
                    {
                        return new Rectangle(x, y, width, length);
                    }
                }
            }

            //returns no table if no space available
            return new Rectangle(0, 0, 0, 0);
        }
        private (Rectangle, List<Rectangle>) MakeGroupedTable(Rectangle r, int tableLength, int tableWidth, int chairLength, int chairGap, int outerGap)
        {
            //returns separate table + list of chairs arranged in a group based on rectangle given
            Rectangle table;
            List<Rectangle> chairs = new List<Rectangle>();

            if (r.Height > r.Width)
            {
                //create table
                table = new Rectangle(r.X + outerGap + chairLength, r.Y + outerGap + chairLength, tableLength, tableWidth);
                //one chair on left, one on right
                chairs.Add(new Rectangle(r.X + outerGap, r.Y + 3 * chairGap + chairLength, chairLength, chairLength));
                chairs.Add(new Rectangle(table.X + table.Width, r.Y + 3 * chairGap + chairLength, chairLength, chairLength));
                //two chairs at top
                chairs.Add(new Rectangle(table.X + chairGap, r.Y + outerGap, chairLength, chairLength));
                chairs.Add(new Rectangle(table.X + chairLength + 2 * chairGap, r.Y + outerGap, chairLength, chairLength));
                //two chairs at bottom
                chairs.Add(new Rectangle(table.X + chairGap, table.Y + table.Height, chairLength, chairLength));
                chairs.Add(new Rectangle(table.X + chairLength + 2 * chairGap, table.Y + table.Height, chairLength, chairLength));

            } else
            {
                //create table
                table = new Rectangle(r.X + outerGap + chairLength, r.Y + outerGap + chairLength, tableWidth, tableLength);
                //chair at top, chair at bottom
                chairs.Add(new Rectangle(table.X + chairGap * 2, r.Y + outerGap, chairLength, chairLength));
                chairs.Add(new Rectangle(table.X + chairGap * 2, table.Y + table.Height, chairLength, chairLength));
                //two chairs on the left
                chairs.Add(new Rectangle(r.X + outerGap, table.Y + chairGap, chairLength, chairLength));
                chairs.Add(new Rectangle(r.X + outerGap, table.Y + 2 * chairGap + chairLength, chairLength, chairLength));
                //two chairs on the right
                chairs.Add(new Rectangle(table.X + table.Width, table.Y + chairGap, chairLength, chairLength));
                chairs.Add(new Rectangle(table.X + table.Width, table.Y + 2 * chairGap + chairLength, chairLength, chairLength));
            }

            return (table, chairs);
        }
        private void AddMiddleTables(Room r, int addedGap, List<(List<Rectangle>, string)> outerRects, int totalWidth, int tableLength, ref List<Rectangle> tempRectList, ref Rectangle tempRect)
        {
            //add extra tables in middle of U-shape if possible

            if (r.FacingTowards == "left" || r.FacingTowards == "right")
            {
                //check if there is enough length + width for tables and chairs in the middle
                if (r.RectHeight / 2 - addedGap - 2 * totalWidth >= 15 && r.RectWidth - 2 * (addedGap + totalWidth) >= 30)
                {
                    //create rectangles in the middle
                    tempRectList = MakeEnclosingRectangles(totalWidth, totalWidth, totalWidth, r.RectWidth - 2 * addedGap - 2 * totalWidth);

                    //if the rectangles don't reach to the right of the room, add other tables to compensate
                    //this ensures that the line is connected to the U-shape
                    if (tempRectList[tempRectList.Count - 1].Right < r.RectWidth - 2 * addedGap - 2 * totalWidth)
                    {
                        tempRect = tempRectList[tempRectList.Count - 1];
                        r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempRect.Right + addedGap + totalWidth, r.RectHeight / 2 - tableLength, r.RectWidth - 2 * addedGap - 2 * totalWidth - tempRect.Right, tableLength)));
                        r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(tempRect.Right + addedGap + totalWidth, r.RectHeight / 2, r.RectWidth - 2 * addedGap - 2 * totalWidth - tempRect.Right, tableLength)));
                    }

                    //add rectangles to dictionary
                    outerRects.Add((tempRectList.Select(i => new Rectangle(i.X + addedGap + totalWidth, r.RectHeight / 2 - totalWidth, i.Width, i.Height)).ToList(), "down"));
                    outerRects.Add((tempRectList.Select(i => new Rectangle(i.X + addedGap + totalWidth, r.RectHeight / 2, i.Width, i.Height)).ToList(), "up"));
                }
            }
            else
            {
                //check if there is enough length + width for tables and chairs in the middle
                if (r.RectWidth / 2 - addedGap - 2 * totalWidth >= 15 && r.RectHeight - 2 * (addedGap + totalWidth) >= 30) 
                {  
                    //create rectangles in the middle
                    tempRectList = MakeEnclosingRectangles(totalWidth, totalWidth, r.RectHeight - 2 * addedGap - 2 * totalWidth, totalWidth);

                    //if the rectangles don't reach to the bottom of the room, add other tables to compensate
                    //this ensures that the line is connected to the U-shape
                    if (tempRectList[tempRectList.Count - 1].Bottom < r.RectHeight - 2 * addedGap - 2 * totalWidth)
                    {
                        tempRect = tempRectList[tempRectList.Count - 1];
                        r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(r.RectWidth / 2 - tableLength, tempRect.Bottom + addedGap + totalWidth, tableLength, r.RectHeight - 2 * addedGap - 2 * totalWidth - tempRect.Bottom)));
                        r.Tables.Add(r.MakeRectPosRelativeToFloor(new Rectangle(r.RectWidth / 2, tempRect.Bottom + addedGap + totalWidth, tableLength, r.RectHeight - 2 * addedGap - 2 * totalWidth - tempRect.Bottom)));
                    }

                    //add rectangles to dictionary
                    outerRects.Add((tempRectList.Select(i => new Rectangle(r.RectWidth / 2 - totalWidth, i.Y + addedGap + totalWidth, i.Width, i.Height)).ToList(), "right"));
                    outerRects.Add((tempRectList.Select(i => new Rectangle(r.RectWidth / 2, i.Y + addedGap + totalWidth, i.Width, i.Height)).ToList(), "left"));
                }
            }
        }

        // - - creating doors - -
        private void AddDoors(Floor f, Zone z)
        {
            List<List<Room>> roomConnections = new List<List<Room>>();

            foreach (Room r in z.Rooms)
            {
                bool doneOuterEdgeCluster = false;
                List<List<Point>> tempCluster = new List<List<Point>>();

                //retreiving lists of suitable edge clusters to use
                //"clusters" = consecutive room edge points with no barrier between them
                (List<List<Point>>, List<List<Point>>, List<List<Point>>, List<List<Point>>) outerEdgeClusters = GetRoomPointsAdjacentToOutside(f, z, r);
                List<List<Point>> outerPoints = outerEdgeClusters.Item1;

                //corridor / stair / entrance clusters = suitable clusters that have some points adjacent to corridor / stair / entrance
                List<List<Point>> outerCorridorPoints = outerEdgeClusters.Item2;
                List<List<Point>> outerStairPoints = outerEdgeClusters.Item3;
                List<List<Point>> outerEntrancePoints = outerEdgeClusters.Item4;

                //remove clusters with less than 30 points available, as there wouldn't be enough room for the door to be placed
                outerPoints.RemoveAll(x => x.Count < 30);
                outerCorridorPoints.RemoveAll(x => x.Count < 30);
                outerStairPoints.RemoveAll(x => x.Count < 30);
                outerEntrancePoints.RemoveAll(x => x.Count < 30);

                CreateOuterRoomDoors(z, r, ref doneOuterEdgeCluster, ref tempCluster, outerPoints, outerCorridorPoints, outerStairPoints, outerEntrancePoints);
                CreateAdjacentDoors(f, z, r, outerPoints);

            }
        }
        private void CreateAdjacentDoors(Floor f, Zone z, Room r, List<List<Point>> outerPoints)
        {
            Dictionary<Room, List<Point>> interZoneAdjacencies = new Dictionary<Room, List<Point>>();

            //find clusters that are adjacent to other rooms
            Dictionary<Room, List<Point>> adjacencies = GetAllRoomAdjacencies(r.GrowthTopLeft, r.RectWidth, r.RectHeight, z.GrowthTopLeft, f);

            //automatically add doors for adjacent rooms within zones
            foreach (KeyValuePair<Room, List<Point>> kvp in adjacencies)
            {
                if (kvp.Key.Type == r.Type)
                {
                    AddDoorFromCluster(kvp.Key, kvp.Value, z.GrowthTopLeft, r);
                }
                else
                {
                    interZoneAdjacencies.Add(kvp.Key, kvp.Value);
                }
            }

            //rooms between zones are only connected if there are no other places for a door to go
            //they are also created on the ground floor where large rooms often destroy all corridors
            //this is done to ensure that there is a route to all rooms
            if ((outerPoints.Count == 0 && adjacencies.Count - interZoneAdjacencies.Count == 0) || f.FloorID == 0)
            {        
                foreach (KeyValuePair<Room, List<Point>> kvp in interZoneAdjacencies)
                {
                    AddDoorFromCluster(kvp.Key, kvp.Value, z.GrowthTopLeft, r);
                }
            }
        }
        private void CreateOuterRoomDoors(Zone z, Room r, ref bool doneOuterEdgeCluster, ref List<List<Point>> tempCluster, List<List<Point>> outerPoints, List<List<Point>> outerCorridorPoints, List<List<Point>> outerStairPoints, List<List<Point>> outerEntrancePoints)
        {
            //adding a door connected to the empty spaces between rooms...
            //to ensure accessibility, if there is a cluster with a stair / entrance / corridor, a door must be added there
            //trying to reduce the amount of doors added by finding clusters that have combinations of these items

            //if there are corridor, stair, and entrance clusters...
            if (outerCorridorPoints.Count > 0 && outerStairPoints.Count > 0 && outerEntrancePoints.Count > 0)
            {
                //try use a cluster connected to the stairs, entrance, and corridor
                tempCluster = GetClusterIntersections(outerCorridorPoints, outerStairPoints, outerEntrancePoints);
                if (tempCluster.Count > 0)
                {
                    AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                    doneOuterEdgeCluster = true;
                }

                //if the above isn't possible, find a cluster connected to the corridor and stairs and use an entrance cluster separately
                if (!doneOuterEdgeCluster)
                {
                    tempCluster = GetClusterIntersections(outerCorridorPoints, outerStairPoints);
                    if (tempCluster.Count > 0)
                    {
                        AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                        AddDoorFromCluster(r, outerEntrancePoints[_random.Next(0, outerEntrancePoints.Count)], z.GrowthTopLeft);
                        doneOuterEdgeCluster = true;
                    }
                }

                //if the above isn't possible, find a cluster connected to the corridor and entrance and use a stair cluster separately
                if (!doneOuterEdgeCluster)
                {
                    tempCluster = GetClusterIntersections(outerCorridorPoints, outerEntrancePoints);
                    if (tempCluster.Count > 0)
                    {
                        AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                        AddDoorFromCluster(r, outerStairPoints[_random.Next(0, outerStairPoints.Count)], z.GrowthTopLeft);
                        doneOuterEdgeCluster = true;
                    }
                }

                //if the above isn't possible, find a cluster connected to the entrance and stairs and use a corridor cluster separately
                if (!doneOuterEdgeCluster)
                {
                    tempCluster = GetClusterIntersections(outerStairPoints, outerEntrancePoints);
                    if (tempCluster.Count > 0)
                    {
                        AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                        AddDoorFromCluster(r, outerEntrancePoints[_random.Next(0, outerEntrancePoints.Count)], z.GrowthTopLeft);
                        doneOuterEdgeCluster = true;
                    }
                }

                //if none of this is possible, just use all three clusters separately
                if (!doneOuterEdgeCluster)
                {
                    AddDoorFromCluster(r, outerStairPoints[_random.Next(0, outerStairPoints.Count)], z.GrowthTopLeft);
                    AddDoorFromCluster(r, outerCorridorPoints[_random.Next(0, outerCorridorPoints.Count)], z.GrowthTopLeft);
                    AddDoorFromCluster(r, outerEntrancePoints[_random.Next(0, outerEntrancePoints.Count)], z.GrowthTopLeft);
                    doneOuterEdgeCluster = true;
                }
            }
            //if only corridor and stair clusters are present
            else if (outerCorridorPoints.Count > 0 && outerStairPoints.Count > 0)
            {
                //try to use a cluster with corridor- and stair- adjacent points
                tempCluster = GetClusterIntersections(outerCorridorPoints, outerStairPoints);
                if (tempCluster.Count > 0)
                {
                    AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                }
                //if not, just use separate clusters
                else
                {
                    AddDoorFromCluster(r, outerCorridorPoints[_random.Next(0, outerCorridorPoints.Count)], z.GrowthTopLeft);
                    AddDoorFromCluster(r, outerStairPoints[_random.Next(0, outerStairPoints.Count)], z.GrowthTopLeft);
                }
            }

            //if only corridor and entrance clusters are present
            else if (outerCorridorPoints.Count > 0 && outerEntrancePoints.Count > 0)
            {
                //try to use a cluster with both a corridor and stairs
                tempCluster = GetClusterIntersections(outerCorridorPoints, outerEntrancePoints);
                if (tempCluster.Count > 0)
                {
                    AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                }
                //if not possible, just use clusters separately
                else
                {
                    AddDoorFromCluster(r, outerCorridorPoints[_random.Next(0, outerCorridorPoints.Count)], z.GrowthTopLeft);
                    AddDoorFromCluster(r, outerEntrancePoints[_random.Next(0, outerEntrancePoints.Count)], z.GrowthTopLeft);
                }
            }

            //if only stair and entrance clusters are present
            else if (outerStairPoints.Count > 0 && outerEntrancePoints.Count > 0)
            {
                //try to find a cluster with stairs and the entrance
                tempCluster = GetClusterIntersections(outerStairPoints, outerEntrancePoints);
                if (tempCluster.Count > 0)
                {
                    AddDoorFromCluster(r, tempCluster[_random.Next(0, tempCluster.Count)], z.GrowthTopLeft);
                }
                //if impossible, just use separate clusters
                else
                {
                    AddDoorFromCluster(r, outerStairPoints[_random.Next(0, outerStairPoints.Count)], z.GrowthTopLeft);
                    AddDoorFromCluster(r, outerEntrancePoints[_random.Next(0, outerEntrancePoints.Count)], z.GrowthTopLeft);
                }
            }
            //use cluster with corridor points if it's the only type available
            else if (outerCorridorPoints.Count > 0)
            {
                AddDoorFromCluster(r, outerCorridorPoints[_random.Next(0, outerCorridorPoints.Count)], z.GrowthTopLeft);
            }
            //use cluster with stair points if it's the only type available
            else if (outerStairPoints.Count > 0)
            {
                AddDoorFromCluster(r, outerStairPoints[_random.Next(0, outerStairPoints.Count)], z.GrowthTopLeft);
            }
            //use cluster with entrance points if it's the only type available
            else if (outerEntrancePoints.Count > 0)
            {
                AddDoorFromCluster(r, outerEntrancePoints[_random.Next(0, outerEntrancePoints.Count)], z.GrowthTopLeft);
            }
            //if no special cluster types are available, just use a regular cluster
            else if (outerPoints.Count > 0)
            {
                AddDoorFromCluster(r, outerPoints[_random.Next(0, outerPoints.Count)], z.GrowthTopLeft);
            }
        }
        private List<List<Point>> GetClusterIntersections(List<List<Point>> c1, List<List<Point>> c2, List<List<Point>> c3)
        {
            List<List<Point>> sharedClusters = new List<List<Point>>();

            foreach (List<Point> l3 in c3)
            {
                foreach (List<Point> l2 in c2)
                {
                    foreach (List<Point> l1 in c1)
                    {
                        if (l1 == l2 && l1 == l3)
                        {
                            sharedClusters.Add(l1);
                        }
                    }
                }
            }

            return sharedClusters;
        }
        private List<List<Point>> GetClusterIntersections(List<List<Point>> c1, List<List<Point>> c2)
        {
            List<List<Point>> sharedClusters = new List<List<Point>>();

            foreach (List<Point> l2 in c2)
            {
                foreach (List<Point> l1 in c1)
                {
                    if (l1 == l2)
                    {
                        sharedClusters.Add(l1);
                    }
                }
            }
            return sharedClusters;
        }
        private (List<List<Point>>, List<List<Point>>, List<List<Point>>, List<List<Point>>) GetRoomPointsAdjacentToOutside(Floor f, Zone z, Room r)
        {
            List<Point> usableEdgePoints = new List<Point>(), stairPoints = new List<Point>();
            Point tempPoint, tempStairPoint;

            //getting all types of clusters in a room's edge points
            List<List<Point>> edgePointClusters = GetEdgePointClusters(z, r, 10, f.RoomGrid, (x, y, grid) => grid[x, y] == 'X'), finalClusters = new List<List<Point>>();
            List<List<Point>> corridorClusters = GetEdgePointClusters(z, r, 1, f.GetGrid, (x, y, grid) => grid[x, y] == 'C'), finalCorridorClusters = new List<List<Point>>();
            List<List<Point>> stairClusters = GetEdgePointClusters(z, r, 1, f.GetGrid, (x, y, grid) => grid[x, y] == 'S'), finalStairClusters = new List<List<Point>>();
            List<List<Point>> entranceClusters = GetEdgePointClusters(z, r, 1, f.GetGrid, (x, y, grid) => grid[x, y] == 'E'), finalEntranceClusters = new List<List<Point>>();

            //adding each stair rectangle in the floor as a point in the stairPoints list
            foreach (Rectangle rect in f.CorridorRects)
            {
                stairPoints.Add(rect.Center);
            }

            //iterate through each edgepoint in the room
            foreach (Point p in r.Edgepoints)
            {
                //if the point hasn't already been examined, and the point is part of a suitable cluster...
                if (!usableEdgePoints.Contains(p) && ContainedInClusterLists(p, edgePointClusters))
                {
                    //tempPoint = position of current edpe point relative to entire floor
                    tempPoint = new Point(p.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X, p.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y);

                    //find the closest stair point to the current point
                    tempStairPoint = FindClosestStairPoint(tempPoint, stairPoints);

                    //if there is a path between this point and the stairs, the point (and all clusters containing it) is usable
                    if (FindShortestPath(tempPoint, tempStairPoint, (char[,])f.RoomGrid.Clone(), (p, grid) => grid[p.X, p.Y] == 'X').Count > 0)
                    {
                        //add the point (and related clusters) to the list of suitable clusters
                        AddEdgesInACluster(p, edgePointClusters, ref finalClusters, ref usableEdgePoints);
                    }
                }
            }

            //find which clusters contain parts adjacent to corridor points, stair points, and edge points
            GetSharedClusters(finalClusters, corridorClusters, ref finalCorridorClusters);
            GetSharedClusters(finalClusters, stairClusters, ref finalStairClusters);
            GetSharedClusters(finalClusters, entranceClusters, ref finalEntranceClusters);

            return (finalClusters, finalCorridorClusters, finalStairClusters, finalEntranceClusters);

        }
        private void GetSharedClusters(List<List<Point>> largerClusters, List<List<Point>> smallerClusters, ref List<List<Point>> sharedClusters)
        {
            //check if a smaller cluster is contained in a larger cluster
            //creates a list of the larger clusters that contain one or more of the smaller clusters
            foreach (List<Point> list1 in largerClusters)
            {
                foreach (List<Point> list2 in smallerClusters)
                {
                    if (list2.All(i => list1.Contains(i)))
                    {
                        sharedClusters.Add(list1);
                    }
                }
            }
        }
        private bool ContainedInClusterLists(Point p, List<List<Point>> c)
        {
            foreach (List<Point> l in c)
            {
                if (l.Contains(p))
                {
                    return true;
                }
            }
            return false;
        }
        private List<List<Point>> GetEdgePointClusters(Zone z, Room r, int minWidth, char[,] grid, Func<int, int, char[,], bool> ValidPoint)
        {
            List<List<Point>> edgePointClusters = new List<List<Point>>();
            Point tempPoint;

            //check edgepoints left, right, up, and down to make clusters
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectHeight, "left", minWidth, grid, ref edgePointClusters, ValidPoint);
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X + r.RectWidth - 1, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectHeight, "right", minWidth, grid, ref edgePointClusters, ValidPoint);
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectWidth, "up", minWidth, grid, ref edgePointClusters, ValidPoint);
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y + r.RectHeight - 1, r.RectWidth, "down", minWidth, grid, ref edgePointClusters, ValidPoint);

            for (int i = 0; i < edgePointClusters.Count; i++)
            {
                //remove clusters that have no points in them
                if (edgePointClusters[i].Count == 0)
                {
                    edgePointClusters.RemoveAt(i);
                    i--;
                }
                else
                {
                    //remake the points in each cluster to be relative to the room grid rather than the floor grid
                    for (int j = 0; j < edgePointClusters[i].Count; j++)
                    {
                        tempPoint = edgePointClusters[i][j];
                        edgePointClusters[i][j] = new Point(tempPoint.X - r.GrowthTopLeft.X - z.GrowthTopLeft.X, tempPoint.Y - r.GrowthTopLeft.Y - z.GrowthTopLeft.Y);
                    }
                }
            }

            return edgePointClusters;
        }
        private void CheckAreaAroundEdge(int x, int y, int length, string direction, int extWidth, char[,] grid, ref List<List<Point>> edgePointClusters, Func<int, int, char[,], bool> ValidPoint)
        {
            int tempLength;
            bool broken;
            List<Point> tempCluster = new List<Point>();

            //iterate through edge points at a side of the room, creating clusters

            if (direction == "left" || direction == "right")
            {
                //iterate through points at left / right of room
                for (int tempY = y; tempY < y + length; tempY++)
                {
                    broken = false;

                    //for each point, "extend" to points further left / right to check if they are valid
                    //extWidth = length to extend by
                    for (int i = 1; i <= extWidth; i++)
                    {
                        if (direction == "left")
                        {
                            tempLength = x - i;
                        }
                        else
                        {
                            tempLength = x + i;
                        }

                        //the point can't be part of a cluster if it's out of bounds / invalid
                        if (!(WithinBounds(tempLength, tempY, grid.GetUpperBound(0), grid.GetUpperBound(1)) && ValidPoint(tempLength, tempY, grid)))
                        {
                            broken = true;
                            break;
                        }
                    }

                    //if the current point breaks the cluster, add the previous cluster and reset the list
                    if (broken)
                    {
                        if (tempCluster.Count > 0)
                        {
                            edgePointClusters.Add(tempCluster);
                            tempCluster = new List<Point>();
                        }
                    }
                    //if the current point hasn't broken the cluster, add it to the current cluster list
                    else
                    {
                        tempCluster.Add(new Point(x, tempY));
                    }
                }

            }
            else if (direction == "up" || direction == "down")
            {

                //iterate through points at up / down edges of room
                for (int tempX = x; tempX < x + length; tempX++)
                {
                    broken = false;

                    //for each point, "extend" to points further up / down to check if they are valid
                    //extWidth = length to extend by
                    for (int i = 1; i <= extWidth; i++)
                    {
                        if (direction == "up")
                        {
                            tempLength = y - i;
                        }
                        else
                        {
                            tempLength = y + i;
                        }

                        //the point can't be part of a cluster if it's out of bounds / invalid
                        if (!(WithinBounds(tempX, tempLength, grid.GetUpperBound(0), grid.GetUpperBound(1)) && ValidPoint(tempX, tempLength, grid)))
                        {
                            broken = true;
                            break;
                        }
                    }

                    //if the current point breaks the cluster, add the previous cluster and reset the list
                    if (broken)
                    {
                        if (tempCluster.Count > 0)
                        {
                            edgePointClusters.Add(tempCluster);
                            tempCluster = new List<Point>();
                        }
                    }
                    //if the current point hasn't broken the cluster, add it to the current cluster list
                    else
                    {
                        tempCluster.Add(new Point(tempX, y));
                    }

                }
            }

            //add the final cluster to the list of clusters
            if (tempCluster.Count > 0)
            {
                edgePointClusters.Add(tempCluster);
            }
        }
        private void AddEdgesInACluster(Point p, List<List<Point>> clusters, ref List<List<Point>> edgePoints, ref List<Point> usedEdgePoints)
        {
            //iterate through each cluster list
            foreach (List<Point> l in clusters)
            {
                //if the cluster contains the given point
                if (l.Contains(p))
                {
                    //add the points that aren't already present to a list of used points
                    foreach (Point point in l)
                    {
                        if (!usedEdgePoints.Contains(point))
                        {
                            usedEdgePoints.Add(point);
                        }
                    }

                    //add the cluster to the list of edge point clusters
                    edgePoints.Add(l);
                }
            }
        }
        private Dictionary<Room, List<Point>> GetAllRoomAdjacencies(Point roomTopLeft, int roomWidth, int roomHeight, Point zoneTopLeft, Floor f)
        {
            Dictionary<Room, List<Point>> adjacencies = new Dictionary<Room, List<Point>>();
            List<List<Point>> clusters = new List<List<Point>>();
            List<Point> tempCluster, tempEdgePoints;

            //check left, right, top, bottom edges to see whether they are adjacent to rooms from another zone
            CheckRoomEdgesBetweenZones("left", roomTopLeft.X - 1, roomTopLeft.Y, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);
            CheckRoomEdgesBetweenZones("right", roomTopLeft.X + roomWidth, roomTopLeft.Y, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);
            CheckRoomEdgesBetweenZones("up", roomTopLeft.X, roomTopLeft.Y - 1, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);
            CheckRoomEdgesBetweenZones("down", roomTopLeft.X, roomTopLeft.Y + roomHeight, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);

            clusters.RemoveAll(x => x.Count < 30);

            //finding the specific adjacent room associated with each cluster found
            foreach (List<Point> c in clusters)
            {
                foreach (Zone z in f.Zones)
                {
                    //look through all rooms
                    foreach (Room r in z.Rooms)
                    {
                        //use a list of the room's edgepoints that are relative to the floor rather than the room grid
                        tempEdgePoints = r.Edgepoints.Select(i => { return new Point(i.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X, i.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y); }).ToList();

                        //if the edge points form part of the cluster, this is the adjacent room
                        if (c.All(x => tempEdgePoints.Contains(x)))
                        {
                            //make a cluster relative to this room's grid and add it to the adjacency dictionary
                            tempCluster = c.Select(i => { return new Point(i.X - r.GrowthTopLeft.X - z.GrowthTopLeft.X, i.Y - r.GrowthTopLeft.Y - z.GrowthTopLeft.Y); }).ToList();
                            adjacencies.Add(r, tempCluster);
                        }
                    }
                }
            }

            return adjacencies;
        }
        private void CheckRoomEdgesBetweenZones(string direction, int roomX, int roomY, int roomWidth, int roomHeight, Point zoneTopLeft, char[,] grid, ref List<List<Point>> clusters, Floor f)
        {
            List<Point> cluster = new List<Point>();
            Room r = null;

            //get clusters of room edges that are adjacent to rooms from different zones

            if (direction == "left" || direction == "right")
            {
                //looking along left / right edge
                for (int y = roomY + zoneTopLeft.Y; y < roomY + zoneTopLeft.Y + roomHeight; y++)
                {
                    //if another room is to the left / right
                    if (roomX + zoneTopLeft.X > 0 && roomX + zoneTopLeft.X < grid.GetUpperBound(0) && grid[roomX + zoneTopLeft.X, y] == 'R')
                    {
                        //if the point is adjacent to a new room / different room than point before
                        if (r is null || (!Room.ReferenceEquals(FindRoomFromGrid(roomX + zoneTopLeft.X, y, f), r)))
                        {
                            //add the previous cluster to the cluster list
                            if (cluster.Count > 0)
                            {
                                clusters.Add(cluster);
                            }

                            //reset the current cluster to contain the new point
                            cluster = new List<Point> { new Point(roomX + zoneTopLeft.X, y) };
                            r = FindRoomFromGrid(roomX + zoneTopLeft.X, y, f);
                        }

                        //if adjacent to same room as previous point, add current point to cluster
                        else
                        {
                            cluster.Add(new Point(roomX + zoneTopLeft.X, y));
                        }
                    }

                    //if point isn't adjacent to a room, it's broken the current cluster
                    else
                    {
                        if (cluster.Count > 0)
                        {
                            clusters.Add(cluster);
                            cluster = new List<Point>();
                        }
                    }
                }
            }
            else if (direction == "up" || direction == "down")
            {
                //looking along top / bottom edge
                for (int x = roomX + zoneTopLeft.X; x < roomX + zoneTopLeft.X + roomWidth; x++)
                {
                    //if another room is above / below
                    if (roomY + zoneTopLeft.Y > 0 && roomY + zoneTopLeft.Y < grid.GetUpperBound(1) && grid[x, roomY + zoneTopLeft.Y] == 'R')
                    {
                        //if the point is adjacent to a new room / different room than point before
                        if (r is null || (!Room.ReferenceEquals(FindRoomFromGrid(x, roomY + zoneTopLeft.Y, f), r)))
                        {
                            //add the previous cluster to the cluster list
                            if (cluster.Count > 0)
                            {
                                clusters.Add(cluster);
                            }

                            //reset the current cluster to contain the new point
                            cluster = new List<Point> { new Point(x, roomY + zoneTopLeft.Y) };
                            r = FindRoomFromGrid(x, roomY + zoneTopLeft.Y, f);
                        }
                        //if adjacent to same room as previous point, add current point to cluster
                        else
                        {
                            cluster.Add(new Point(x, roomY + zoneTopLeft.Y));
                        }
                    }
                    //if point isn't adjacent to a room, it's broken the current cluster
                    else
                    {
                        if (cluster.Count > 0)
                        {
                            clusters.Add(cluster);
                            cluster = new List<Point>();
                        }
                    }
                }
            }

            //add final cluster to cluster list
            if (cluster.Count > 0)
            {
                clusters.Add(cluster);
            }

        }
        private Room FindRoomFromGrid(int x, int y, Floor f)
        {
            //identify a room in the floor by one of its edgepoints

            foreach (Zone z in f.Zones)
            {
                foreach (Room r in z.Rooms)
                {
                    foreach (Point p in r.Edgepoints)
                    {
                        if (p.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X == x && p.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y == y)
                        {
                            return r;
                        }
                    }
                }
            }
            return null;
        }
        private void AddDoorFromCluster(Room r, List<Point> cluster, Point zoneTopLeft, Room r2 = null)
        {
            //only make new doors if adjacent to outside
            //or adjacent to other room and this room hasn't had a door between them made for it already
            if (r2 is null || (r2 is not null && !r.AdjacencyDoors.ContainsKey(r2)))
            {

                Rectangle tempRect;
                int doorLength = 20;
                int doorWidth = 5;
                Point startPoint;

                //if cluster is on left / right
                if (cluster[0].X == cluster[1].X)
                {
                    //make suitable rectangle
                    tempRect = new Rectangle(0, 0, doorWidth, doorLength);
                    startPoint = cluster[_random.Next(0, cluster.Count - doorLength)];

                    //set door position based on whether it's on the left of right
                    if (cluster[0].X - 1 >= 0)
                    {
                        tempRect.X = startPoint.X - doorWidth + 1;
                    }
                    else
                    {
                        tempRect.X = startPoint.X;
                    }
                    tempRect.Y = startPoint.Y;
                }
                //if cluster is at top / bottom edge
                else
                {
                    //make suitable rectangle
                    tempRect = new Rectangle(0, 0, doorLength, doorWidth);
                    startPoint = cluster[_random.Next(0, cluster.Count - doorLength)];

                    //set door position based on whether it's above or below
                    if (cluster[0].Y - 1 >= 0)
                    {
                        tempRect.Y = startPoint.Y - doorWidth + 1;
                    }
                    else
                    {
                        tempRect.Y = startPoint.Y;
                    }
                    tempRect.X = startPoint.X;
                }

                //if this door connects two rooms, need to make door for other room too
                if (r2 is not null)
                {
                    Rectangle tempR = new Rectangle(0, 0, 0, 0);
                    int tempLength = 0;

                    //set other room's door position and dimensions based on first door's position and which side the first door is on
                    if (cluster[0].X == cluster[1].X)
                    {
                        tempLength = tempRect.Y + r.ZoneTopLeft.Y + r.GrowthTopLeft.Y - r2.ZoneTopLeft.Y - r2.GrowthTopLeft.Y;
                        if (cluster[0].X == 0)
                        {
                            tempR = new Rectangle(r2.RectWidth - doorWidth, tempLength, doorWidth, doorLength);
                        }
                        else
                        {
                            tempR = new Rectangle(0, tempLength, doorWidth, doorLength);
                        }
                    }
                    else if (cluster[0].Y == cluster[1].Y)
                    {
                        tempLength = tempRect.X + r.ZoneTopLeft.X + r.GrowthTopLeft.X - r2.ZoneTopLeft.X - r2.GrowthTopLeft.X;
                        if (cluster[0].Y == 0)
                        {
                            tempR = new Rectangle(tempLength, r2.RectHeight - doorWidth, doorLength, doorWidth);
                        }
                        else
                        {
                            tempR = new Rectangle(tempLength, 0, doorLength, doorWidth);
                        }
                    }

                    //make points on second room's door relative to floor grid
                    tempR.X += r2.GrowthTopLeft.X + r2.ZoneTopLeft.X;
                    tempR.Y += r2.GrowthTopLeft.Y + r2.ZoneTopLeft.Y;

                    r2.Doors.Add(tempR);
                    r2.AdjacencyDoors.Add(r, tempR);
                }

                //make points on first room's door relative to floor grid
                tempRect.X += r.GrowthTopLeft.X + r.ZoneTopLeft.X;
                tempRect.Y += r.GrowthTopLeft.Y + r.ZoneTopLeft.Y;

                if (r2 is not null)
                {
                    r.AdjacencyDoors.Add(r2, tempRect);
                }
                r.Doors.Add(tempRect);
            }
        }
        private void RemoveDoors(Zone z)
        {
            //add all rooms with door adjacent to corridor to a queue
            Room room;
            Queue<Room> _roomsConnectedToCorridor = new Queue<Room>();
            foreach (Room r in z.Rooms)
            {
                if (r.Doors.Count > r.AdjacencyDoors.Count)
                {
                    _roomsConnectedToCorridor.Enqueue(r);
                }
            }

            while (_roomsConnectedToCorridor.Count > 0)
            {
                room = _roomsConnectedToCorridor.Dequeue();
                //for each room connected to the corridor directly or indirectly, look at rooms connected to it
                foreach (Room r in room.AdjacencyDoors.Keys)
                {
                    //if both rooms are part of the same zone and this information isn't already stored, store the connection between the two rooms
                    if (!room.Connections.Contains(r) && r.Type == room.Type)
                    {
                        r.Connections.Add(room);
                        _roomsConnectedToCorridor.Enqueue(r);
                    }
                }
            }

            //iterate through all rooms in the zone
            foreach (Room r in z.Rooms)
            {
                //if the room is connected to the corridor through one of its own doors, doesn't need connections from other rooms, so remove them
                if (r.Doors.Count > r.AdjacencyDoors.Count && r.Connections.Count > 0)
                {
                    foreach (Room connectedRoom in r.Connections)
                    {
                        Room.RemoveRoomAdjacency(connectedRoom, r);
                    }
                }
                //alternatively, if a room is connected to a corridor indirectly via multiple rooms, remove extra connections to only leave with one
                else if (r.Connections.Count > 1)
                {
                    room = r.Connections[_random.Next(0, r.Connections.Count)];

                    foreach (Room connectedRoom in r.Connections)
                    {
                        if (!Room.ReferenceEquals(connectedRoom, room))
                        {
                            Room.RemoveRoomAdjacency(connectedRoom, r);
                        }
                    }
                }
            }
        }

        // - - creating walls - -
        private void AddWallsToFloor(Floor currentFloor)
        {
            int wallWidth = 5;

            foreach (Zone z in currentFloor.Zones)
            {
                //remove excess connections between room before wall creation
                RemoveDoors(z);

                //store spaces that must be kept clear for each room in the zone
                AddClearEdges(z);

                //then add walls for each room in the zone
                foreach (Room r in z.Rooms)
                {
                    AddWalls(r, wallWidth, z.GrowthTopLeft);
                    UpdateInnerEdgePoints(r, wallWidth);
                    UpdateInnerClearPoints(r, wallWidth, r.ClearPoints, true);
                }
            }
            currentFloor.MadeWalls = true;
        }
        private void AddClearEdges(Zone z)
        {
            List<Point> tempEdgePoints;

            //iterate through each room
            foreach (Room r in z.Rooms)
            {
                //create list of edge points relative to floor grid rather than room grid
                tempEdgePoints = r.Edgepoints.Select(i => { return new Point(i.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X, i.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y); }).ToList();

                //find points that must be kept clear for each door in the room to go
                foreach (Rectangle door in r.Doors)
                {
                    r.ClearPoints.AddRange(GetClearPointsFromRect(tempEdgePoints, door));
                }           
            }
        }
        private List<Point> GetClearPointsFromRect(List<Point> edgePoints, Rectangle door)
        {
           // bool foundEdge = false;
            List<Point> pointsToAdd = new List<Point>();
            Point tempPoint;

            //iterate through points on each side of the door and see if they intersect with the room's edge points
            //if they do, these are the points that must be kept clear

            //check left
            for (int y = door.Y; y < door.Y + door.Height; y++)
            {
                tempPoint = new Point(door.X, y);
                if (edgePoints.Contains(tempPoint))
                {
                    pointsToAdd.Add(tempPoint);
                    //if (pointsToAdd.Count > 1)
                    //{
                    //    foundEdge = true;
                    //}
                }
            }

            //check right
          //  if (!foundEdge)
           // {
                for (int y = door.Y; y < door.Y + door.Height; y++)
                {
                    tempPoint = new Point(door.X + door.Width - 1, y);
                    if (edgePoints.Contains(tempPoint))
                    {
                        pointsToAdd.Add(tempPoint);
                        //if (pointsToAdd.Count > 2)
                        //{
                        //    foundEdge = true;
                        //}
                    }
                }
           // }

            //check up
            //if (!foundEdge)
            //{
                for (int x = door.X; x < door.X + door.Width; x++)
                {
                    tempPoint = new Point(x, door.Y);
                    if (edgePoints.Contains(tempPoint))
                    {
                        pointsToAdd.Add(tempPoint);
                        //if (pointsToAdd.Count > 3)
                        //{
                        //    foundEdge = true;
                        //}
                    }
                }
          //  }

            //check down
            //if (!foundEdge)
            //{
                for (int x = door.X; x < door.X + door.Width; x++)
                {
                    tempPoint = new Point(x, door.Y + door.Height - 1);
                if (edgePoints.Contains(tempPoint))
                {
                    pointsToAdd.Add(tempPoint);
                }
            }
            //}

            return pointsToAdd;
        }
        private void AddWalls(Room r, int width, Point zoneTopLeft)
        {
            List<(int, int)> clusters = new List<(int, int)>();

            //go through each side of a room, adding wall rectangles while keeping certain points clear for doors

            //add walls to left
            clusters = GetWallPositionPairs("left", r, zoneTopLeft, rectX: r.GrowthTopLeft.X);
            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(r.GrowthTopLeft.X + zoneTopLeft.X, c.Item1, width, c.Item2 - c.Item1 + 1));
            }

            //add walls to right
            clusters = GetWallPositionPairs("right", r, zoneTopLeft, rectX: r.GrowthTopLeft.X + r.RectWidth - 1);
            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(r.GrowthTopLeft.X + zoneTopLeft.X + r.RectWidth - width, c.Item1, width, c.Item2 - c.Item1 + 1));
            }

            //add walls above
            clusters = GetWallPositionPairs("up", r, zoneTopLeft, rectY: r.GrowthTopLeft.Y);
            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(c.Item1, r.GrowthTopLeft.Y + zoneTopLeft.Y, c.Item2 - c.Item1 + 1, width));
            }

            //add walls below
            clusters = GetWallPositionPairs("down", r, zoneTopLeft, rectY: r.GrowthTopLeft.Y + r.RectHeight - 1);
            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(c.Item1, zoneTopLeft.Y + r.GrowthTopLeft.Y + r.RectHeight - width, c.Item2 - c.Item1 + 1, width));
            }
        }
        private List<(int, int)> GetWallPositionPairs(string direction, Room r, Point zoneTopLeft, int rectX = 0, int rectY = 0)
        {
            //a position pair contains the start and end x or y position of a continuous wall along an edge
            List<(int, int)> positionPairs = new List<(int, int)>();
            (int, int) positionPair = (-1, -1);

            if (direction == "left" || direction == "right")
            {
                //iterate through points on left / right edge
                for (int y = r.GrowthTopLeft.Y; y < r.GrowthTopLeft.Y + r.RectHeight; y++)
                {
                    //if current point doesn't need to be kept clear and isn't part of an existing pair, start a new pair
                    if (!r.ClearPoints.Contains(new Point(rectX + zoneTopLeft.X, y + zoneTopLeft.Y)) && positionPair.Item1 == -1)
                    {
                        positionPair.Item1 = y + zoneTopLeft.Y;
                    }

                    //if current point needs to be kept clear, the cluster can't be continued
                    else if (r.ClearPoints.Contains(new Point(rectX + zoneTopLeft.X, y + zoneTopLeft.Y)) && positionPair.Item1 != -1)
                    {
                        //add the pair to the list and reset the position pair
                        positionPair.Item2 = y + zoneTopLeft.Y;
                        positionPairs.Add(positionPair);
                        positionPair = (-1, -1);
                    }
                }

                //add the final position pair to the list of pairs
                if (positionPair.Item1 != -1)
                {
                    positionPair.Item2 = r.GrowthTopLeft.Y + r.RectHeight + zoneTopLeft.Y - 1;
                    positionPairs.Add(positionPair);
                }
            }
            else
            {
                //iterate through points on top / bottom edge
                for (int x = r.GrowthTopLeft.X; x < r.GrowthTopLeft.X + r.RectWidth; x++)
                {
                    //if current point doesn't need to be kept clear and isn't part of an existing pair, start a new pair
                    if (!r.ClearPoints.Contains(new Point(x + zoneTopLeft.X, rectY + zoneTopLeft.Y)) && positionPair.Item1 == -1)
                    {
                        positionPair.Item1 = x + zoneTopLeft.X;
                    }

                    //if current point needs to be kept clear, the cluster can't be continued
                    else if (r.ClearPoints.Contains(new Point(x + zoneTopLeft.X, rectY + zoneTopLeft.Y)) && positionPair.Item1 != -1)
                    {
                        //add the pair to the list and reset the position pair
                        positionPair.Item2 = x + zoneTopLeft.X;
                        positionPairs.Add(positionPair);
                        positionPair = (-1, -1);
                    }
                }

                //add the final position pair to the list of pairs
                if (positionPair.Item1 != -1)
                {
                    positionPair.Item2 = r.GrowthTopLeft.X + zoneTopLeft.X + r.RectWidth - 1;
                    positionPairs.Add(positionPair);
                }
            }
            return positionPairs;
        }
        private void UpdateInnerEdgePoints(Room r, int wallWidth)
        {
            r.InnerEdgePoints = new List<Point>();

            for (int i = 0; i < r.RectWidth; i++)
            {
                r.InnerEdgePoints.Add(new Point(i, wallWidth));
                r.InnerEdgePoints.Add(new Point(i, r.RectHeight - 1 - wallWidth));
            }
            for (int i = 0; i < r.RectHeight; i++)
            {
                r.InnerEdgePoints.Add(new Point(wallWidth, i));
                r.InnerEdgePoints.Add(new Point(r.RectWidth - 1 - wallWidth, i));
            }
        }
        private void UpdateInnerClearPoints(Room r, int wallWidth, List<Point> clearPoints, bool reset = false)
        {
            if (reset)
            {
                r.InnerClearPoints = new List<Point>();
            }
           
            foreach (Point p in clearPoints)
            {
                if (p.X - r.GrowthTopLeft.X - r.ZoneTopLeft.X == 0)
                {
                    r.InnerClearPoints.Add(new Point(p.X + wallWidth, p.Y));
                }
                else if (p.X - r.GrowthTopLeft.X - r.ZoneTopLeft.X == r.RectWidth - 1)
                {
                    r.InnerClearPoints.Add(new Point(p.X - wallWidth, p.Y));
                }
                else if (p.Y - r.GrowthTopLeft.Y - r.ZoneTopLeft.Y == 0)
                {
                    r.InnerClearPoints.Add(new Point(p.X, p.Y + wallWidth));
                }
                else if (p.Y - r.GrowthTopLeft.Y - r.ZoneTopLeft.Y == r.RectHeight - 1)
                {
                    r.InnerClearPoints.Add(new Point(p.X, p.Y - wallWidth));
                }
                else
                {

                }
            }
        }

        // - update message queue -
        private void UpdateMessageQueue(string s)
        {
            if (s != "")
            {
                //remove oldest message if the queue has grown too large
                if (_displayMessages.Count > 5)
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
            else if (_gameState == "create floors" || _gameState == "create stairs" || _gameState == "create corridors" || _gameState == "grow rectangular zones" || _gameState == "create rooms" || _gameState == "create furniture")
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
            //draws message at bottom left corner of screen, most recent closest to the top
            for (int i = 0; i < _displayMessages.Count; i++)
            {
                _spriteBatch.DrawString(_consolas, _displayMessages.ToList()[i], new Vector2(10, _screenHeight - 100 - (20 * (i + 1))), Color.White);
            }
        }
    }
}