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
            RoomType.SetTypeColours();
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

            if (_floorSize < 100000)
            {
                _growthSpeed = 5;
            }
            else if (_floorSize < 150000)
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
            // _currentZoneIndex >= _allFloors[_currentFloorIndex].Zones.Count
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
                    //and if the zone is bigger then the count...only happens when stuff has been deleted
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
                if (((!left && !right && z.RectWidth < 75) || (!up && !down && z.RectHeight < 75)) && !z.FirstGrown)
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
                        UpdateStairsOrCorridorsToZone(f.Entrance, z);
                    }
                    foreach (Rectangle r in f.StairPoints)
                    {
                        UpdateStairsOrCorridorsToZone(r, z);
                    }

                    //update all edgepoints of the zone
                    z.FindAllEdgePoints((char)('0' | z.ID));
                    z.UpdateArea();
                }
            }
        }
        private void UpdateStairsOrCorridorsToZone(Rectangle r, Zone z)
        {
            for (int x = r.X; x < r.X + r.Width; x++)
            {
                for (int y = r.Y; y < r.Y + r.Height; y++)
                {
                    if (WithinBounds(x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y, z.RectWidth - 1, z.RectHeight - 1))
                    {
                        z.GetGrid[x - z.GrowthTopLeft.X, y - z.GrowthTopLeft.Y] = 'E';
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
                        else
                        {
                            _currentZoneIndex++;
                            _currentRoomIndex = 0;
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
                                currentZone.Rooms[_currentRoomIndex].Grown = true;
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
                if ((!left && !right && r.RectWidth < 50) || (!up && !down && r.RectHeight < 50))
                {
                    z.Rooms.Remove(r);
                    z.BadGrowthPoints.Add(r.GrowthPoint);
                }

                //if no more growth available (or required area reached on first growth), finish growth
                else if ((!left && !right && !up && !down) || (r.RectWidth * r.RectHeight >= (r.IdealSize)))
                {

                    //remove the room if it is too long
                    if (r.RectHeight >= r.RectWidth * 3.5 || r.RectWidth >= r.RectHeight * 3.5)
                    {
                        z.AddRectToGrid(new Rectangle(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight), (char)('0' | z.ID), true, addRect: false);
                        z.Rooms.Remove(r);
                        z.BadGrowthPoints.Add(new Point(r.GrowthPoint.X, r.GrowthPoint.Y));                
                    }

                    else {
                        SetRoomRect(r, z);
                        return true;
                    }
                   
                }
            }
            return false;
        
        }
        private void SetRoomGrowthPoint(Room r, Zone z, Rectangle entrance)
        {
            r.WeightedGrid = MakeRoomWeightedGrid(r, z, entrance);
            r.GrowthPoint = ChooseGrowthPoint(r.WeightedGrid);

            if (r.WeightedGrid[r.GrowthPoint.X, r.GrowthPoint.Y] < 0)
            {
                z.Rooms.Remove(r);
                //_currentRoomIndex--;
            } else
            {
                r.RectWidth = 1;
                r.RectHeight = 1;
                r.FloorRectangles.Add(new Rectangle(0, 0, 0, 0));
                r.GrowthTopLeft = r.GrowthPoint;
                r.GrowthFloorPoint = new Point(r.GrowthPoint.X + z.GrowthTopLeft.X, r.GrowthPoint.Y + z.GrowthTopLeft.Y);
            }

        }
        private int[,] MakeRoomWeightedGrid(Room r, Zone z, Rectangle entrance) 
        {
            int minArea = 300; //TODO: tweak

            //max of those two...absolute min is the min area
            int estArea = Math.Max((int)(z.Area / z.Rooms.Count), minArea);
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

            //encourage a distance away from the edge...
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
        private void SetRoomRect(Room r, Zone z)
        {
            char tempID;

            //updates the zone's grid with new dimensions
            r.ResetGrid(r.RectWidth, r.RectHeight, 'R');

            //set all edgepoints of the zone
            r.FindAllEdgePoints('R');

            //sets an ID for the room (can't have the room ID be the same as the zone ID)
            if (z.ID == r.ID)
            {
                //TODO: this will bite you in the ass later, a problem for future me
                tempID = 'U';
            }
            else
            {
                tempID = (char)('0' | r.ID);
            }

            //mark the zone's grid with the completed room shape
            z.AddRectToGrid(new Rectangle(r.GrowthTopLeft.X, r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight), tempID, false, (char)('0' | z.ID), false);
        }

        // - create room furniture - TODO
        private string UpdateFurnitureCreation()
        {
            if (_currentFloorIndex == -1)
            {
                //initialise furniture creation
                _currentFloorIndex = 0;
                _currentZoneIndex = 0;
                _currentRoomIndex = 0;
                _previousUpdateTime = DateTime.Now;

                foreach (Floor f in _allFloors)
                {
                    f.SetRoomGrid();
                    foreach (Zone z in f.Zones)
                    {
                        foreach (Room r in z.Rooms)
                        {
                            f.AddToRoomGrid(new Rectangle(z.GrowthTopLeft.X + r.GrowthTopLeft.X, z.GrowthTopLeft.Y + r.GrowthTopLeft.Y, r.RectWidth, r.RectHeight));
                            r.Adjacencies = new Dictionary<Room, List<Point>>();
                        }
                    }
                }

                return "> initialising furniture creation";
            }
            else if (_currentFloorIndex < _allFloors.Count)
            {
                Floor currentFloor = _allFloors[_currentFloorIndex];

                if (_currentZoneIndex == currentFloor.Zones.Count && !currentFloor.MadeWalls)
                {
             
                    foreach (Zone z in currentFloor.Zones)
                    {
                        AddWalls(z, currentFloor.GetGrid.GetUpperBound(0), currentFloor.GetGrid.GetUpperBound(1));
                    }
                    currentFloor.MadeWalls = true;
                    _previousUpdateTime = DateTime.Now;
                } 
                else if (_currentZoneIndex == currentFloor.Zones.Count && currentFloor.MadeWalls && DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange))
                {
                    _currentFloorIndex++;
                    _currentZoneIndex = 0;
                    _currentRoomIndex = 0;
                    return $"> finished furniture creation: floor {_currentFloorIndex - 1}";
                }
                else if (_currentZoneIndex < currentFloor.Zones.Count){

                    Zone currentZone = currentFloor.Zones[_currentZoneIndex];

                    if (_currentRoomIndex == currentZone.Rooms.Count)
                    {
                        AddDoors(currentFloor, currentZone);
                       // RemoveDoors(currentZone);
                        _currentZoneIndex++;
                        _currentRoomIndex = 0;
                        return $"> created doors: floor {_currentFloorIndex}, zone {_currentZoneIndex - 1}";
                    } 
                    //TODO: tweak the time
                    else if (DateTime.Now >= _previousUpdateTime.AddMilliseconds(_timeBetweenDisplayChange)) {
                        Room currentRoom = currentZone.Rooms[_currentRoomIndex];
                        //do the things...
                        RemoveCorridorsFromRoom(currentFloor, currentRoom, (char)('0' | currentZone.ID));
                        _previousUpdateTime = DateTime.Now;
                        _currentRoomIndex++;
                    }

                }

                if (_currentFloorIndex < _allFloors.Count)
                {
                    return $"> created furniture: floor {_currentFloorIndex}, zone {_currentZoneIndex}, room {_currentRoomIndex}";
                }
                return "> finished creating furniture";
            }
            else
            {
                return "> finished creating furniture";
            }
        }

        //for each room
        // - remove corridors if present (done)
        // - add door adjacent to corridor / not building edge <---
        // - - careful with room adjacencies
        // - add furniture (depends on room type)
        // - add walls

        //TODO: 
        // - force the rooms to add a door to all "corridor" cluster
        // - - just fix up CheckAreaAroundEdge to check for a 'C' in the door? but what if zone doesn't have Cs?? just re-add them before this happens
        // - - then use the actual clusters made by that sub and see if they have a C in em
        // - - if they do, call sub to add doors

        /*
         * check for corridor zones
         * check for outer edge points (as an ext of point 1)
         * add corridor zones if available
         * if not, add another door to outside
         * do the rest as usual
         */

        // - fix up the RemoveRooms() sub

        /*
         * starts with rooms connected to outside
         * then "reach over" to others connected (if not already)
         * if a room is connected to the outside and has a connection - remove all connections
         * or if not connected to outside multiple connections - remove all but one
         */

        // - fix up presentation of walls

        private void RemoveCorridorsFromRoom(Floor f, Room r, char zoneID)
        {
            //if corridor present...
            for (int x = 0; x < r.RectWidth; x++)
            {
                for (int y = 0; y < r.RectHeight; y++)
                {
                    //how to actually locate?
                    //C will not be present in zone grid
                    if (f.GetGrid[r.FloorRectangles[0].X + x, r.FloorRectangles[0].Y + y] == 'C')
                    {
                        //TODO: remove from floor grid, remove from floor rects
                        //using a Floor sub?
                        f.RemoveCorridorPoint(r.FloorRectangles[0].X + x, r.FloorRectangles[0].Y + y, zoneID);
                    }
                }

            }

        }

        private void AddDoors(Floor f, Zone z)
        {
            List<List<Room>> roomConnections = new List<List<Room>>();
            //for each room - add door if adjacent to a corridor and add door if adjacent to another room
            //don't add if already done...
            //store these in a list of lists
            //so do it by zone; store graph in zone

            foreach (Room r in z.Rooms)
            {
                
                //check if room adjacent to a valid outside place <-- TODO: check
                // - - - repeat this for all of the points along the edge to get ones that can be used for the door
                //done!
                //might want the clusters too...just think bout that later

                //okay how am i actually meant to use this
                //think i need the clusters that are good points to use
                //and then....
                //choose a door from there
                List<List<Point>> outerEdgeClusters = GetRoomPointsAdjacentToOutside(f, z, r);

                outerEdgeClusters.RemoveAll(x => x.Count < 30);
                //now check if adjacent to another room in the zone <--- TODO
                //looiofhisdfnakc depends HOW MUCH adjacency too
                //check if edgepoints share with another room in the zone
                //then how many fit
                //and add cluster if they fit

                GetRoomAdjacencies(f, z, r);

                //can add a door for these quire easily too

                //now just add them
                if (outerEdgeClusters.Count > 0)
                {
                    AddDoorFromCluster(r, outerEdgeClusters[_random.Next(0, outerEdgeClusters.Count)], z.GrowthTopLeft);
                }

                foreach (KeyValuePair<Room, List<Point>> kvp in r.Adjacencies)
                {
                    if (WithinBounds(kvp.Value[0].X, kvp.Value[0].Y, r.RectWidth, r.RectHeight))
                    {
                        AddDoorFromCluster(r, kvp.Value, z.GrowthTopLeft, kvp.Key);
                    }

                }

                if ((outerEdgeClusters.Count == 0 && r.Adjacencies.Count == 0) || f.FloorID == 0)
                {
                    //how to connect between zones? TODO
                    //use the floor grid
                    //find adjacencies left, right, up, down
                    //then find the zone based on the coordinates
                    // - loop through and see which contains them
                    //then add a door to there, and to the room
                    //should still work as a Door rect and in the dictionary
                    Dictionary<Room, List<Point>> adjacencies = GetRoomAdjacenciesBetweenZones(r.GrowthTopLeft, r.RectWidth, r.RectHeight, z.GrowthTopLeft, f);


                    foreach (KeyValuePair<Room, List<Point>> kvp in adjacencies)
                    {
                        AddDoorFromCluster(kvp.Key, kvp.Value, z.GrowthTopLeft, r);
                    }

                }
             

              

            }
        }

        private List<List<Point>> GetRoomPointsAdjacentToOutside(Floor f, Zone z, Room r)
        {
            List<Point> usableEdgePoints = new List<Point>(), stairPoints = new List<Point>();
            Point end = new Point(f.StairPoints[0].X, f.StairPoints[0].Y), tempPoint, tempStairPoint;
            List<List<Point>> edgePointClusters = GetEdgePointClusters(f, z, r), finalClusters = new List<List<Point>> ();

            foreach (Rectangle rect in f.CorridorRects)
            {
                stairPoints.Add(rect.Center);
            }

            //TODO: make this faster by 'grouping' consecutive edgepoints
            //on each side...
            //broken if there is an obstruction adjacent

            foreach (Point p in r.Edgepoints)
            {
                if (!usableEdgePoints.Contains(p) && ContainedInClusterLists(p, edgePointClusters)) //check if in cluster first
                {
                    tempPoint = new Point(p.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X, p.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y);
                    tempStairPoint = FindClosestStairPoint(tempPoint, stairPoints);
                    //how to quickly check adjacency? Point, char[]...
                    //mark rooms onto floor?? with an R
                    //then make sure points are an X..

                    //STEP OVER THIS
                    if (FindShortestPath(tempPoint, tempStairPoint, (char[,])f.RoomGrid.Clone(), (p, grid) => grid[p.X, p.Y] == 'X').Count > 0)
                    {
                        //TODO: make to add every other in a cluster
                        AddEdgesInACluster(p, edgePointClusters, ref finalClusters, ref usableEdgePoints);
                    }
                }
            }
            return finalClusters;
        }
        private List<List<Point>> GetEdgePointClusters(Floor f, Zone z, Room r)
        {
            List<List<Point>> edgePointClusters = new List<List<Point>>();
            //TODO: tweak
            int minWidth = 10;
            Point tempPoint;

            //check left, right, up, down

            //left:
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectHeight, "left", minWidth, f.RoomGrid, ref edgePointClusters);

            //right:
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X + r.RectWidth - 1, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectHeight, "right", minWidth, f.RoomGrid, ref edgePointClusters);

            //up:
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y, r.RectWidth, "up", minWidth, f.RoomGrid, ref edgePointClusters);

            //down:
            CheckAreaAroundEdge(r.GrowthTopLeft.X + z.GrowthTopLeft.X, r.GrowthTopLeft.Y + z.GrowthTopLeft.Y + r.RectHeight - 1, r.RectWidth, "down", minWidth, f.RoomGrid, ref edgePointClusters);

            for (int i = 0; i < edgePointClusters.Count; i++)
            {
                if (edgePointClusters[i].Count == 0)
                {
                    edgePointClusters.RemoveAt(i);
                    i--;
                } else
                {
                    for (int j = 0; j < edgePointClusters[i].Count; j++)
                    {
                        tempPoint = edgePointClusters[i][j];
                        edgePointClusters[i][j] = new Point(tempPoint.X - r.GrowthTopLeft.X - z.GrowthTopLeft.X, tempPoint.Y - r.GrowthTopLeft.Y - z.GrowthTopLeft.Y);
                    }
                }
            }

            char[,] tempG = new char[f.RoomGrid.GetUpperBound(0) - 251, f.RoomGrid.GetUpperBound(1) - 399];

            return edgePointClusters;
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
        private void CheckAreaAroundEdge(int x, int y, int length, string direction, int minWidth, char[,] grid, ref List<List<Point>> edgePointClusters  )
        {
            int tempLength = 0;
            bool broken = false;
            List<Point> tempCluster = new List<Point>();

            if (direction == "left" || direction == "right")
            {
                for (int tempY = y; tempY < y + length; tempY++)
                {
                    broken = false;
                    for (int i = 1; i <= minWidth; i++)
                    {
                        if (direction == "left")
                        {
                            tempLength = x - i;
                        }
                        else
                        {
                            tempLength = x + i;
                        }

                        if (!(WithinBounds(tempLength, tempY, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[tempLength, tempY] == 'X'))
                        {
                            //this point can't be added
                            broken = true;
                            break;
                        }
                    }

                    if (broken)
                    {
                        if (tempCluster.Count > 0)
                        {
                            edgePointClusters.Add(tempCluster);
                            tempCluster = new List<Point>();
                        }
                    } else
                    {
                        tempCluster.Add(new Point(x, tempY));
                    }

                }

            } else if (direction == "up" || direction == "down")
            {
                for (int tempX = x; tempX < x + length; tempX++)
                {
                    broken = false;
                    for (int i = 1; i <= minWidth; i++)
                    {
                        if (direction == "up")
                        {
                            tempLength = y - i;
                        }
                        else
                        {
                            tempLength = y + i;
                        }

                        if (!(WithinBounds(tempX, tempLength, grid.GetUpperBound(0), grid.GetUpperBound(1)) && grid[tempX, tempLength] == 'X'))
                        {
                            //this point can't be added
                            broken = true;
                            break;
                        }
                    }

                    if (broken)
                    {
                        if (tempCluster.Count > 0)
                        {
                            edgePointClusters.Add(tempCluster);
                            tempCluster = new List<Point>();
                        }
                    }
                    else
                    {
                        tempCluster.Add(new Point(tempX, y));
                    }

                }
            }
            if (tempCluster.Count > 0)
            {
                edgePointClusters.Add(tempCluster);
            }
        }
        private bool AddEdgesInACluster(Point p, List<List<Point>> clusters, ref List<List<Point>> edgePoints, ref List<Point> usableEdgePoints)
        {
            bool found = false;
            foreach (List<Point> l in clusters)
            {
                if (l.Contains(p))
                {
                    found = true;
                    foreach (Point point in l)
                    {
                        if (!usableEdgePoints.Contains(point))
                        {
                            usableEdgePoints.Add(point);
                        }
                    }

                    //now add that to the clusters..
                    edgePoints.Add(l);

                }
            }
            return found;
        }
        private void GetRoomAdjacencies(Floor f, Zone z, Room r)
        {
            //check edges
            //check adj
            //add if not already added
            //gotta make sure its the same other room too...store the char value
            Room tempRoom;
            Dictionary<char, List<Point>> adjLists = new Dictionary<char, List<Point>>();

            //left
            CheckAdjacencyBySide("left", r.GrowthTopLeft.X - 1, f, z, r, ref adjLists);
            //right
            CheckAdjacencyBySide("right", r.GrowthTopLeft.X + r.RectWidth, f, z, r, ref adjLists);
            //up
            CheckAdjacencyBySide("up", r.GrowthTopLeft.Y - 1, f, z, r, ref adjLists);
            //down
            CheckAdjacencyBySide("down", r.GrowthTopLeft.Y + r.RectHeight, f, z, r, ref adjLists);


            foreach (KeyValuePair<char, List<Point>> kvp in adjLists) {
                if (kvp.Value.Count >= 30)
                {
                    //now actually store it with the rooms
                    //means getting the room from the char
                    tempRoom = GetRoomByChar(z.Rooms, kvp.Key, z.ID);
                    if (tempRoom is not null && !r.Adjacencies.ContainsKey(tempRoom)) //this shouldnt ever happen..
                    {

                        //r.Adjacencies.Add(tempRoom, MakeClusterForOtherRoom(r, kvp.Value));
                        ////gonna need a new list for the new room too...along the edge
                        //tempRoom.Adjacencies.Add(r, kvp.Value);

                        //TODO: sometimes the cluster ends up weird 
                        //like forgetting to subtract the room'a growth point
                       
                        if (kvp.Value[0].X != -1 && kvp.Value[0].X != r.RectWidth && kvp.Value[0].Y != -1 && kvp.Value[0].Y != r.RectHeight)
                        {

                        }

                        if (MakeClusterForFirstRoom(r, kvp.Value).Count > 30)
                        {
                            //glossing over that weird error i get sometimes
                            r.Adjacencies.Add(tempRoom, MakeClusterForFirstRoom(r, kvp.Value));
                            tempRoom.Adjacencies.Add(r, MakeClusterForSecondRoom(tempRoom, r, kvp.Value));
                        }

                        //making cluster for room 1 - shift it 
                    }
                }
            }

        }

        private void CheckAdjacencyBySide(string side, int start, Floor f, Zone z, Room r, ref Dictionary<char, List<Point>> adjLists)
        {

            List<Point> adjList = new List<Point>();          
            char c = '-';
            //left
           

            if (side == "left" || side == "right")
            {
                for (int y = r.GrowthTopLeft.Y; y < r.GrowthTopLeft.Y + r.RectHeight; y++)
                {

                    if (WithinBounds(start, y, z.GetGrid.GetUpperBound(0), z.GetGrid.GetUpperBound(1)) && z.GetGrid[start, y] != (char)('0' | z.ID))
                    {
                        //now gotta check if the different num still belongs to that zone
                        //floor will only have different num if different zone
                        if (f.GetGrid[z.GrowthTopLeft.X + start, z.GrowthTopLeft.Y + y] == (char)('0' | z.ID))
                        {
                            if (z.GetGrid[start, y] == c)
                            {
                                //add it to the list
                                adjList.Add(new Point(start, y));
                            }
                            else
                            {
                                //TODO: check this behaviour, i'm worried its by ref here
                                if (adjList.Count > 0)
                                {
                                    adjLists.Add(c, adjList.ToList());
                                }
                                c = z.GetGrid[start, y];
                                adjList = new List<Point> { new Point(start, y) };
                                //add a new entry in the dictionary

                            }
                        }
                    }

                }
            }
            else if (side == "up" || side == "down")
            {
                for (int x = r.GrowthTopLeft.X; x < r.GrowthTopLeft.X + r.RectWidth; x++)
                {

                    if (WithinBounds(x, start, z.GetGrid.GetUpperBound(0), z.GetGrid.GetUpperBound(1)) && z.GetGrid[x, start] != (char)('0' | z.ID))
                    {
                        //now gotta check if the different num still belongs to that zone
                        //floor will only have different num if different zone
                        if (f.GetGrid[z.GrowthTopLeft.X + x, z.GrowthTopLeft.Y + start] == (char)('0' | z.ID))
                        {
                            if (z.GetGrid[x, start] == c)
                            {
                                //add it to the list
                                adjList.Add(new Point(x, start));
                            }
                            else
                            {
                                //TODO: check this behaviour, i'm worried its by ref here
                                if (adjList.Count > 0)
                                {
                                    adjLists.Add(c, adjList.ToList());
                                }
                                c = z.GetGrid[x, start];
                                adjList = new List<Point> { new Point(x, start) };
                                //add a new entry in the dictionary

                            }
                        }
                    }

                }
            }

            if (adjList.Count > 2)
            {
                for (int i = 0; i < adjList.Count; i++)
                {
                    adjList[i] = new Point(adjList[i].X - r.GrowthTopLeft.X, adjList[i].Y - r.GrowthTopLeft.Y);
                }
                adjLists.Add(c, adjList.ToList());
            }
          
        }

        private Room GetRoomByChar(List<Room> rooms, char c, int zoneID)
        {
            foreach (Room r in rooms)
            {
                if ((char)('0' | r.ID) == c || (r.ID == zoneID && c == 'U'))
                {
                    return r;
                } 
            }
            return null;
        }
        private List<Point> MakeClusterForFirstRoom(Room r1, List<Point> cluster)
        {
            List<Point> cluster2 = new List<Point>();

            if (cluster[0].X == cluster[1].X)
            {
                if (cluster[0].X == -1)
                {
                    //left of the original room...
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(p.X + 1, p.Y));
                    }
                }
                else if (cluster[0].X == r1.RectWidth)
                {
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(p.X - 1, p.Y));
                    }
                }
                else
                {

                }

                //if (!WithinBounds(cluster[0].X, cluster[0].Y - 1, r1.GetGrid.GetUpperBound(0), r1.GetGrid.GetUpperBound(1)))
                //{
                //    //left


                //} else
                //{
                //    //right
                //    foreach (Point p in cluster)
                //    {
                //        cluster2.Add(new Point(p.X, p.Y + 1));
                //    }
                //}
            }
            else
            {
                //if (!WithinBounds(cluster[0].X - 1, cluster[0].Y, r1.GetGrid.GetUpperBound(0), r1.GetGrid.GetUpperBound(1)))
                //{
                //    foreach (Point p in cluster)
                //    {
                //        cluster2.Add(new Point(p.X - 1, p.Y));
                //    }
                //} else
                //{
                //    foreach (Point p in cluster)
                //    {
                //        cluster2.Add(new Point(p.X + 1, p.Y));
                //    }
                //}
                if (cluster[0].Y == -1)
                {
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(p.X, p.Y + 1));
                    }
                }
                else if (cluster[0].Y == r1.RectHeight)
                {
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(p.X, p.Y - 1));
                    }
                }
                else
                {

                }
            }

            return cluster2;
        }
        private List<Point> MakeClusterForSecondRoom(Room r1, Room r2, List<Point> cluster)
        {
            List<Point> cluster2 = new List<Point>();

            if (cluster[0].X == cluster[1].X)
            {
                if (cluster[0].X == -1)
                {
                    //left of the original room, so will be right of the new room
                    //e.g., -1 and 67 to 70
                    //will need room width and ...

                    //how to get the height relative to second room?
                    //add the og room top left
                    //then subtract the new room top left y
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(r1.RectWidth - 1, p.Y + r2.GrowthTopLeft.Y - r1.GrowthTopLeft.Y));
                    }
                }
                else if (cluster[0].X == r2.RectWidth)
                {
                    //so the left
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(0, p.Y + r2.GrowthTopLeft.Y - r1.GrowthTopLeft.Y));
                    }
                }
            }
            else
            {
                if (cluster[0].Y == -1)
                {
                    //left of the original room, so will be right of the new room
                    //e.g., -1 and 67 to 70
                    //will need room width and ...

                    //how to get the height relative to second room?
                    //add the og room top left
                    //then subtract the new room top left y
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(p.X + r2.GrowthTopLeft.X - r1.GrowthTopLeft.X, r1.RectHeight - 1 ));
                    }
                }
                else if (cluster[0].Y == r2.RectHeight)
                {
                    //so the left
                    foreach (Point p in cluster)
                    {
                        cluster2.Add(new Point(p.X + r2.GrowthTopLeft.X - r1.GrowthTopLeft.X, 0));
                    }
                }
            }

            return cluster2;
        }

        private Dictionary<Room, List<Point>> GetRoomAdjacenciesBetweenZones(Point roomTopLeft, int roomWidth, int roomHeight, Point zoneTopLeft, Floor f)
        {
            //check left, right, up, down for R
            //add cluster if so 
            Dictionary<Room, List<Point>> adjacencies = new Dictionary<Room, List<Point>>();
            List<List<Point>> clusters = new List<List<Point>>();
            List<Point> tempCluster, tempEdgePoints;

            CheckRoomEdgesBetweenZones("left", roomTopLeft.X - 1, roomTopLeft.Y, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);
            CheckRoomEdgesBetweenZones("right", roomTopLeft.X + roomWidth, roomTopLeft.Y, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);
            CheckRoomEdgesBetweenZones("up", roomTopLeft.X, roomTopLeft.Y - 1, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);
            CheckRoomEdgesBetweenZones("down", roomTopLeft.X, roomTopLeft.Y + roomHeight, roomWidth, roomHeight, zoneTopLeft, f.RoomGrid, ref clusters, f);

            clusters.RemoveAll(x => x.Count < 30);

            //now actually have the room associated with the cluster
            //actually just need to search for edgepoints that are contained

            foreach (List<Point> c in clusters)
            {
                foreach (Zone z in f.Zones)
                {
                    foreach (Room r in z.Rooms)
                    {

                        //check if the cluster is a subset of their edgepoints
                        //bool results = query2.All(i => query1.Contains(i));

                        tempEdgePoints = r.Edgepoints.Select(i => { return new Point(i.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X, i.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y); }).ToList();

                        if (c.All(x => tempEdgePoints.Contains(x))) {
                            //adjacencies.Add(r, c);
                            //collection.Select(c => {c.PropertyToSet = value; return c;}).ToList();
                            tempCluster = c.Select(i => { return new Point(i.X - r.GrowthTopLeft.X - z.GrowthTopLeft.X, i.Y - r.GrowthTopLeft.Y - z.GrowthTopLeft.Y); }).ToList();
                            adjacencies.Add(r, tempCluster);
                        }
                    }
                }
            }

            //return clusters;
            return adjacencies;
        }
        private void CheckRoomEdgesBetweenZones(string direction, int roomX, int roomY, int roomWidth, int roomHeight, Point zoneTopLeft, char[,] grid, ref List<List<Point>> clusters, Floor f)
        {
            List<List<Point>> allClusters = new List<List<Point>>();
            List<Point> cluster = new List<Point>();
            int tempPos;
            Room r = null;

            if (direction == "left" || direction == "right")
            {
                for (int y = roomY + zoneTopLeft.Y; y < roomY + zoneTopLeft.Y + roomHeight; y++)
                {
                    //if (direction == "left")
                    //{
                    //    tempPos = roomX - 1;
                    //}
                    //else
                    //{
                    //    tempPos = roomX + 1;
                    //}
                    if (roomX + zoneTopLeft.X > 0 && roomX + zoneTopLeft.X < grid.GetUpperBound(0) && grid[roomX + zoneTopLeft.X, y] == 'R')
                    {
                        if (r is null || (!Room.ReferenceEquals(FindRoomFromGrid(roomX + zoneTopLeft.X, y, f ), r )))
                        {
                            if (cluster.Count > 0)
                            {
                                clusters.Add(cluster);
                            }
                            cluster = new List<Point> { new Point(roomX + zoneTopLeft.X, y) };
                            r = FindRoomFromGrid(roomX + zoneTopLeft.X, y, f);
                        } else
                        {
                            cluster.Add(new Point(roomX + zoneTopLeft.X, y));
                        }
                    }
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
                for (int x = roomX + zoneTopLeft.X; x < roomX + zoneTopLeft.X + roomWidth; x++)
                {
                    //if (direction == "up")
                    //{
                    //    tempPos = roomY - 1;
                    //}
                    //else
                    //{
                    //    tempPos = roomY + 1;
                    //}
                    if (roomY + zoneTopLeft.Y > 0 && roomY + zoneTopLeft.Y < grid.GetUpperBound(1) && grid[x, roomY + zoneTopLeft.Y] == 'R')
                    {

                        if (r is null || (!Room.ReferenceEquals(FindRoomFromGrid(x, roomY + zoneTopLeft.Y, f), r)))
                        {
                            if (cluster.Count > 0)
                            {
                                clusters.Add(cluster);
                            }
                            cluster = new List<Point> { new Point(x, roomY + zoneTopLeft.Y) };
                            r = FindRoomFromGrid(x, roomY + zoneTopLeft.Y, f);
                        }
                        else
                        {
                            cluster.Add(new Point(x, roomY + zoneTopLeft.Y));
                        }

                       // cluster.Add(new Point(x, roomY + zoneTopLeft.Y));
                    }
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
            if (cluster.Count > 0)
            {
                clusters.Add(cluster);
               // cluster = new List<Point>();
            }

        }
        private Room FindRoomFromGrid(int x, int y, Floor f)
        {
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
            //TODO: should I add a door to both rooms if adjacent?
            //dont add a door if already connected
            // && r2.AdjacencyDoors.ContainsKey(r)

            //if this room doesn't have a set pair
            if (r2 is null || (r2 is not null && !r.AdjacencyDoors.ContainsKey(r2))) {
                Rectangle tempRect;
                int doorLength = 20;
                int doorWidth = 5;
                Point startPoint;
                //door length goes parallel to the cluster
                //just add the rec from a random point

                if (cluster[0].X == cluster[1].X)
                {
                    tempRect = new Rectangle(0, 0, doorWidth, doorLength);
                    startPoint = cluster[_random.Next(0, cluster.Count - doorLength)];
                    if (cluster[0].X - 1 >= 0)
                    {
                        tempRect.X = startPoint.X - doorWidth + 1;
                        tempRect.Y = startPoint.Y;
                    }
                    else
                    {
                        tempRect.X = startPoint.X;
                        tempRect.Y = startPoint.Y;
                    }
                }
                else
                {
                    tempRect = new Rectangle(0, 0, doorLength, doorWidth);
                    startPoint = cluster[_random.Next(0, cluster.Count - doorLength)];

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

                //make sure the width goes the right way!! ughhhhfoeifheslf <----- TODO

                //r.Doors.Add(tempRect);


                if (r2 is not null)
                {
                    //only 1 room needs a rectangle
                    //just make sure both know trhat they have a door

                    //so the room will have the pos of the door on the floor, not jsut the room

                    //r.AdjacencyDoors.Add(r2, tempRect);
                    //r2.AdjacencyDoors.Add(r, tempRect);
                    // r2.Doors.Add(tempRect);
                    //make the door....but make it at the same x or y coordinates
                    //store x / y end of door in original room
                    Rectangle tempR = new Rectangle(0, 0, 0, 0);
                    int tempLength = 0;
                    //make r2 rect
                    if (cluster[0].X == cluster[1].X)
                    {
                        tempLength = tempRect.Y + r.ZoneTopLeft.Y + r.GrowthTopLeft.Y - r2.ZoneTopLeft.Y - r2.GrowthTopLeft.Y;
                        if (cluster[0].X == 0)
                        {
                            
                            //make on right
                            tempR = new Rectangle(r2.RectWidth - doorWidth, tempLength, doorWidth, doorLength);
                        } else
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
                        } else
                        {
                            tempR = new Rectangle(tempLength, 0 , doorLength, doorWidth);
                        }
                    }
                    tempR.X += r2.GrowthTopLeft.X + r2.ZoneTopLeft.X;
                    tempR.Y += r2.GrowthTopLeft.Y + r2.ZoneTopLeft.Y;
                    r2.Doors.Add(tempR);
                    r2.AdjacencyDoors.Add(r, tempR);
                }
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
            //add all valid to a queue
            Room room;
            Queue<Room> _roomsConnectedToOutside = new Queue<Room>();
            foreach (Room r in z.Rooms)
            {
                if (r.Doors.Count > r.Adjacencies.Count)
                {
                    _roomsConnectedToOutside.Enqueue(r);
                }
            }

            //while loop...
            while (_roomsConnectedToOutside.Count > 0)
            {
                //for each in the queue..mark friends as conected with a link to the room
                //make sure only new connections are marked
                room = _roomsConnectedToOutside.Dequeue();
                foreach (Room r in room.AdjacencyDoors.Keys)
                {
                    if (!room.Connections.Contains(r) && r.RoomType.Type == room.RoomType.Type)
                    {
                        r.Connections.Add(room);
                        _roomsConnectedToOutside.Enqueue(r);
                    }
                }
            }

            //now actually remove em...
            foreach (Room r in z.Rooms)
            {
                //if the room has more than one connection? choose a random one and discard all the rest
                if (r.Doors.Count > r.Adjacencies.Count &&r.Connections.Count > 0 )
                {
                    foreach (Room connectedRoom in r.Connections)
                    {
                        Room.RemoveRoomAdjacency(connectedRoom, r);
                    }
                }
                else if (r.Connections.Count > 1)
                {
                    //choose a random room
                    //remove all adjacencies from other rooms
                    //and get rid of the rectangles too..for both rooms
                    room = r.Connections[_random.Next(0, r.Connections.Count)];

                    foreach (Room connectedRoom in r.Connections)
                    {
                        if (!Room.ReferenceEquals(connectedRoom, room))
                        {
                            //remove it...TODO: smth wrong here
                            Room.RemoveRoomAdjacency(connectedRoom, r);
                        }
                    }

                }
            }

        }

        //TODO: add walls
        //after doors added
        //wall width and height goes inside rooms
        //decide on dimensions

        private void AddWalls(Zone z, int xUpperBound, int yUpperBound)
        {
            int wallWidth = 5; //TODO: tweak
            //go left, right, up, down
            //how to check for doors?
            //if own door
            // - avoid intersection of edge and door
            // - also add the place to avoid for the other room
            
            //what if room has adjacency from another zone??
            //gotta do it for all rooms on the floor

            //for doors - add clear points
            AddClearEdges(z, xUpperBound, yUpperBound);
            //now add the walls for that zone
            foreach (Room r in z.Rooms)
            {
                AddWalls(r, wallWidth, z.GrowthTopLeft);
            }
            
        }

        private void AddClearEdges(Zone z, int xUpperBound, int yUpperBound)
        {
            List<Point> tempEdgePoints;
            foreach (Room r in z.Rooms)
            {
                tempEdgePoints = r.Edgepoints.Select(i => { return new Point(i.X + r.GrowthTopLeft.X + z.GrowthTopLeft.X, i.Y + r.GrowthTopLeft.Y + z.GrowthTopLeft.Y); }).ToList();
                foreach (Rectangle door in r.Doors)
                {
                    r.ClearPoints.AddRange(GetClearPointsFromDoor(tempEdgePoints, door));
                }
                foreach (KeyValuePair<Room, Rectangle> kvp in r.AdjacencyDoors)
                {
                    //r.ClearPoints.AddRange(GetClearPointsFromDoor(tempEdgePoints, kvp.Value));
                    //kvp.Key.ClearPoints.AddRange(GetClearPointsFromDoor(tempEdgePoints, kvp.Value));
                    //r.ClearPoints.AddRange(GetClearPointsFromOtherDoor(tempEdgePoints, kvp.Value, xUpperBound, yUpperBound));
                    //kvp.Key.ClearPoints.AddRange(GetClearPointsFromOtherDoor(tempEdgePoints, kvp.Value, xUpperBound, yUpperBound));
                }
            }
        }

        private List<Point> GetClearPointsFromOtherDoor(List<Point> edgePoints, Rectangle door, int xUpperBound, int yUpperBound)
        {
            //list of edgepoints
            //check depending on door alignment
            // - horizontal: check above top, or below bottom
            // - vertical: check left to left, or right of right
            List<Point> pointsToAdd = new List<Point>();
            if (door.Width > door.Height )
            {
                //horizontal
                if (door.X > 0)
                {
                    pointsToAdd.AddRange(GetClearPointsFromDoor(edgePoints, new Rectangle(door.X - 1, door.Y, door.Width, door.Height)));
                }

                if (pointsToAdd.Count == 0 && door.X + door.Width <= xUpperBound )
                {
                    pointsToAdd.AddRange(GetClearPointsFromDoor(edgePoints, new Rectangle(door.X, door.Y, door.Width + 1, door.Height)));
                }
            } else
            {
                if (door.Y > 0)
                {
                    pointsToAdd.AddRange(GetClearPointsFromDoor(edgePoints, new Rectangle(door.X, door.Y - 1, door.Width, door.Height)));
                }

                if (pointsToAdd.Count == 0 && door.Y + door.Height <= yUpperBound )
                {
                    pointsToAdd.AddRange(GetClearPointsFromDoor(edgePoints, new Rectangle(door.X, door.Y, door.Width, door.Height + 1)));
                }
            }
            return pointsToAdd;
        }

        private List<Point> GetClearPointsFromDoor( List<Point> edgePoints, Rectangle door)
        {
            bool foundEdge = false;
            List<Point> pointsToAdd = new List<Point>();
            Point tempPoint;

            //left
            for (int y = door.Y; y < door.Y + door.Height; y++)
            {
                tempPoint = new Point(door.X, y);
                if (edgePoints.Contains(tempPoint))
                {
                    pointsToAdd.Add(tempPoint);
                    if (pointsToAdd.Count > 1)
                    {
                        foundEdge = true;
                    }
                }
            }

            if (!foundEdge)
            {
                for (int y = door.Y; y < door.Y + door.Height; y++)
                {
                    tempPoint = new Point(door.X + door.Width - 1, y);
                    if (edgePoints.Contains(tempPoint))
                    {
                        pointsToAdd.Add(tempPoint);
                        if (pointsToAdd.Count > 2)
                        {
                            foundEdge = true;
                        }
                    }
                }
            }

            if (!foundEdge)
            {
                for (int x = door.X; x < door.X + door.Width; x++)
                {
                    tempPoint = new Point(x, door.Y);
                    if (edgePoints.Contains(tempPoint))
                    {
                        pointsToAdd.Add(tempPoint);
                        if (pointsToAdd.Count > 3)
                        {
                            foundEdge = true;
                        }
                    }
                }
            }


            if (!foundEdge)
            {
                for (int x = door.X; x < door.X + door.Width; x++)
                {
                    tempPoint = new Point(x, door.Y + door.Height - 1);
                    if (edgePoints.Contains(tempPoint))
                    {
                        pointsToAdd.Add(tempPoint);
                    }
                }
            }
            return pointsToAdd;
        }

        private void AddWalls(Room r, int width, Point zoneTopLeft)
        {
            //but need to stay away from clear points :(

            List<(int, int)> clusters = new List<(int, int)>();

            clusters = GetRectPairs("left", r, zoneTopLeft, rectX: r.GrowthTopLeft.X);

            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(r.GrowthTopLeft.X + zoneTopLeft.X, c.Item1, width, c.Item2 - c.Item1 + 1));
            }

            clusters = GetRectPairs("right", r, zoneTopLeft, rectX: r.GrowthTopLeft.X + r.RectWidth - 1);

            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(r.GrowthTopLeft.X + zoneTopLeft.X + r.RectWidth - width, c.Item1, width, c.Item2 - c.Item1 + 1));
            }

            clusters = GetRectPairs("up", r, zoneTopLeft, rectY: r.GrowthTopLeft.Y);

            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(c.Item1, r.GrowthTopLeft.Y + zoneTopLeft.Y, c.Item2 - c.Item1 + 1, width));
            }

            clusters = GetRectPairs("down", r, zoneTopLeft, rectY: r.GrowthTopLeft.Y + r.RectHeight - 1);

            foreach ((int, int) c in clusters)
            {
                r.Walls.Add(new Rectangle(c.Item1, zoneTopLeft.Y + r.GrowthTopLeft.Y + r.RectHeight - width, c.Item2 - c.Item1 + 1, width));
            }
        }

        private List<(int, int)> GetRectPairs(string direction, Room r, Point zoneTopLeft, int rectX = 0, int rectY = 0)
        {
            List<(int, int)> clusters = new List<(int, int)>();
            (int, int) rectPair = (-1, -1);
            //iterate and check? 
            //store the one you end at and the one you start again at

            if (direction == "left" || direction == "right")
            {
                for (int y = r.GrowthTopLeft.Y; y < r.GrowthTopLeft.Y + r.RectHeight; y++)
                {
                    if (!r.ClearPoints.Contains(new Point(rectX + zoneTopLeft.X, y + zoneTopLeft.Y)) && rectPair.Item1 == -1)
                    {
                        rectPair.Item1 = y + zoneTopLeft.Y;
                    }
                    else if (r.ClearPoints.Contains(new Point(rectX + zoneTopLeft.X, y + zoneTopLeft.Y)) && rectPair.Item1 != -1)
                    {
                        rectPair.Item2 = y + zoneTopLeft.Y;
                        clusters.Add(rectPair);
                        rectPair = (-1, -1);
                    }
                }


                if (rectPair.Item1 != -1)
                {
                    rectPair.Item2 = r.GrowthTopLeft.Y + r.RectHeight + zoneTopLeft.Y - 1;
                    clusters.Add(rectPair);
                }
            }
            else
            {
                for (int x = r.GrowthTopLeft.X; x < r.GrowthTopLeft.X + r.RectWidth; x++)
                {
                    if (!r.ClearPoints.Contains(new Point(x + zoneTopLeft.X, rectY + zoneTopLeft.Y)) && rectPair.Item1 == -1)
                    {
                        rectPair.Item1 = x + zoneTopLeft.X;
                    }
                    else if (r.ClearPoints.Contains(new Point(x + zoneTopLeft.X, rectY + zoneTopLeft.Y)) && rectPair.Item1 != -1)
                    {
                        rectPair.Item2 = x + zoneTopLeft.X;
                        clusters.Add(rectPair);
                        rectPair = (-1, -1);
                    }
                }
                if (rectPair.Item1 != -1)
                {
                    rectPair.Item2 = r.GrowthTopLeft.X + zoneTopLeft.X + r.RectWidth - 1;
                    clusters.Add(rectPair);
                }
            }
            return clusters;
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