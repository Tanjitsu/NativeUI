﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using Control = GTA.Control;
using Font = GTA.Font;

namespace NativeUI
{
    public delegate void IndexChangedEvent(UIMenu sender, int newIndex);

    public delegate void ListChangedEvent(UIMenu sender, UIMenuListItem listItem, int newIndex);

    public delegate void CheckboxChangeEvent(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool Checked);

    public delegate void ItemSelectEvent(UIMenu sender, UIMenuItem selectedItem, int index);


    /// <summary>
    /// Base class for NativeUI. Calls the next events: OnIndexChange, OnListChanged, OnCheckboxChange, OnItemSelect.
    /// </summary>
    public class UIMenu
    {
        private readonly UIContainer _mainMenu;
        private readonly Sprite _logo;
        private readonly Sprite _background;

        private readonly UIRectangle _descriptionBar;
        private readonly Sprite _descriptionRectangle;
        private UIText _descriptionText;
        private UIText _counterText;


        private int _activeItem = 1000;

        //Pagination
        private const int MaxItemsOnScreen = 9;
        private int _minItem;
        private int _maxItem = MaxItemsOnScreen;

        
        private readonly Sprite _upAndDownSprite;
        private readonly UIRectangle _extraRectangleUp;
        private readonly UIRectangle _extraRectangleDown;

        private Point Offset;
        private int ExtraYOffset;
        
        public List<UIMenuItem> MenuItems = new List<UIMenuItem>();

        //Events

        /// <summary>
        /// Called when user presses up or down, changing current selection.
        /// </summary>
        public event IndexChangedEvent OnIndexChange;

        /// <summary>
        /// Called when user presses left or right, changing a list position.
        /// </summary>
        public event ListChangedEvent OnListChange;

        /// <summary>
        /// Called when user presses enter on a checkbox item.
        /// </summary>
        public event CheckboxChangeEvent OnCheckboxChange;

        /// <summary>
        /// Called when user selects a simple item.
        /// </summary>
        public event ItemSelectEvent OnItemSelect;

        //Keys
        private Dictionary<MenuControls, Tuple<List<Keys>, List<Tuple<GTA.Control, int>>>> _keyDictionary = new Dictionary<MenuControls, Tuple<List<Keys>, List<Tuple<GTA.Control, int>>>> ();
        

        /// <summary>
        /// Basic Menu constructor.
        /// </summary>
        /// <param name="title">Title that appears on the big banner.</param>
        /// <param name="subtitle">Subtitle that appears in capital letters in a small black bar.</param>
        public UIMenu(string title, string subtitle) : this(title, subtitle, new Point(0, 0), "commonmenu", "interaction_bgd")
        {
        }


        /// <summary>
        /// Basic Menu constructor with an offset.
        /// </summary>
        /// <param name="title">Title that appears on the big banner.</param>
        /// <param name="subtitle">Subtitle that appears in capital letters in a small black bar.</param>
        /// <param name="offset">Point object with X and Y data for offsets. Applied to all menu elements.</param>
        public UIMenu(string title, string subtitle, Point offset) : this(title, subtitle, offset, "commonmenu", "interaction_bgd")
        {
        }


        /// <summary>
        /// Advanced Menu constructor that allows custom title banner.
        /// </summary>
        /// <param name="title">Title that appears on the big banner. Set to "" if you are using a custom banner.</param>
        /// <param name="subtitle">Subtitle that appears in capital letters in a small black bar.</param>
        /// <param name="offset">Point object with X and Y data for offsets. Applied to all menu elements.</param>
        /// <param name="spriteLibrary">Sprite library name for the banner.</param>
        /// <param name="spriteName">Sprite name for the banner.</param>
        public UIMenu(string title, string subtitle, Point offset, string spriteLibrary, string spriteName)
        {
            Offset = offset;

            _mainMenu = new UIContainer(new Point(0 + Offset.X, 0 + Offset.Y), new Size(700, 500), Color.FromArgb(0, 0, 0, 0));
            _logo = new Sprite(spriteLibrary, spriteName, new Point(0 + Offset.X, 0 + Offset.Y), new Size(290, 75));
            _mainMenu.Items.Add(new UIText(title, new Point(145, 15), 1.15f, Color.White, Font.HouseScript, true));
            if (!String.IsNullOrWhiteSpace(subtitle))
            {
                _mainMenu.Items.Add(new UIRectangle(new Point(0, 75), new Size(290, 25), Color.Black));
                _mainMenu.Items.Add(new UIText(subtitle, new Point(5, 78), 0.35f, Color.WhiteSmoke, 0, false));

                if (subtitle.StartsWith("~"))
                {
                    CounterPretext = subtitle.Substring(0, 3);
                }
                _counterText = new UIText("", new Point(270 + Offset.X, 78 + Offset.Y), 0.35f, Color.WhiteSmoke, 0, false);
                ExtraYOffset = 25;
            }
            Title = title;
            Subtitle = subtitle;

            _upAndDownSprite = new Sprite("commonmenu", "shop_arrows_upanddown", new Point(120 + Offset.X, 97 + 25 * (MaxItemsOnScreen + 1) + Offset.Y - 25 + ExtraYOffset), new Size(30, 30));
            _extraRectangleUp = new UIRectangle(new Point(0 + Offset.X, 100 + 25 * (MaxItemsOnScreen + 1) + Offset.Y - 25 + ExtraYOffset), new Size(290, 12), Color.FromArgb(200, 0, 0, 0));
            _extraRectangleDown = new UIRectangle(new Point(0 + Offset.X, 112 + 25 * (MaxItemsOnScreen + 1) + Offset.Y - 25 + ExtraYOffset), new Size(290, 12), Color.FromArgb(200, 0, 0, 0));

            _descriptionBar = new UIRectangle(new Point(Offset.X, 125), new Size(290, 2), Color.Black);
            _descriptionRectangle = new Sprite("commonmenu", "gradient_bgd", new Point(Offset.X, 127), new Size(290, 30));
            _descriptionText = new UIText("Description", new Point(Offset.X + 5, 125), 0.33f, Color.FromArgb(255, 255, 255, 255), Font.ChaletLondon, false);

            _background = new Sprite("commonmenu", "gradient_bgd", new Point(Offset.X, 100 + Offset.Y - 25 + ExtraYOffset), new Size(290, 25));

            SetKey(MenuControls.Up, GTA.Control.FrontendUp);
            SetKey(MenuControls.Down, GTA.Control.FrontendDown);
            SetKey(MenuControls.Left, GTA.Control.FrontendLeft);
            SetKey(MenuControls.Right, GTA.Control.FrontendRight);
            SetKey(MenuControls.Select, GTA.Control.FrontendAccept);
        }

        private void RecaulculateDescriptionPosition()
        {
            _descriptionBar.Position = new Point(Offset.X, 105 - 25 + ExtraYOffset);
            _descriptionRectangle.Position = new Point(Offset.X, 105 - 25 + ExtraYOffset);
            _descriptionText.Position = new Point(Offset.X + 5, 107 - 25 + ExtraYOffset);

            int count = Size;
            if (count > MaxItemsOnScreen + 1)
                count = MaxItemsOnScreen + 2;

            _descriptionBar.Position = new Point(Offset.X, 25*count + _descriptionBar.Position.Y);
            _descriptionRectangle.Position = new Point(Offset.X, 25*count + _descriptionRectangle.Position.Y);
            _descriptionText.Position = new Point(Offset.X + 5, 25*count + _descriptionText.Position.Y);
        }

        private void DisEnableControls(bool enable)
        {
            Hash thehash = enable ? Hash.ENABLE_CONTROL_ACTION : Hash.DISABLE_CONTROL_ACTION;
            foreach (var con in Enum.GetValues(typeof(GTA.Control)))
            {
                Function.Call(thehash, 0, (int)con);
                Function.Call(thehash, 1, (int)con);
                Function.Call(thehash, 2, (int)con);
            }
            //Controls we want
            // -Frontend
            // -Mouse
            // -Walk/Move
            // -
            if (!enable)
            {
                var list = new List<GTA.Control>
                {
                    Control.FrontendAccept,
                    Control.FrontendAxisX,
                    Control.FrontendAxisY,
                    Control.FrontendDown,
                    Control.FrontendUp,
                    Control.FrontendLeft,
                    Control.FrontendRight,
                    Control.FrontendCancel,
                    Control.FrontendSelect,
                    Control.CursorScrollDown,
                    Control.CursorScrollUp,
                    Control.CursorX,
                    Control.CursorY,
                    Control.MoveUpDown,
                    Control.MoveLeftRight,
                    Control.Sprint,
                    Control.Jump,
                    Control.Enter,
                    Control.VehicleAccelerate,
                    Control.VehicleBrake,
                    Control.VehicleMoveLeftRight,
                    Control.VehicleFlyYawLeft,
                    Control.FlyLeftRight,
                    Control.FlyUpDown,
                    Control.VehicleFlyYawRight,
                    Control.VehicleHandbrake,
                    Control.FrontendPause,
                    Control.FrontendPauseAlternate,
                };
                foreach (var control in list)
                {
                    Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int)control);
                    Function.Call(Hash.ENABLE_CONTROL_ACTION, 1, (int)control);
                    Function.Call(Hash.ENABLE_CONTROL_ACTION, 2, (int)control);
                }
            }
        }

        /// <summary>
        /// Add an item to the menu.
        /// </summary>
        /// <param name="item">Item object to be added. Can be normal item, checkbox or list item.</param>
        public void AddItem(UIMenuItem item)
        {
            item.Offset = Offset;
            item.Position((MenuItems.Count * 25) - 25 + ExtraYOffset);
            MenuItems.Add(item);

            RecaulculateDescriptionPosition();
        }


        /// <summary>
        /// Remove an item at index n
        /// </summary>
        /// <param name="index">Index to remove the item at.</param>
        public void RemoveItemAt(int index)
        {
            if (Size > MaxItemsOnScreen && _maxItem == Size - 1)
            {
                _maxItem--;
                _minItem--;
            }
            MenuItems.RemoveAt(index);
            RecaulculateDescriptionPosition();
        }


        /// <summary>
        /// Reset the current selected item to 0. Use this after you add or remove items dynamically.
        /// </summary>
        public void RefreshIndex()
        {
            MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
            _activeItem = 1000 - (1000 % MenuItems.Count);
            _maxItem = MaxItemsOnScreen;
            _minItem = 0;
        }


        /// <summary>
        /// Remove all items from the menu.
        /// </summary>
        public void Clear()
        {
            MenuItems.Clear();
            RecaulculateDescriptionPosition();
        }


        /// <summary>
        /// Draw the menu and all of it's components.
        /// </summary>
        public void Draw()
        {
            if (Visible)
            {
                DisEnableControls(false);
            }
            else
            {
                DisEnableControls(true);
                return;
            }
            Function.Call((Hash)0xB8A850F20A067EB6, 76, 84);           // Safezone
            Function.Call((Hash)0xF5A2C681787E579D, 0f, 0f, 0f, 0f);   // stuff


            _background.Size = Size > MaxItemsOnScreen + 1 ? new Size(290, 25*(MaxItemsOnScreen + 1)) : new Size(290, 25 * Size);
            _background.Draw();

            _logo.Draw();
            MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
            _mainMenu.Draw();
            if (!String.IsNullOrWhiteSpace(MenuItems[_activeItem%(MenuItems.Count)].Description))
            {
                _descriptionText.Caption = FormatDescription(MenuItems[_activeItem%(MenuItems.Count)].Description);
                int numLines = _descriptionText.Caption.Split('\n').Length;
                _descriptionRectangle.Size = new Size(290, numLines * 20 + 2);

                _descriptionBar.Draw();
                _descriptionRectangle.Draw();
                _descriptionText.Draw();
            }

            if (MenuItems.Count <= MaxItemsOnScreen + 1)
            {
                int count = 0;
                foreach (var item in MenuItems)
                {
                    item.Position(count * 25 - 25 + ExtraYOffset);
                    item.Draw();
                    count++;
                }
            }
            else
            {
                int count = 0;
                for (int index = _minItem; index <= _maxItem; index++)
                {
                    var item = MenuItems[index];
                    item.Position(count * 25 - 25 + ExtraYOffset);
                    item.Draw();
                    count++;
                }
                _extraRectangleUp.Draw();
                _extraRectangleDown.Draw();
                _upAndDownSprite.Draw();
                if (_counterText != null)
                {
                    string cap = (CurrentSelection + 1) + " / " + Size;
                    SizeF strSize;
                    using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
                    {
                        strSize = g.MeasureString(cap, new System.Drawing.Font("Helvetica", 11, FontStyle.Regular, GraphicsUnit.Pixel));
                    }
                    int offset = Convert.ToInt32(strSize.Width);
                    _counterText.Position = new Point(285 - offset + Offset.X, 78 + Offset.Y);
                    _counterText.Caption = CounterPretext + cap;
                    _counterText.Draw();
                }
            }
            Function.Call((Hash)0xE3A3DB414A373DAB); // Safezone end
        }

        public bool IsMouseInBounds(Point TopLeft, Size boxSize)
        {
            int mouseX = Convert.ToInt32(Math.Round(Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.CursorX) * UI.WIDTH));
            int mouseY = Convert.ToInt32(Math.Round(Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)GTA.Control.CursorY) * UI.HEIGHT));
            //UI.ShowSubtitle(String.Format("X: {0} Y: {1}", mouseX, mouseY)); //debug
            return (mouseX >= TopLeft.X && mouseX <= TopLeft.X + boxSize.Width)
                   && (mouseY > TopLeft.Y && mouseY < TopLeft.Y + boxSize.Height);
        }
        
        /// <summary>
        /// Function to get whether the cursor is in an arrow space, or in label.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>0 - Not in item at all, 1 - In label, 2 - In arrow space.</returns>
        public int IsMouseInListItemArrows(UIMenuListItem item, Point TopLeft, Point Safezone)
        {
            int labelSize;
            SizeF strSize;
            using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
            {
                strSize = g.MeasureString(item.Text, new System.Drawing.Font("Arial", 11, FontStyle.Regular, GraphicsUnit.Pixel));
            }
            labelSize = Convert.ToInt32(strSize.Width);
            int labelSizeX = 5 + labelSize + 5;
            int arrowSizeX = 290 - labelSizeX;
            return IsMouseInBounds(TopLeft, new Size(labelSizeX, 25))
                ? 1
                : IsMouseInBounds(new Point(TopLeft.X + labelSizeX, TopLeft.Y), new Size(arrowSizeX, 25)) ? 2 : 0;

        }

        public void GetSafezoneBounds(out int safezoneX, out int safezoneY)
        {
            float t = Function.Call<float>(Hash._0xBAF107B6BB2C97F0);
            double g = Math.Round(Convert.ToDouble(t), 2);
            g = (g * 100) - 90;
            g = 10 - g;
            safezoneX = Convert.ToInt32(Math.Round(g*6.4));
            safezoneY = Convert.ToInt32(Math.Round(g*3.6));
        }

        public void GoUpOverflow()
        {
            if (Size <= MaxItemsOnScreen + 1) return;
            if (_activeItem % MenuItems.Count <= _minItem)
            {
                if (_activeItem % MenuItems.Count == 0)
                {
                    _minItem = MenuItems.Count - MaxItemsOnScreen - 1;
                    _maxItem = MenuItems.Count - 1;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
                    _activeItem = 1000 - (1000 % MenuItems.Count);
                    _activeItem += MenuItems.Count - 1;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
                }
                else
                {
                    _minItem--;
                    _maxItem--;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
                    _activeItem--;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
                }
            }
            else
            {
                MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
                _activeItem--;
                MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
            }
            Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            IndexChange(CurrentSelection);
        }

        public void GoUp()
        {
            if (Size > MaxItemsOnScreen + 1) return;
            MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
            _activeItem--;
            MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
            Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            IndexChange(CurrentSelection);
        }

        public void GoDownOverflow()
        {
            if (Size <= MaxItemsOnScreen + 1) return;
            if (_activeItem % MenuItems.Count >= _maxItem)
            {
                if (_activeItem % MenuItems.Count == MenuItems.Count - 1)
                {
                    _minItem = 0;
                    _maxItem = MaxItemsOnScreen;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
                    _activeItem = 1000 - (1000 % MenuItems.Count);
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
                }
                else
                {
                    _minItem++;
                    _maxItem++;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
                    _activeItem++;
                    MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
                }
            }
            else
            {
                MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
                _activeItem++;
                MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
            }
            Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            IndexChange(CurrentSelection);
        }

        public void GoDown()
        {
            if (Size > MaxItemsOnScreen + 1) return;
            MenuItems[_activeItem % (MenuItems.Count)].Selected = false;
            _activeItem++;
            MenuItems[_activeItem % (MenuItems.Count)].Selected = true;
            Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            IndexChange(CurrentSelection);
        }

        public void GoLeft()
        {
            if (!(MenuItems[CurrentSelection] is UIMenuListItem)) return;
            var it = (UIMenuListItem)MenuItems[CurrentSelection];
            it.Index--;
            Game.PlaySound("NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            ListChange(it, it.Index);
        }

        public void GoRight()
        {
            if (!(MenuItems[CurrentSelection] is UIMenuListItem)) return;
            var it = (UIMenuListItem)MenuItems[CurrentSelection];
            it.Index++;
            Game.PlaySound("NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
            ListChange(it, it.Index);
        }

        public void SelectItem()
        {
            if (MenuItems[CurrentSelection] is UIMenuCheckboxItem)
            {
                var it = (UIMenuCheckboxItem)MenuItems[CurrentSelection];
                it.Checked = !it.Checked;
                Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                CheckboxChange(it, it.Checked);
            }
            else
            {
                Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                ItemSelect(MenuItems[CurrentSelection], CurrentSelection);
            }
        }
        
        /// <summary>
        /// Call this in OnTick
        /// </summary>
        public void ProcessMouse()
        {
            if (!Visible) return;
            int safezoneOffsetX;
            int safezoneOffsetY;
            GetSafezoneBounds(out safezoneOffsetX, out safezoneOffsetY);
            Function.Call(Hash._SHOW_CURSOR_THIS_FRAME);
            int limit = MenuItems.Count - 1;
            int counter = 0;
            if (MenuItems.Count > MaxItemsOnScreen + 1)
                limit = _maxItem;
            for (int i = _minItem; i <= limit; i++)
            {
                int Xpos = Offset.X + safezoneOffsetX;
                int Ypos = Offset.Y + 100 - 25 + ExtraYOffset + (counter*25) + safezoneOffsetY;
                int Xsize = 290;
                int Ysize = 25;
                UIMenuItem uiMenuItem = MenuItems[i];
                if (IsMouseInBounds(new Point(Xpos, Ypos), new Size(Xsize, Ysize)))
                {
                    uiMenuItem.Hovered = true;
                    if (Game.IsControlJustPressed(0, GTA.Control.Attack))
                        if (uiMenuItem.Selected)
                        {
                            if (MenuItems[i] is UIMenuListItem &&
                                IsMouseInListItemArrows((UIMenuListItem) MenuItems[i], new Point(Xpos, Ypos),
                                    new Point(safezoneOffsetX, safezoneOffsetY)) > 0)
                            {
                                int res = IsMouseInListItemArrows((UIMenuListItem) MenuItems[i], new Point(Xpos, Ypos),
                                    new Point(safezoneOffsetX, safezoneOffsetY));
                                if (res == 1) // Label clicked
                                {
                                    Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                    ItemSelect(MenuItems[i], i);
                                }
                                else if (res == 2) // Arrow clicked: next
                                {
                                    var it = (UIMenuListItem) MenuItems[i];
                                    it.Index++;
                                    Game.PlaySound("NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                                    ListChange(it, it.Index);
                                }
                            }
                            else
                                SelectItem();
                        }
                        else
                        {
                            CurrentSelection = i;
                            Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
                            IndexChange(CurrentSelection);
                        }
                }
                else
                    uiMenuItem.Hovered = false;
                counter++;
            }
            int extraY = 100 + 25*(MaxItemsOnScreen + 1) + Offset.Y - 25 + ExtraYOffset + safezoneOffsetY;
            int extraX = safezoneOffsetX + Offset.X;

            if (IsMouseInBounds(new Point(extraX, extraY), new Size(290, 12)))
            {
                _extraRectangleUp.Color = Color.FromArgb(255, 30, 30, 30);
                if (Game.IsControlJustPressed(0, GTA.Control.Attack))
                {
                    if(Size > MaxItemsOnScreen+1)
                        GoUpOverflow();
                    else
                        GoUp();
                }
            }
            else
                _extraRectangleUp.Color = Color.FromArgb(200, 0, 0, 0);
            
            if (IsMouseInBounds(new Point(extraX, extraY+12), new Size(290, 12)))
            {
                _extraRectangleDown.Color = Color.FromArgb(255, 30, 30, 30);
                if (Game.IsControlJustPressed(0, GTA.Control.Attack))
                {
                    if (Size > MaxItemsOnScreen + 1)
                        GoDownOverflow();
                    else
                        GoDown();
                }
            }
            else
                _extraRectangleDown.Color = Color.FromArgb(200, 0, 0, 0);
        }

        /// <summary>
        /// Set a key to control a menu. Can be multiple keys for each control.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="keyToSet"></param>
        public void SetKey(MenuControls control, Keys keyToSet)
        {
            if (_keyDictionary.ContainsKey(control))
                _keyDictionary[control].Item1.Add(keyToSet);
            else
            {
                _keyDictionary.Add(control,
                    new Tuple<List<Keys>, List<Tuple<Control, int>>>(new List<Keys>(), new List<Tuple<Control, int>>()));
                _keyDictionary[control].Item1.Add(keyToSet);
            }
        }


        /// <summary>
        /// Set a GTA.Control to control a menu. Can be multiple controls. This applies it to all indexes.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="gtaControl"></param>
        public void SetKey(MenuControls control, GTA.Control gtaControl)
        {
            SetKey(control, gtaControl, 0);
            SetKey(control, gtaControl, 1);
            SetKey(control, gtaControl, 2);
        }


        /// <summary>
        /// Set a GTA.Control to control a menu only on a specific index.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="gtaControl"></param>
        /// <param name="controlIndex"></param>
        public void SetKey(MenuControls control, GTA.Control gtaControl, int controlIndex)
        {
            if (_keyDictionary.ContainsKey(control))
                _keyDictionary[control].Item2.Add(new Tuple<Control, int>(gtaControl, controlIndex));
            else
            {
                _keyDictionary.Add(control,
                    new Tuple<List<Keys>, List<Tuple<Control, int>>>(new List<Keys>(), new List<Tuple<Control, int>>()));
                _keyDictionary[control].Item2.Add(new Tuple<Control, int>(gtaControl, controlIndex));
            }

        }


        /// <summary>
        /// Remove all controls on a control.
        /// </summary>
        /// <param name="control"></param>
        public void ResetKey(MenuControls control)
        {
            _keyDictionary[control].Item1.Clear();
            _keyDictionary[control].Item2.Clear();
        }

        public bool HasControlJustBeenPressed(MenuControls control, Keys key = Keys.None)
        {
            List<Keys> tmpKeys = new List<Keys>(_keyDictionary[control].Item1);
            List<Tuple<GTA.Control, int>> tmpControls = new List<Tuple<Control, int>>(_keyDictionary[control].Item2);

            if (key != Keys.None)
            {
                if (tmpKeys.Any(Game.IsKeyPressed))
                    return true;
            }
            if (tmpControls.Any(tuple => Game.IsControlJustPressed(tuple.Item2, tuple.Item1)))
                return true;
            return false;
        }
        
        /// <summary>
        /// Process control-stroke. Call this in the OnTick event.
        /// </summary>
        public void ProcessControl(Keys key = Keys.None)
        {
            if(!Visible) return;
            if (HasControlJustBeenPressed(MenuControls.Up, key) || Game.IsControlJustPressed(0, GTA.Control.CursorScrollUp))
            {
                if (Size > MaxItemsOnScreen + 1)
                    GoUpOverflow();
                else
                    GoUp();
            }
            else if (HasControlJustBeenPressed(MenuControls.Down, key) || Game.IsControlJustPressed(0, GTA.Control.CursorScrollDown))
            {
                if (Size > MaxItemsOnScreen + 1)
                    GoDownOverflow();
                else
                    GoDown();
            }
            else if (HasControlJustBeenPressed(MenuControls.Left, key))
            {
                GoLeft();
            }

            else if (HasControlJustBeenPressed(MenuControls.Right, key))
            {
                GoRight();
            }

            else if (HasControlJustBeenPressed(MenuControls.Select, key))
            {
                SelectItem();
            }
        }


        /// <summary>
        /// Process keystroke. Call this in the OnKeyDown event.
        /// </summary>
        /// <param name="key"></param>
        public void ProcessKey(Keys key)
        {
            if ((from object menuControl in Enum.GetValues(typeof(MenuControls)) select new List<Keys>(_keyDictionary[(MenuControls)menuControl].Item1)).Any(tmpKeys => tmpKeys.Any(k => k == key)))
            {
                ProcessControl(key);
            }
        }

        private string FormatDescription(string input)
        {
            int maxPixelsPerLine = 250;
            int aggregatePixels = 0;
            string output = "";
            string[] words = input.Split(' ');
            foreach (string word in words)
            {
                SizeF strSize;
                using (Graphics g = Graphics.FromImage(new Bitmap(1, 1)))
                {
                    strSize = g.MeasureString(word, new System.Drawing.Font("Helvetica", 11, FontStyle.Regular, GraphicsUnit.Pixel));
                }
                aggregatePixels += Convert.ToInt32(strSize.Width);
                if (aggregatePixels > maxPixelsPerLine)
                {
                    output += "\n" + word + " ";
                    aggregatePixels = Convert.ToInt32(strSize.Width);
                }
                else
                {
                    output += word + " ";
                }
            }
            return output;
        }

        /// <summary>
        /// Change whether this menu is visible to the user.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Returns the current selected item's index.
        /// Change the current selected item to index. Use this after you add or remove items dynamically.
        /// </summary>
        public int CurrentSelection
        {
            get { return _activeItem % MenuItems.Count; }
            set
            {
                MenuItems[_activeItem%(MenuItems.Count)].Selected = false;
                _activeItem = 1000 - (1000 % MenuItems.Count) + value;
                if (CurrentSelection > _maxItem)
                {
                    _maxItem = CurrentSelection;
                    _minItem = CurrentSelection - MaxItemsOnScreen;
                }
                else if (CurrentSelection < _minItem)
                {
                    _maxItem = MaxItemsOnScreen + CurrentSelection;
                    _minItem = CurrentSelection;
                }
            }
        }


        /// <summary>
        /// Returns the amount of items in the menu.
        /// </summary>
        public int Size
        {
            get { return MenuItems.Count; }
        }


        /// <summary>
        /// Returns the current title.
        /// </summary>
        public string Title { get; }


        /// <summary>
        /// Returns the current subtitle.
        /// </summary>
        public string Subtitle { get; }

        public string CounterPretext { get; set; }

        protected virtual void IndexChange(int newindex)
        {
            OnIndexChange?.Invoke(this, newindex);
        }

        protected virtual void ListChange(UIMenuListItem sender, int newindex)
        {
            OnListChange?.Invoke(this, sender, newindex);
        }

        protected virtual void ItemSelect(UIMenuItem selecteditem, int index)
        {
            OnItemSelect?.Invoke(this, selecteditem, index);
        }

        protected virtual void CheckboxChange(UIMenuCheckboxItem sender, bool Checked)
        {
            OnCheckboxChange?.Invoke(this, sender, Checked);
        }

        public enum MenuControls
        {
            Up,
            Down,
            Left,
            Right,
            Select
        }
    }
}

