﻿using System;
using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Native;
using Font = GTA.UI.Font;

namespace NativeUI.PauseMenu
{
    public class TabInteractiveListItem : TabItem
    {
        public TabInteractiveListItem(string name, IEnumerable<UIMenuItem> items) : base(name)
        {
            DrawBg = false;
            CanBeFocused = true;
            Items = new List<UIMenuItem>(items);
            IsInList = true;
            _maxItem = MaxItemsPerView;
            _minItem = 0;
        }

        public List<UIMenuItem> Items { get; set; }
        public int Index { get; set; }
        public bool IsInList { get; set; }
        protected const int MaxItemsPerView = 15;
        protected int _minItem;
        protected int _maxItem;
        //private bool _focused;
        
        public void MoveDown()
        {
            Index = (1000 - (1000 % Items.Count) + Index + 1) % Items.Count;

            if (Items.Count <= MaxItemsPerView) return;

            if (Index >= _maxItem)
            {
                _maxItem++;
                _minItem++;
            }

            if (Index == 0)
            {
                _minItem = 0;
                _maxItem = MaxItemsPerView;
            }
        }

        public void MoveUp()
        {
            Index = (1000 - (1000 % Items.Count) + Index - 1) % Items.Count;

            if (Items.Count <= MaxItemsPerView) return;

            if (Index < _minItem)
            {
                _minItem--;
                _maxItem--;
            }

            if (Index == Items.Count - 1)
            {
                _minItem = Items.Count - MaxItemsPerView;
                _maxItem = Items.Count;
            }
        }

        public void RefreshIndex()
        {
            Index = 0;
            _maxItem = MaxItemsPerView;
            _minItem = 0;
        }


        public override void ProcessControls()
        {
            if (!Visible) return;
            if (JustOpened)
            {
                JustOpened = false;
                return;
            }

            if (!Focused) return;

            if (Items.Count == 0) return;


            if (Game.IsControlJustPressed(Control.FrontendAccept) && Focused && Items[Index] is UIMenuCheckboxItem)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                ((UIMenuCheckboxItem) Items[Index]).Checked = !((UIMenuCheckboxItem) Items[Index]).Checked;
                ((UIMenuCheckboxItem)Items[Index]).CheckboxEventTrigger();
            }
            else if (Game.IsControlJustPressed(Control.FrontendAccept) && Focused)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                Items[Index].ItemActivate(null);
            }

            if (Game.IsControlJustPressed(Control.FrontendLeft) && Focused && Items[Index] is UIMenuListItem)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                ((UIMenuListItem)Items[Index]).ListChangedTrigger(--((UIMenuListItem)Items[Index]).Index);
            }

            if (Game.IsControlJustPressed(Control.FrontendRight) && Focused && Items[Index] is UIMenuListItem)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                ((UIMenuListItem)Items[Index]).ListChangedTrigger(++((UIMenuListItem)Items[Index]).Index);
            }

            if (Game.IsControlJustPressed(Control.FrontendUp) || Game.IsControlJustPressed(Control.MoveUpOnly) || Game.IsControlJustPressed(Control.CursorScrollUp))
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                MoveUp();
            }

            else if (Game.IsControlJustPressed(Control.FrontendDown) || Game.IsControlJustPressed(Control.MoveDownOnly) || Game.IsControlJustPressed(Control.CursorScrollDown))
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                MoveDown();
            }
        }

        public override void Draw()
        {
            if (!Visible) return;
            base.Draw();

            var res = UIMenu.GetScreenResolutionMantainRatio();

            var alpha = Focused ? 120 : 30;
            var blackAlpha = Focused ? 200 : 100;
            var fullAlpha = Focused ? 255 : 150;

            var submenuWidth = (BottomRight.X - TopLeft.X);
            var itemSize = new Size(submenuWidth, 40);

            int i = 0;
            for (int c = _minItem; c < Math.Min(Items.Count, _maxItem); c++)
            {
                var hovering = UIMenu.IsMouseInBounds(SafeSize.AddPoints(new Point(0, (itemSize.Height + 3)*i)),
                    itemSize);

                var hasLeftBadge = Items[c].LeftBadge != UIMenuItem.BadgeStyle.None;
                var hasRightBadge = Items[c].RightBadge != UIMenuItem.BadgeStyle.None;

                var hasBothBadges = hasRightBadge && hasLeftBadge;
                var hasAnyBadge = hasRightBadge || hasLeftBadge;

                new UIResRectangle(SafeSize.AddPoints(new Point(0, (itemSize.Height + 3) * i)), itemSize, (Index == c && Focused) ? Color.FromArgb(fullAlpha, Color.White) : Focused && hovering ? Color.FromArgb(100, 50, 50,50) : Color.FromArgb(blackAlpha, Color.Black)).Draw();
                new UIResText(Items[c].Text, SafeSize.AddPoints(new Point((hasBothBadges ? 60 : hasAnyBadge ? 30 : 6), 5 + (itemSize.Height + 3) * i)), 0.35f, Color.FromArgb(fullAlpha, (Index == c && Focused) ? Color.Black : Color.White)).Draw();

                if (hasLeftBadge && !hasRightBadge)
                {
                    new Sprite(UIMenuItem.BadgeToSpriteLib(Items[c].LeftBadge),
                        UIMenuItem.BadgeToSpriteName(Items[c].LeftBadge, (Index == c && Focused)), SafeSize.AddPoints(new Point(-2, 1 + (itemSize.Height + 3) * i)), new Size(40, 40), 0f,
                        UIMenuItem.BadgeToColor(Items[c].LeftBadge, (Index == c && Focused))).Draw();
                }

                if (!hasLeftBadge && hasRightBadge)
                {
                    new Sprite(UIMenuItem.BadgeToSpriteLib(Items[c].RightBadge),
                        UIMenuItem.BadgeToSpriteName(Items[c].RightBadge, (Index == c && Focused)), SafeSize.AddPoints(new Point(-2, 1 + (itemSize.Height + 3) * i)), new Size(40, 40), 0f,
                        UIMenuItem.BadgeToColor(Items[c].RightBadge, (Index == c && Focused))).Draw();
                }

                if (hasLeftBadge && hasRightBadge)
                {
                    new Sprite(UIMenuItem.BadgeToSpriteLib(Items[c].LeftBadge),
                        UIMenuItem.BadgeToSpriteName(Items[c].LeftBadge, (Index == c && Focused)), SafeSize.AddPoints(new Point(-2, 1 + (itemSize.Height + 3) * i)), new Size(40, 40), 0f,
                        UIMenuItem.BadgeToColor(Items[c].LeftBadge, (Index == c && Focused))).Draw();

                    new Sprite(UIMenuItem.BadgeToSpriteLib(Items[c].RightBadge),
                        UIMenuItem.BadgeToSpriteName(Items[c].RightBadge, (Index == c && Focused)), SafeSize.AddPoints(new Point(25, 1 + (itemSize.Height + 3) * i)), new Size(40, 40), 0f,
                        UIMenuItem.BadgeToColor(Items[c].RightBadge, (Index == c && Focused))).Draw();
                }

                if (!string.IsNullOrEmpty(Items[c].RightLabel))
                {
                    new UIResText(Items[c].RightLabel,
                        SafeSize.AddPoints(new Point(BottomRight.X - SafeSize.X - 5, 5 + (itemSize.Height + 3)*i)),
                        0.35f, Color.FromArgb(fullAlpha, (Index == c && Focused) ? Color.Black : Color.White),
                        Font.ChaletLondon, UIResText.Alignment.Right).Draw();
                }

                if (Items[c] is UIMenuCheckboxItem)
                {
                    string textureName = "";
                    if (c == Index && Focused)
                    {
                        textureName = ((UIMenuCheckboxItem)Items[c]).Checked ? "shop_box_tickb" : "shop_box_blankb";
                    }
                    else
                    {
                        textureName = ((UIMenuCheckboxItem)Items[c]).Checked ? "shop_box_tick" : "shop_box_blank";
                    }
                    new Sprite("commonmenu", textureName, SafeSize.AddPoints(new Point(BottomRight.X - SafeSize.X - 60, -5 + (itemSize.Height + 3) * i)), new Size(50, 50)).Draw();
                }

                if (Focused && hovering && Game.IsControlJustPressed(Control.CursorAccept))
                {
                    bool open = Index == c;
                    Index = (1000 - (1000 % Items.Count) + c) % Items.Count;
                    if (!open)
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                    else
                    {
                        if (Items[Index] is UIMenuCheckboxItem)
                        {
                            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                            ((UIMenuCheckboxItem) Items[Index]).Checked = !((UIMenuCheckboxItem) Items[Index]).Checked;
                            ((UIMenuCheckboxItem) Items[Index]).CheckboxEventTrigger();
                        }
                        else
                        {
                            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", 1);
                            Items[Index].ItemActivate(null);
                        }
                    }
                }

                i++;
            }
        }
    }
}