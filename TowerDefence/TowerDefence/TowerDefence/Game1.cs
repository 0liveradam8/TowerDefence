using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using C3.XNA;
using Maki;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TowerDefence
{
    /// <summary>
    /// The main base for the game, where all other code is called from.
    /// </summary>
    [Serializable]// Marks a class so that it can be serialised and stored or sent over a network.
    public class TowerDefenceGame : Game
    {
    public static Game1 GameDataInstance;// The instance of the game data.
    public TowerDefenceGame()
    {
        GameDataInstance = new Game1(this);// Creates a new instance of the game data, with this instance as the base for the game.
    }
    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
        GameDataInstance.LoadContent();// Loads all of the content for the game, and instantiates the initial instances.
        base.LoadContent();// Line included by default.
    }
    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
        GameDataInstance.Update(gameTime);// Updates all of the instances in the game each frame, at approximately 60 times per second.
        base.Update(gameTime);// Line included by default.
    }
    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
        GameDataInstance.Draw(gameTime);// Draws all of the Instances and the background.
        base.Draw(gameTime);// Line included by default
    }
    protected override void OnExiting(object sender, EventArgs args)// When the game closes, by any means
    {
        Game1.Multiplayer.Disconnect();// End all multiplayer connections
        base.OnExiting(sender, args);// Executes any underlying commands to run when closing the game.
    }
    }


    [Serializable]
    public class Instance// The base type for almost all objects which are updated and drawn to the screen.
    {
        public double RadianRotation;// The rotation of an Instance, in radians, where 0 is pointing directly right.
        public double X;// The X position of an Instance.
        public double Y;// The Y position of an Instance.
        // Although the collision boxes of Instances can only have an integer position and size, and Instances can only be drawn to the screen using integers as positions,
        // the coordinates of the Instances must be able to take on decimal values so that Instances can move at a speed less than one pixel per frame.
        public bool Destroyed;// If true, an Instance has already been removed from being drawn or updated. By validating whether an Instance has been destroyed, it ensures that
        // an Instance is only destroyed once, so the OnDestroy method is only called once and the program does not attempt to remove the Instance from the Game1.Rooms
        // twice.
        public string Sprite;// The key of the Texture2D in the game data instance sprite list that is drawn to the screen for this instance.
        public Rectangle SourceBox;// The portion of the Instance's Texture2D that will be drawn to the screen to represent this Instance.
        public string InstanceName = "";// The key of this Instance in Game1.Rooms.
        public int Room;// The index of the dictionary in Game1.Rooms that this Instance is present in. If it is equal to Game1.Room, this Instance will be drawn and updated.
        public double Speed;// The number of pixels per frame this Instance will move.
        public Rectangle CollisionBox;// The rectangle that is used to detect collisions between Instances.
        public Color Colour = Color.White;// The colour of the tint applied to this Instance when drawn to the screen.
        public bool IsEnemy,// If true, this Instance is an Enemy.
            IsTower;// If true, this Instance is a Tower.
        // It is more efficient and easier to use IsEnemy and IsTower to detect if an Instance is one of these than to type *.GetType().BaseType == typeof(Tower).
        public int Damage;// The amount of damage this Instance will deal.
        public Game1.E Element;// The element of this Instance; used for calculating damage dealt.
        /// <summary>
        /// The main constructor for creating an Instance, as it automatically adds it to the game.
        /// </summary>
        /// <param name="spriteName">The key of the Texture2D in Game1.SpriteList to draw to the screen.</param>
        /// <param name="aX">The x position of this Instance.</param>
        /// <param name="aY">The y position of this Instance.</param>
        /// <param name="room">The room this Instance is present in.</param>
        public Instance(String spriteName, double aX, double aY, int room = 0)
        {
            Sprite = spriteName;// Sets the sprite key to the parameter.
            CollisionBox = new Rectangle((int)X - (Game1.SpriteList[Sprite].Width / 2), (int)Y - (Game1.SpriteList[Sprite].Height / 2), Game1.SpriteList[Sprite].Width, Game1.SpriteList[Sprite].Height);
            // By default, the collision box has a width and height of the width and height of the Texture2D that Sprite is the key for in Game1.SpriteList. The origin of
            // the collision box is in the centre of the Texture2D.
            SourceBox = new Rectangle(0, 0, Game1.SpriteList[Sprite].Width, Game1.SpriteList[Sprite].Height);
            // By default, the entire Texture2D will be drawn to the screen.
            X = aX;// Sets the x position of this Instance to the parameter.
            Y = aY;// Sets the y position of this Instance to the parameter.
            InstanceName = ((Game1.TheGame.NextInstanceId).ToString() + GetType());
            // The key generated for this Instance is a number that increments by one for each object created, followed by the type of this object as a string.
            Game1.TheGame.NextInstanceId += 1;// Increments the next id number by one so the next Instance definitely has a unique key.
            Game1.TheGame.ToBeCreated.Add(this);// Adds this Instance to the list of Instances to be created, so that it is updated and drawn.
            Room = room;// Holds the room that this Instance will be added to.
        }
        public Instance()// An empty constructor for any Instance that is created differently.
        {
        }
        /// <summary>
        /// This Update method is called once per frame and contains any processing of the Instance.
        /// </summary>
        public virtual void Update()
        {
            // Refreshes the collision box of the Instance so that it stays updated when the Instance moves or changes size.
            CollisionBox = new Rectangle((int)X - (Game1.SpriteList[Sprite].Width/2), (int)Y - (Game1.SpriteList[Sprite].Height/2), CollisionBox.Width, CollisionBox.Height);
            // Any Instance that goes outside the room is destroyed.
            if (X < 0 || X > Game1.TheGame.GameWidth || Y < 0 || Y > Game1.TheGame.GameHeight)
            {
                DestroyInstance();
            }
        }
        /// <summary>
        /// Anything that an Instance requires to be shown on screen will be added here.
        /// </summary>
        public virtual void Draw()
        {
            // Draws the Texture2D in the Game1.SpriteList using Sprite as the key to the screen.
            Game1.SpriteBatch.Draw(Game1.SpriteList[Sprite], // This is the Texture2D that is drawn.
                new Rectangle((int)X,(int)Y,CollisionBox.Width,CollisionBox.Height),// This is the position and size that the Texture2D will be drawn at. It is the size of the CollisionBox
                SourceBox,// This is the portion of the Texture2D that will be drawn.
                Colour,// This is the colour of the tint that will be applied to the Instance.
                (float)RadianRotation,// This is the rotation of the Texture2D when it is drawn.
                new Vector2(Game1.SpriteList[Sprite].Width/2f,Game1.SpriteList[Sprite].Height/2f),// This shifts the position where the Instance is drawn so that the centre of it
                // is in the centre of the Texture2D drawn. This also ensures that the Texture2D is rotated around its centre.
                SpriteEffects.None,// The Texture2D is not flipped.
                0);// The depth of all Instances is the same.
        }
        /// <summary>
        /// This is a subroutine that allows child classes to run code when they're destroyed. It will only run once per Instance, unless the Destroyed bool is tampered with.
        /// </summary>
        public virtual void OnDestroy()
        {
        }
        /// <summary>
        /// This is called when an Instance need to be removed from being updated and drawn. It ensures that Instances are only destroyed once, and that the game does not error by
        /// having Instances removed directly from Game1.Rooms.
        /// </summary>
        /// <returns>Returns true if the Instance has not already been destroyed.</returns>
        public bool DestroyInstance()
        {
            if (Destroyed == false)// If the Instance has not already been destroyed.
            {
                Destroyed = true;// Mark it as having been destroyed.
                Game1.TheGame.ToBeDestroyed.Add(this);// Adds it to a List, so when all the Instances have finished being updated this frame, it will be removed.
                OnDestroy();// Calls the subroutine in case any child classes want to run code when an Instance is destroyed.
                return true;
            }
            return false;// Returns false if this Instance has already been destroyed.
        }
        /// <summary>
        /// Moves an Instance towards a point, by changing its X and Y coordinates.
        /// </summary>
        /// <param name="targetPoint">The point that this Instance will move towards.</param>
        /// <param name="movingspeed">The number of pixels that this Instance will travel.</param>
        public void move_towards_point(Vector2 targetPoint, Double movingspeed = 1)
        {
            double xDiff = (X - targetPoint.X);// Calculates the difference in the positions between this Instance and the target point.
            double yDiff = (Y - targetPoint.Y);
            double length = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);// Uses pythagorus' theorem to calculate the length of the line between the two points.
            if (length != 0d)
            {
                X -= (xDiff / length) * movingspeed;// Calculates the ratio of how much of the distance will be covered horizontally and then multiplies it by the distance that
                // will be travlled.
                Y -= (yDiff / length) * movingspeed;// Calculates the ratio of how much of the distance will be covered vertically and then multiplies it by the distance that
                // will be travelled.
                // The sum of the distance travelled vertically and horizontally is the same as movingspeed.
            }
        }
        /// <summary>
        /// Moves an Instance towards another Instance.
        /// </summary>
        /// <param name="target">The target instance to move towards.</param>
        /// <param name="movingspeed">The number of pixels that this Instance will travel.</param>
        public void MoveTowards(Instance target, Double movingspeed = 1)
        {
            move_towards_point(new Vector2((float) target.X, (float) target.Y), movingspeed);
            // Moves towards the point at which the target is located.
        }
        /// <summary>
        /// Detects if there is a collision between two Instances, by checking if their collision boxes intersect.
        /// </summary>
        /// <param name="target">The target Instance that will have its collision box checked for collision with this Instance.</param>
        /// <returns>Returns true if there is a collision.</returns>
        public bool Collision(Instance target)
        {
            if (CollisionBox.Intersects(target.CollisionBox))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Moves an Instance in a specified direction, by changing its X and Y coordinates,
        /// </summary>
        /// <param name="radianAngle">The direction in which the Instance will move.</param>
        /// <param name="movingspeed">The number of pixels that this Instance will travel.</param>
        public void MoveAtAngle(double radianAngle, double movingspeed = 1)
        {
            Vector2 direction = new Vector2((float)Math.Cos(radianAngle),
                                            (float)Math.Sin(radianAngle));
            // Converts the angle parameter to give the direction as a point, rather than a direction, which can easily be approached by the Instance.
            direction.Normalize();
            // Normalises the resultant Vector2, so that it always has a length of 1, so truly only represents a ratio between x and y movement, rather than a distance too.
            X += direction.X * movingspeed;
            Y += direction.Y * movingspeed;
            // Uses the ratio provided by the Vector2 to change the X and Y coordinates of this Instance, multiplying by the distance specified by the parameter, to move the Instance by that distance.
        }
        /// <summary>
        /// Gets a List of all the Instances that this Instance's collision box is intersecting with.
        /// </summary>
        /// <returns>Returns a List of Instances that this Instance is colliding with.</returns>
        public List<Instance> GetColliders()
        {
            List<Instance> returnColliders = new List<Instance>();// A List of Instances that this Instance is colliding with, to be populated.
            // A for loop is used instead of a foreach loop as many sources state it can be more efficient.
            for (int i = 0; i < Game1.TheGame.Rooms[Room].Count; i += 1)// For each index of all of the Instances in this Instance's room.
            {
                if (Collision(Game1.TheGame.Rooms[Room].ElementAt(i).Value))// If there is a collision between this Instance and the Instance with an index of this Instance's room at i.
                {
                    returnColliders.Add(Game1.TheGame.Rooms[Room].ElementAt(i).Value);// Add the Instance at the index of i in this Instance's room to the List of Instances that
                    // this Instance is colliding with.
                }
            }
            return returnColliders;// Returns the List of Instances that this Instance is colliding with.
        }
        /// <summary>
        /// Gets the Instance with the least Euclidean distance from this Instance.
        /// </summary>
        /// <param name="requireEnemy">If true, the nearest Enemy is return instead.</param>
        /// <returns>Returns the nearest Instance.</returns>
        public Instance GetNearest(bool requireEnemy = false)
        {
            try// Ensures that if there is no nearest Instance, then the game does not error from returning null.
            {
                Instance returnNearest = null;// By default, no other Instance has been found.
                int distance = int.MaxValue;// The initial distance is very high so that the first eligible Instance will be set as the returnNearest.
                foreach (Instance i in Game1.TheGame.Rooms[Room].Values)// Iterates through each Instance in this Instance's room.
                {
                    int iteratedDistance = (int)GetDistance(i);// Calculates the distance between this Instance and the currently iterated Instance.
                    if (iteratedDistance < distance)// If the currently iterated Instance is closer than the closest found Instance so far.
                    {
                        if (i != this)// If the currently iterated Instance is not this Instance (as this Instance will always be the closest).
                        {
                            if (i.IsEnemy || requireEnemy == false)// If the nearest Instance does not have to be an enemy or it is an Enemy.
                            {
                                returnNearest = i;// Get the new closest Instance found so far.
                                distance = iteratedDistance;// Get the new shortest distance, which is the distance to the currently iterated Instance.
                            }
                        }
                    }
                }
                return returnNearest;// Returns the nearest Instance.
            }
            catch (NullReferenceException)// If no other Instance has been found.
            {
                return null;// Return null, as no Instance can be found.
            }
        }
        /// <summary>
        /// Gets the Euclidean distance between this Instance and another Instance.
        /// </summary>
        /// <param name="firstInstance">The Instance to find the distance to.</param>
        /// <returns>Returns a decimal of the exact distance between this Instance and the specified Instance.</returns>
        public double GetDistance(Instance firstInstance)
        {
            // Returns the distance between this Instance's X and Y coordinates and the specified Instance's X and Y coordinates.
            return GetDistance(firstInstance.X, firstInstance.Y);
        }
        /// <summary>
        /// Gets the Euclidean distance between this Instance and a point.
        /// </summary>
        /// <param name="targetX">The x component of the point to find the distance to.</param>
        /// <param name="targetY">The y component of the point to find the distance to.</param>
        /// <returns>Returns a decimal of the exact distance between this Instance and the specified point.</returns>
        public double GetDistance(double targetX, double targetY)
        {
            double xDist = (X - targetX);// Calculates the distance between the X components of this Instance's X coordinate and the target X coordinate of the specified point.
            double yDist = (Y - targetY);// Calculates the distance between the Y components of this Instance's Y coordinate and the target Y coordinate of the specified point.
            // Uses pythagorus' theorem to find the exact distance between the two points, and returns the result.
            return (Math.Sqrt((xDist * xDist) + (yDist * yDist)));
        }
        /// <summary>
        /// Gets the direction that the specified target is in.
        /// </summary>
        /// <param name="target">The Instance to get the relative direction of.</param>
        /// <returns>Returns the relative direction, in radians, in which the specified Instance is in.</returns>
        public double PointDirection(Instance target)
        {
            return PointDirection(X,Y,target.X,target.Y);
        }
        /// <summary>
        /// Get the direction from the coordinates (x1,y1) to the coordinates (x2,y2).
        /// </summary>
        /// <param name="x1">The first X coordinate.</param>
        /// <param name="y1">The first Y coordinate.</param>
        /// <param name="x2">The second X coordinate.</param>
        /// <param name="y2">The second Y coordinate.</param>
        /// <returns>Returns the direction, in radians, from the first coordinates to the second coordinates.</returns>
        public double PointDirection(double x1, double y1, double x2, double y2)
        {
            double xDiff = x2 - x1;// Gets the difference between the x components of the two coordinates.
            double yDiff = y2 - y1;// Gets the difference between the y components of the two coordinates.
            return Math.Atan2(yDiff, xDiff);// Uses the differences between the coordinates to calculate the direction of the second coordinates relative to the first, and returns the result.
        }
        /// <summary>
        /// Gets the chance of a set number to be selected from any number between 0 and the specified number, exclusive of the specified number.
        /// </summary>
        /// <param name="outOf">The probability of this method returning true is the reciprocal of this argument.</param>
        /// <returns>Randomly returns true, though true is more likely the lower outOf is.</returns>
        public static bool ChanceOneIn(int outOf)// For example, to chance getting a 6 on a fair dice, it would be ChanceOneIn(6).
        {
            if (outOf <= 0)// The upper bound must be higher than the lower bound, so false is returned by default.
            {
                return false;
            }
            int randomNumberOne = Game1.TheGame.Rand.Next(0, outOf);// Generates a random number from 0 (inclusive) to outOf (exclusive).
            if (randomNumberOne == 0)// If the random number is 0 (as 0 is included in the range regardless of outOf).
            {
                return true;// Return true.
            }
            return false;// Returns false if the random number generated was not 0.
        }
        /// <summary>
        /// Gets whether the left mouse button was up in the previous frame and down in this frame, and itd position is within this Instance.
        /// </summary>
        /// <returns>Returns true if the mouse is pressed and lies within this Instance's collision box.</returns>
        public bool MousePressedInMe()
        {
            if (CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y) && I.MousePressed(I.Mouses.Left))// If the mouse's position is within this Instance's
                // collision box and the left button has been pressed.
            {
                I.UpdateStates();// Updates the mouse state so that if instances overlap, only one of them return true for mouse pressed.
                // This is so a menu button on a subsequent room is not pressed as soon as the subsequent room is entered.
                return true;// Returns true if the if statement conditions are met.
            }
            return false;// Returns false if the left mouse button has not been pressed or it is not within this Instance's collision box.
        }
        /// <summary>
        /// Damages the specified Instance, with this Instance's damage.
        /// </summary>
        /// <param name="target">The Instance to damage.</param>
        public void DamageInstance(Enemy target)
        {
            Debug.WriteLine(InstanceName + ".DamageInstance(" + target.InstanceName + ")");
            Debug.WriteLine("{");
            Debug.WriteLine("    Game1.GameScreenTime" + Game1.TheGame.GameScreenTime);
            Debug.WriteLine("    Starting Health: " + target.CurrentHealth);
            Debug.WriteLine("    Damage: " + Damage);
            Debug.WriteLine("    Tower Element: " + Element);
            Debug.WriteLine("    Enemy Element: " + target.Element);
            target.CurrentHealth -= (int)(Damage * Game1.ElementCompare[(int)Element, (int)target.Element]);// Negates the target Instance's current health using
            // this Instance's damage and the multiplier from comparing whether the element of this Instance is effective or ineffective against the target's element.
            Debug.WriteLine("    New Health: " + target.CurrentHealth);
            Debug.WriteLine("}");
        }
        /// <summary>
        /// Damages the specified Instance, using a specified element for the multiplier, and specified damage.
        /// </summary>
        /// <param name="target">The target to damage.</param>
        /// <param name="element">The element to compare to the target's element.</param>
        /// <param name="damage">The amount of damage to inflict.</param>
        public static void DamageInstance(Enemy target, Game1.E element, int damage)
        {
            Debug.WriteLine("DamageInstance(" + target.InstanceName + ", " + element + ", " + damage + ")");
            Debug.WriteLine("{");
            Debug.WriteLine("    Game1.GameScreenTime" + Game1.TheGame.GameScreenTime);
            Debug.WriteLine("    Starting Health: " + target.CurrentHealth);
            Debug.WriteLine("    Enemy Element: " + target.Element);
            target.CurrentHealth -= (int)(damage * Game1.ElementCompare[(int)element, (int)target.Element]);// Negates the target Instance's current health using
            // the specified damage and the multiplier from comparing whether the specified element is effective or ineffective against the target's element.
            Debug.WriteLine("    New Health: " + target.CurrentHealth);
            Debug.WriteLine("}");
        }
    }

    [Serializable]
    public class UpgradeButton : Instance// When pressed, a tower's next upgrade occurs, at a price.
    {
        public bool Show;// If true, this button is shown and can be pressed.
        public Tower Caller;// The tower for which it is being shown for.

        public UpgradeButton(String spriteName, double aX, double aY, int room = 0) : base(spriteName,aX,aY,room)//Calls the default constructor.
        {
        }
        public override void Update()
        {
            if (Show)// If this button is visible.
            {
                if (MousePressedInMe())// If it has been pressed.
                {
                    if (Caller.Upgrades.Count > 0)// If the tower that it is shown for has upgrades left to be done.
                    {
                        if (Caller.Upgrades.Peek().TryUpgrade())// Attempt to upgrade the tower. If it can be upgraded then it is, and removes the upgrade from the towers queue
                            //so that it cannot recieve the same upgrade again. If the tower cannot be upgraded, due to lack of tower money, then the upgrade remains in the queue.
                        {
                            Caller.Upgrades.Dequeue();
                        }
                    }
                    else
                    {
                        Show = false;// If the tower doesn't have upgrades left to be done, then this button is hidden.
                    }
                }
                base.Update();
            }
        }
        public override void Draw()
        {
            if (Show)// Only draw this button if it is supposed to be shown and if the tower it is shown for has upgrades left.
            {
                if (Caller.Upgrades.Count > 0)
                {
                    base.Draw();
                    Game1.SpriteBatch.DrawString(Game1.Arial10, Caller.Upgrades.Peek().Cost + " - " + Caller.Upgrades.Peek().UpgradeMessage, new Vector2(4, 750), Color.White);
                    // Draw the cost and description of the next upgrade at the bottom of the screen.
                }
            }
        }
    }
    [Serializable]
    public class DeleteButton : Instance//When pressed, a tower is deleted.
    {
        public bool Show;// If true, this button is shown and can be pressed.
        public Tower Caller;// The tower for which it is being shown for.
        public DeleteButton(String spriteName, double aX, double aY, int room = 0) : base(spriteName,aX,aY,room)//Calls the default constructor.
        {   
        }
        public override void Update()
        {
            if (Show)// If this button is visible
            {
                if (MousePressedInMe())// If it has been pressed.
                {
                    Show = false;// Hide this button, so that the tower can no longer be deleted.
                    Game1.TheGame.Upgradebutton.Show = false;// Hide the upgrade button, towers cannot be upgraded after being deleted.
                    Game1.TheGame.TowerMoney += (int)(Caller.Cost * 0.9);// Partially refund the cost of the tower.
                    Caller.DestroyInstance();// Destroy the tower for which this button is showing for.
                }
                base.Update();
            }
        }
        public override void Draw()
        {
            if (Show)// Only draw this button if it is being shown.
            {
                base.Draw();
            }
        }
    }
    [Serializable]
    public class Castle : Instance// The user's castle the enemeis must reach to reduce the user's health.
    {
        // Calls the inherited constructor.
        public Castle(String spriteName, double aX, double aY, int room = 0) : base(spriteName, aX, aY, room)
        {
        }
        public override void Draw()
        {
            // Draws the user's current and max health above the Castle. It does this by first drawing a rectangle of a white pixel tinted dark greenstretched to represent the
            // max health the user can have, and then draws another rectangle on top, which is shorter when the user has less current health, which is tinted lime green.
            Game1.SpriteBatch.Draw(Game1.SpriteList["WhitePixel"], new Rectangle((int)(X - 32), (int)(Y - 64), 64, 8), Color.DarkGreen);
            Game1.SpriteBatch.Draw(Game1.SpriteList["WhitePixel"], new Rectangle((int)(X - 32), (int)(Y - 64), (Game1.TheGame.CurrentHealth * 64) / Game1.TheGame.MaxHealth, 8), Color.LimeGreen);
            base.Draw();
        }
    }
    [Serializable]
    public class TowerPlacer : Instance// Used to place towers at the mouse position.
    {
        public bool Show;// If true, draw and update this.
        public Type TowerType;// The type of tower that will be created.
        public string TowerSprite;// The sprite of the tower that will be created.
        public int TowerCost;// The cost of creating a tower of the tower type.
        // Calls the inherited constructor.
        public TowerPlacer(String spriteName, double aX, double aY, int room = 0) : base(spriteName, aX, aY, room)
        {

        }
        public override void Update()
        {
            if (Show)// If it is shown.
            {
                if (I.MousePressed(I.Mouses.Right))// Right clicking will cancel the placement of a tower.
                {
                    Show = false;
                }
                // These two if statements keep this Instance from leaving the playing area.
                // The code inside them snaps them to grid, with nodes spaced 32 pixels apart vertically and horizontally., starting from coordinate (16,16).
                // It does this by rounding the coordinates to the nearest multiple of 32 and adding 16.
                if (I.NewMouse.X > 32 && I.NewMouse.X < Game1.TheGame.PathMaxX - 32)
                {
                    X = 16 + I.NewMouse.X / 32 * 32;
                }
                if (I.NewMouse.Y > 32 && I.NewMouse.Y < Game1.TheGame.PathMaxY - 32)
                {
                    Y = 16 + I.NewMouse.Y / 32 * 32;
                }
                if (X < Game1.TheGame.PathMaxX - 32 && Y < Game1.TheGame.PathMaxY - 32)// If this Instance is within the game's boundaries.
                {
                    if (Game1.TheGame.Map[(int)X, (int)Y] ^ TowerType == typeof(Wall))// If this Instance is on an area of the map that can have a Wall placed on it, though the
                        // user is trying to place a non-wall tower, it is true. If the area can have any non-wall tower on it, though the user is trying to place a wall, it
                        // is also true. The user cannot place a tower in these cases.
                    {
                        Colour = Color.FromNonPremultiplied(255, 150, 150, 255);// Tint this Instance to be a negative red colour, 
                        // so that the user knows that they cannot place their tower here.
                    }
                    else// Otherwise, if the area of this Instance will allow for a tower to be placed.
                    {
                        Colour = Color.LightGreen;// Tint this Instance to be a positive green colour, so the user knows that they can place a tower.
                        if (I.MousePressed(I.Mouses.Left) && Game1.TheGame.TowerMoney >= TowerCost)// If the user has pressed the mouse down and has enough money to place a tower.
                        {
                            if (new Rectangle(0,0,1088,768).Contains(I.NewMouse.X,I.NewMouse.Y) && !TowerAtPosition(false))// If the cursor is within the playing area and there
                                // is not already a non-wall tower at this Instance's location (Walls do not need to be excluded here as if there is already a wall present at
                                // this Instance's location it would've been filtered out by an earlier if statement).
                            {

                                object[] arguments = { TowerSprite, X, Y,0 };// Sets the arguments to create the tower with.
                                // Uses the Activator class to create a new tower with the user selected sprite/type, position, and in room 0.
                                Tower d = (Tower)Activator.CreateInstance(TowerType, arguments);
                                // Tells the created tower its cost so that it refunds the correct amount when deleted.
                                d.Cost = TowerCost;
                                // Reduces the user's tower money by the cost of the tower placed.
                                Game1.TheGame.TowerMoney -= TowerCost;
                            }
                            // Hides this tower placer.
                            Show = false;
                        }
                    }
                }
                base.Update();
            }
        }
        public override void Draw()
        {
            if (Show)// Only draw this Instance if it is meant to be shown.
            {
                base.Draw();
            }
        }
        /// <summary>
        /// Gets whether there is a tower already present at this Instance's position.
        /// </summary>
        /// <param name="IncludeWall">If false, then Walls will not count as Towers for this method.</param>
        /// <returns>Returns true if there is a Tower at this Instance's location.</returns>
        public bool TowerAtPosition(bool IncludeWall)
        {
            foreach (Instance i in Game1.TheGame.Rooms[Room].Values)// Iterates over each Instance in this Instance's room.
            {
                if (i.GetType().BaseType == typeof(Tower) && (IncludeWall || i.GetType() != typeof(Wall)))// If the currently iterated Instance is a Tower or Wall, and
                    // either walls are included or the currently iterated Instance is not a Wall.
                {
                    if (i.X == X && i.Y == Y)// If the currently Iterated Instance is at the location that the user is trying to place a Tower.
                    {
                        return true;// Return true; there is already a tower present at this location.
                    }
                }
            }
            return false;// Return false, these is no tower present at this location.
        }
    }
    [Serializable]
    public class TowerButton : Instance// When pressed, activates the TowerPlacer for the tower that the button relates to.
    {
        public Type TowerType;// The type of tower that this button represents.
        public string TowerSprite;// The sprite of the tower that will be created using this button.
        public int TowerCost;// The cost of the tower that will be created using this button.
        // Calls the inherited constructor.
        public TowerButton(String spriteName, double aX, double aY, Type tower, string towersprite, int cost, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            // Gets the type, sprite, and cost of the tower that will be created by this button.
            TowerType = tower;
            TowerSprite = towersprite;
            TowerCost = cost;
        }
        public override void Update()
        {
            if (MousePressedInMe())// If the cursor is in this button and the left mouse button is pressed.
            {
                if (Game1.TheGame.TowerMoney >= TowerCost)// If the user has sufficient tower money to build the tower that this button represents.
                {
                    Game1.TheGame.Towerplacer.Show = !Game1.TheGame.Towerplacer.Show;// Show the tower placer, and set its tower properties to correspond with the tower
                    // represented by this button.
                    Game1.TheGame.Towerplacer.Sprite = TowerSprite;
                    Game1.TheGame.Towerplacer.TowerType = TowerType;
                    Game1.TheGame.Towerplacer.TowerSprite = TowerSprite;
                    Game1.TheGame.Towerplacer.TowerCost = TowerCost;
                }
            }
            base.Update();
        }
        public override void Draw()
        {

            base.Draw();
            Game1.SpriteBatch.DrawString(Game1.Arial10, TowerCost.ToString(), new Vector2((float)X - 32, (float)Y - 32), Color.White); // Draws the cost of building the tower
            // represented by this button.
        }
    }

    [Serializable]
    public class EnemyButton : Instance// When pressed, sens an enemy to the other user.
    {
        public Type EnemyType;// The type of enemy created by this button.
        public string EnemySprite;// The sprite of the enemy created by this button.
        public int Cost;// The cost of creating the enemy represented by this button.
        // Calls the inherited constructor.
        public EnemyButton(String spriteName, double aX, double aY, Type enemy, string enemysprite, int enemyCost, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            // Gets the type, sprite, and cost of the enemy created by this button.
            EnemyType = enemy;
            EnemySprite = enemysprite;
            Cost = enemyCost;
        }
        public override void Update()
        {
            if (Game1.TheGame.IsMultiplayer)
            {
                if (MousePressedInMe())
                {
                    if (Game1.TheGame.EnemyMoney >= (int)(Cost * Game1.TheGame.EnemyMultiplier))
                    {
                        Game1.Multiplayer.SendObject(EnemyType + ";" + EnemySprite + ";" + Cost + ";" + Game1.TheGame.EnemyMultiplier);
                        Game1.TheGame.EnemyMoney -= (int)(Cost * Game1.TheGame.EnemyMultiplier);
                    }
                }
                base.Update();
            }
        }
        public override void Draw()
        {
            if (Game1.TheGame.IsMultiplayer)
            {
                base.Draw();
                Game1.SpriteBatch.DrawString(Game1.Arial10, (Cost * Game1.TheGame.EnemyMultiplier).ToString(), new Vector2((float)X - 32, (float)Y - 32), Color.Red);
            }
        }
    }
    [Serializable]
    public class TextBox : Instance// Allows the user to input a string.
    {
        public bool Focused;// The user can only type in one TextBox at a time, which is the Focused TextBox.
        public string TextBoxString = "";// Initially, TextBoxes are empty.
        public int Interval;// The number of frames that must pass to type repeat character in a TextBox.
        // This is very similar to calling the inherited constructor, except that the TextBox is only added to Game1.Rooms if it is specified that it should. This is because
        // In some cases the TextBoxes are given specific Instance names, to be explicitly referenced.
        public TextBox(String spriteName, double aX, double aY,int room, bool addToInstances = true)
        {
            Sprite = spriteName;
            CollisionBox = new Rectangle((int)aX, (int)aY, Game1.SpriteList[Sprite].Width, Game1.SpriteList[Sprite].Height);
            SourceBox = new Rectangle(0, 0, Game1.SpriteList[Sprite].Width, Game1.SpriteList[Sprite].Height);
            X = aX;
            Y = aY;
            Room = room;
            if (addToInstances)
            {
                InstanceName = ((Game1.TheGame.NextInstanceId).ToString() + GetType());
                Game1.TheGame.ToBeCreated.Add(this);
                Game1.TheGame.NextInstanceId++;
            }
        }
        public override void Update()
        {
            TryGetFocus();// Checks if this TextBox should be focused.
            Interval -= 1;// Reduce the Interval for repeat characters by one, so that a character can be repeated after a moderate amount of time.
            if (Focused)// If this TextBox has focus.
            {
                if (I.NewKeyboard.GetPressedKeys().Length > 0)// If any keys on the keyboard are down.
                {
                    Keys i = I.NewKeyboard.GetPressedKeys().Last();// Get the key that is most recently down.
                    if ((int)i > 32 && (int)i <= 126)// If the key is from a specific set of typable characters, such as letters and numbers.
                    {
                        if (Interval <= 0 || i.ToString().Last() != TextBoxString.Last())// If the interval between character presses has elapsed or
                            // the user is pressing a different key.
                        {
                            TextBoxString += i.ToString().Last();// Append the key pressed to the text box.
                            Interval = 9;// Set an interval between key presses.
                        }
                    }
                    else if (i == Keys.Back && TextBoxString.Length > 0)// If there is text in the text box and the user presses backspace.
                    {
                        if (Interval <= 0)// If the interval has elapsed.
                        {
                            TextBoxString = TextBoxString.Substring(0, TextBoxString.Length - 1);// Removes one character from the end of the text box.
                            Interval = 9;// Set an interval between key presses.
                        }
                    }
                    else if (i == Keys.OemPeriod)// If the user presses the . key (the . key cannot be detected unless explicitly tested for in an if statement)
                    {
                        if (Interval <= 0)// If the interval has elapsed.
                        {
                            TextBoxString += ".";// Appends a full stop to the text box.
                            Interval = 9;// Set an interval between key presses.
                        }
                    }
                }
            }
            base.Update();
        }
        /// <summary>
        /// Checks if this text box should get focus.
        /// </summary>
        public void TryGetFocus()
        {
            if (MousePressedInMe())// If the user has left clicked inside this Instance.
            {
                foreach (Instance i in Game1.TheGame.Rooms[Room].Values)// Iterates over each Instance in this Instance's room.
                {
                    if (i.GetType() == typeof(TextBox))// If the currently iterated Instance is a text box.
                    {
                        ((TextBox)i).Focused = false;// Sets all other text boxes as not focused.
                    }
                }
                Focused = true;// Sets this TextBox as focused.
            }
        }
        public override void Draw()
        {
            base.Draw();
            // Draws the string that is written in this text box.
            Game1.SpriteBatch.DrawString(Game1.Arial10, TextBoxString, new Vector2((int)X - (Game1.SpriteList[Sprite].Width / 2) + 3, (int)Y - (Game1.SpriteList[Sprite].Height / 2) + 3), Color.Black);
        }
    }


    [Serializable]
    public abstract class Enemy : Instance// The base class for all enemies.
    {
        public int NextNodeIndex;// The index of the node that this Enemy is approaching.
        public int SuctionTimeout;// A number to determine the interval between stuns and pulls on this Enemy.
        public int Cost;// The cost of creating this Enemy.
        public int StunTime;// The number of frames that must pass before this Enemy is no longer stunned.
        public int PoisonTime;// The number of frames that must pass before this Enemy is no longer poisoned.
        public double IncomingEnemyMultiplier;// The difficulty of an enemy. Increasing it increases the cost, reward, speed, and health of an Enemy.
        public int CurrentHealth;// The amount of damage that must be inflicted on this Enemy before it is destroyed.
        public int MaxHealth;// The initial CurrentHealth of this Enemy.
        // Calls the inherited constructor.
        protected Enemy(String spriteName, double aX, double aY, int room, double multiplier) : base(spriteName,aX,aY,room)
        {
            IsEnemy = true;// Marks this Instance so it can easily be determined that this is an Enemy.
            IncomingEnemyMultiplier = multiplier;// Gets the multiplier parameter.
        }
        public override void Update()
        {
            if (CurrentHealth <= 0)// If this Enemy has run out of health.
            {
                DestroyInstance();// Destroy this Enemy.
                Game1.TheGame.EnemyMoney += (int)(Cost * 1.1 * IncomingEnemyMultiplier);// Give the user more enemy money than the cost of this Enemy (so that users can
                // progressively send more difficult enemies to each other).
                Game1.TheGame.TowerMoney += (int)(Cost * 0.1 * IncomingEnemyMultiplier);// Give the user some tower money, also related to the cost of this Enemy.
            }
            // If an Enemy has a timeout less than 59, it is being or has recently been pulled. If the timeout is 59 or 60, it can be pulled or stunned. If it is greater than 60,
            // it has been pulled excessively so cannot be pulled or stunned until the timeout reduces back to 60.
            if (SuctionTimeout <= 0)// If this Enemy has been pulled excessively.
            {
                SuctionTimeout = 240;// Set the timeout so that this Enemy cannot be pulled for another 180 frames.
            }
            else if (SuctionTimeout < 60)// If this Enemy is being or has recently been pulled.
                // This statement is here so that if an Enemy has only been pulled for 20 of the 60 available frames, for example, it will retain the 40 remaining frames that it
                // can be pulled for. It also stops the timeout passively decreasing.
            {
                SuctionTimeout += 1;// Increase the the timeout by 1.
            }
            if (PoisonTime > 0)// If this Enemy is poisoned.
            {
                DamageInstance(this, Element, (int)(MaxHealth * 0.001));// Damage this Enemy for a portion of its maximum health, using this Enemy's element so that the damage from
                // poison is fixed regardless of the Enemy's element.
                PoisonTime -= 1;// Decreases the time remaining that this Enemy will spend poisoned.
            }
            SuctionTimeout -= 1;// Decreases the timeout until this Enemy can be pulled by 1.
            base.Update();
        }
        /// <summary>
        /// Moves the Enemy along the path, towards the next node, at this Enemy's speed.
        /// </summary>
        protected void FollowPath()
        {
            if (StunTime <= 0)// If this Enemy has not been stunned by a LaserBeam.
            {
                if (NextNodeIndex >= Game1.TheGame.Path.Count)// If this Enemy has reached the user's Castle.
                {
                    if (DestroyInstance())// If this Enemy has not been destroyed from any other source; destroy this Enemy.
                    {
                        Game1.TheGame.CurrentHealth -= 100;// Reduce the user's health by 100.
                        Game1.Multiplayer.SendObject("Earn;" + ((int)Math.Round(Cost * 0.65 * IncomingEnemyMultiplier)) + ";" + ((int)Math.Round(Cost * 0.1 * IncomingEnemyMultiplier)));
                        // If the other user has managed to get an Enemy to this user's base, they recieve money as a reward.
                    }
                }
                else// If this Enemy is still following the path.
                {
                    move_towards_point(new Vector2(Game1.TheGame.Path[NextNodeIndex].X, Game1.TheGame.Path[NextNodeIndex].Y), Speed);
                    // Move towards the next node in the path, at this Enemy's speed.
                    if (GetDistance(Game1.TheGame.Path[NextNodeIndex].X, Game1.TheGame.Path[NextNodeIndex].Y) < Math.Ceiling(Speed))
                        // If this Enemy is within one frame of the user's base. It compares the distance to this Enemy's speed as if it used a static amount the amount would
                        // be too large for slow enemies, as they wouldn't be near their node when moving on to the next, or it would be too small for fast enemies, in which
                        // case they may get stuck at nodes.
                    {
                        NextNodeIndex += 1;// Increases the node index, so that this Enemy will follow the next node in the path.
                    }
                }
            }
            else
            {
                StunTime -= 1;
            }
        }
        public override void Draw()
        {
            base.Draw();
            // Draws the health bars, much in the same way that the health bars in Castle are drawn.
            Game1.SpriteBatch.Draw(Game1.SpriteList["WhitePixel"], new Rectangle((int)(X - 16), (int)(Y - 24), 32, 8), Color.DarkGreen);
            Game1.SpriteBatch.Draw(Game1.SpriteList["WhitePixel"], new Rectangle((int)(X - 16), (int)(Y - 24), (CurrentHealth*32) / MaxHealth, 8), Color.LimeGreen);
            
        }
    }

    [Serializable]
    public class WaterEnemy : Enemy
    {
        // Calls the inherited constructor. Also sets the speed, element, and health of this type of Enemy. The health and speed are set to correspond with the multiplier.
        public WaterEnemy(String spriteName, double aX, double aY, int room, double multiplier)
            : base(spriteName, aX, aY, room, multiplier)
        {
            Element = Game1.E.Water;
            CurrentHealth = (int)(3250.0 * IncomingEnemyMultiplier);
            MaxHealth = CurrentHealth;
            Speed = 0.5+(0.4 * IncomingEnemyMultiplier);
        }
        public override void Update()
        {
            FollowPath();// This Enemy follows the path, and has no additional effects.
            base.Update();
        }
    }
    [Serializable]
    public class Orb : Enemy
    {
        // Calls the inherited constructor. Also sets the speed, element, and health of this type of Enemy. The health and speed are set to correspond with the multiplier.
        public Orb(String spriteName, double aX, double aY, int room, double multiplier)
            : base(spriteName, aX, aY, room, multiplier)
        {
            Element = Game1.E.Light;
            CurrentHealth = (int)(1200.0*IncomingEnemyMultiplier);
            MaxHealth = CurrentHealth;
            Speed = 0.5+(0.9*IncomingEnemyMultiplier);
        }
        public override void Update()
        {
            FollowPath();// This Enemy follows the path, and has no additional effects.
            base.Update();
        }
    }
    [Serializable]
    public class NormalEnemy : Enemy
    {
        // Calls the inherited constructor. Also sets the speed, element, and health of this type of Enemy. The health and speed are set to correspond with the multiplier.
        public NormalEnemy(String spriteName, double aX, double aY, int room, double multiplier)
            : base(spriteName, aX, aY, room, multiplier)
        {
            Element = Game1.E.Normal;
            CurrentHealth = (int)(2750.0 * IncomingEnemyMultiplier);
            MaxHealth = CurrentHealth;
            Speed = 0.5 + (0.6 * IncomingEnemyMultiplier);
        }
        public override void Update()
        {
            FollowPath();// This Enemy follows the path, and has no additional effects.
            base.Update();
        }
    }
    [Serializable]
    public class FireballEnemy : Enemy
    {
        // Calls the inherited constructor. Also sets the speed, element, and health of this type of Enemy. The health and speed are set to correspond with the multiplier.
        public FireballEnemy(String spriteName, double aX, double aY, int room, double multiplier)
            : base(spriteName, aX, aY, room, multiplier)
        {
            Element = Game1.E.Fire;
            CurrentHealth = (int)(15000 * IncomingEnemyMultiplier);
            MaxHealth = CurrentHealth;
            Speed = 0.5 + (0.3 * IncomingEnemyMultiplier);
        }
        public override void Update()
        {
            if (NextNodeIndex > 0 && NextNodeIndex < Game1.TheGame.Path.Count)
            {
                RadianRotation = PointDirection((int)Game1.TheGame.Path[NextNodeIndex - 1].X, (int)Game1.TheGame.Path[NextNodeIndex - 1].Y, (int)Game1.TheGame.Path[NextNodeIndex].X, (int)Game1.TheGame.Path[NextNodeIndex].Y);
            }
            FollowPath();// This unit follows the path, and also points towards wherever it is travelling, which is from the previous node to the node it is attempting to reach.
            base.Update();
        }
    }
    [Serializable]
    public class WindEnemy : Enemy
    {
        // Calls the inherited constructor. Also sets the speed, element, and health of this type of Enemy. The health and speed are set to correspond with the multiplier.
        public WindEnemy(String spriteName, double aX, double aY, int room, double multiplier)
            : base(spriteName, aX, aY, room, multiplier)
        {
            Element = Game1.E.Wind;
            CurrentHealth = (int)(900.0 * IncomingEnemyMultiplier);
            MaxHealth = (int)(900.0 * IncomingEnemyMultiplier);
            switch (Game1.TheGame.Rand.Next(0,4))// Selects one of the four case statements at random. Each case statement declares a side of the map this Enemy will be created
                    // on.
            {
                case 0:
                    X = Game1.TheGame.Rand.Next(1,1088);
                    Y = 1;
                    break;
                case 1:
                    X = Game1.TheGame.Rand.Next(1,1088);
                    Y = 767;
                    break;
                case 2:
                    X = 1;
                    Y = Game1.TheGame.Rand.Next(1,768);
                    break;
                case 3:
                    X = 1087;
                    Y = Game1.TheGame.Rand.Next(1, 768);
                    break;
            }
            Speed = 0.2 + (0.55 * IncomingEnemyMultiplier)*(GetDistance((int)Game1.TheGame.EndPos.X,(int)Game1.TheGame.EndPos.Y) / (double)(Game1.TheGame.PathMaxX - 128));
            // This sets the speed on this enemy to correspond with the multiplier, but also ensures that no matter where this Enemy is created, it will always take the same
            // amount of time to reach the user's base, by encompassing its distance from the base.
            RadianRotation = PointDirection((int)X, (int)Y, (int)Game1.TheGame.EndPos.X, (int)Game1.TheGame.EndPos.Y);
            // This points this Enemy in the direction of travel (from itself to the user's Castle).
        }
        public override void Update()
        {
            move_towards_point(Game1.TheGame.EndPos,Speed);
            // Move towards the user's Castle, at this Enemy's speed.
            if (GetDistance(Game1.TheGame.EndPos.X,Game1.TheGame.EndPos.Y) < Speed)// If this Enemy is close to the Castle.
            {
                if (DestroyInstance())// If this Enemy has not already been destroyed; destroy this Enemy.
                {
                    Game1.TheGame.CurrentHealth -= 100;// Reduce the user's health.
                }
            }
            base.Update();
        }
    }
    [Serializable]
    public class FlowerEnemy : Enemy
    {
        // Calls the inherited constructor. Also sets the speed, element, and health of this type of Enemy. The health and speed are set to correspond with the multiplier.
        public FlowerEnemy(String spriteName, double aX, double aY, int room, double multiplier)
            : base(spriteName, aX, aY, room, multiplier)
        {
            Element = Game1.E.Earth;
            CurrentHealth = (int)(4000.0 * IncomingEnemyMultiplier);
            MaxHealth = (int)(4000.0 * IncomingEnemyMultiplier);
            Speed = 0.5 + (0.5 * IncomingEnemyMultiplier) ;
        }
        public override void Update()
        {
            FollowPath();// This Enemy follows the path, and has no additional effects.
            base.Update();
        }
    }


    [Serializable]
    public abstract class Tower : Instance// The base class for all towers.
    {
        public int Range;// The maximum distance Enemy's can be while still able to be affected by this Tower.
        public Color RangeColour = Color.Black;// The colour of the circle that displays the range of this Tower.
        public bool Focused;// Only one Tower can have the upgrade and delete buttons display for it. Also range is only displayed for the focused tower, so the graphics of the
        // range circles does not confuse the user (except for the BeamTower).
        public int Cost;// The cost of building this Tower.
        public Queue<Upgrade> Upgrades = new Queue<Upgrade>();// The upgrades that this Tower can have.
        public Instance TargetInstance;// The Instance that this Tower will aim at (may not apply to all Towers).
        // Calls the inherited constructor.
        public Tower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            IsTower = true;// Marks this Instance so that it can easily be determine that this is a Tower.
        }
        public override void Update()
        {
            TryGetFocus();// Checks if this Tower should be selected.
            Colour = Focused ? Color.Yellow : Color.White;// The user knows if this Tower is selected as it will have a yellow tint.
            base.Update();
        }
        public override void Draw()
        {
            if (Focused || InstanceName.EndsWith("BeamTower"))// This will draw the range circle of this Tower if it is a BeamTower or is selected. It doesn't actually use a
                // circle, it draws a 48-sided shape as an approximation to a circle. A higher number of sides would make it more closely resemble a circle, however, would cost
                // performance.
            {
                Game1.SpriteBatch.DrawCircle(new Vector2((float)X, (float)Y), Range, 48, RangeColour);
            }
            base.Draw();
        }
        public void TryGetFocus()
        {
            if (I.MousePressed(I.Mouses.Left))// If the user has left clicked anywhere.
            {
                if ((GetType() != typeof(Wall) || CollisionWithAnyTowers()) &&
                    !Focused) // If this Tower is not a wall or there is not a collision with any towers, and it is 
                    // not already focused. If there is a Tower placed on a Wall and it is clicked, this makes sure that the Wall cannot be clicked, only the Tower can, as the
                    // Wall cannot be deleted with a Tower on top.
                {
                    if (CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y))// If the user's cursor is on this Tower.
                    {
                        foreach (Instance i in Game1.TheGame.Rooms[Room].Values
                        ) // Iterates over every Instance in this Instance's room.
                        {
                            if (i.IsTower) // If the Instance is a Tower.
                            {
                                ((Tower) i).Focused = false; // Make every other Tower not focused.
                            }
                        }
                        Focused = true; // Makes this tower focused.
                        if (GetType() != typeof(BeamTower) ||
                            ((BeamTower) this).Partner != null
                            ) // If this Tower is not a BeamTower or this BeamTower has a Partner. Unpartnered Beam
                            // -Towers cannot be selected.
                        {
                            Game1.TheGame.Deletebutton.Show = true; // Show the delete button up and right of this tower.
                            Game1.TheGame.Deletebutton.X = X + 32;
                            Game1.TheGame.Deletebutton.Y = Y - 32;
                            Game1.TheGame.Deletebutton.Caller = this; // Set the delete button so that it knows it is shown for this Tower.
                            if (Upgrades.Count > 0) // If this Tower can be upgraded and has upgrades remaining.
                            {
                                Game1.TheGame.Upgradebutton.Show = true; // Show the upgrade button up and left of this tower.
                                Game1.TheGame.Upgradebutton.X = X - 32;
                                Game1.TheGame.Upgradebutton.Y = Y - 32;
                                Game1.TheGame.Upgradebutton.Caller =
                                    this; // Set the upgrade button so that it knows it is shown for this Tower.

                            }
                        }
                        
                    }

                }
                else// If the user left clicks anywhere on the screen, except for the upgrade button, while this Tower is selected. (This is because the Tower will remain selected
                    // when upgraded in case the user wants to upgrade the Tower twice). This makes sure that the tower does not remain selected after it has been deleted.
                {
                    if (!Game1.TheGame.Upgradebutton.CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y))
                    {
                        // Deselects this Tower and hide the upgrade and delete buttons.
                        Game1.TheGame.Upgradebutton.Show = false;
                        Game1.TheGame.Deletebutton.Show = false;
                        Focused = false;
                    }
                }
            }
        }
        /// <summary>
        /// Gets whether there are any Towers already present at this Tower's location.
        /// </summary>
        /// <returns>Returns true if there is another Tower at this Tower's location.</returns>
        public bool CollisionWithAnyTowers()// Used to detect if this Tower is on top of a Wall.
        {
            foreach (Instance i in GetColliders())// For each Instance in this Instance's room that this Instance is colliding with.
            {
                if (i.IsTower)// If it is a Tower.
                {
                    return true;// Return true, there is another Tower present at this Tower's location.
                }
            }
            return false;// Return false, there is no other Tower at this Tower's location.
        }
            
    }

    [Serializable]
    public class ArrowTower : Tower
    {
        public int ArrowInterval;// The number of frames that must pass before another Arrow can be fired.
        public int MaxArrowInterval = 100;// The minimum number of frames that must pass between Arrow fires.
        public bool HasPoison;// If true, this Tower has had the poison upgrade.
        public bool HasPierce;// If true, this Tower has had its pierce upgrade.
        public string ArrowSprite = "Arrow";// The key for the Texture2D of Arrows fired by this Tower.
        public double ArrowSpeed = 3;// The speed that fired Arrows will travel at.
        // Calls the inherited constructor, enqueue's this Tower's upgrades, and sets this Tower's element and range.
        public ArrowTower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Element = Game1.E.Earth;
            Range = 96;
            Upgrades.Enqueue(new Upgrade(Poison,200,"Poison Tip - Increases tower range and damage, and applies over-time poison damage on hit."));
            Upgrades.Enqueue(new Upgrade(Pierce, 200,"Piercing Bolts - Increases frequency of arrow fire and arrows can pierce through enemies."));
        }
        public override void Update()
        {
            ArrowInterval -= 1;// Reduces the number of frames that must pass before the next Arrow is fired by 1.
            TargetInstance = GetNearest(true);// Sets this Tower's target as the nearest Enemy.
            if (TargetInstance != null)// If there is an Enemy in the playing area.
            {
                if (GetDistance(TargetInstance) < Range)// If the Enemy lies within this Tower's range. (Since the target is the nearest Enemy then other enemies cannot be within
                    // range of this Tower if the target isn't).
                {
                    RadianRotation = PointDirection(TargetInstance);// Point this Tower towards the Enemy.
                    if (ArrowInterval <= 0)// If this Tower is able to fire an Arrow.
                    {
                        new Arrow(ArrowSprite, X, Y, HasPoison, HasPierce) {Speed = ArrowSpeed};// Fire an Arrow., with a speed equal to ArrowSpeed.
                        ArrowInterval = MaxArrowInterval;// Set the number of frames that must pass before the next Arrow is fired.
                    }
                }
                else
                {
                    RadianRotation += 0.01;// This Tower will rotate slowly to notify the user that it is looking for a target.
                }
            }
            
            base.Update();
        }
        public void Poison()// An upgrade.
        {
            // This upgrade makes the Arrows poisonous, increases their damage, increases this Tower's range, increases the speed at which Arrows travel, and changes how
            // this Tower and the Arrows it fires look.
            HasPoison = true;
            Range += 32;
            Damage += 100;
            Sprite = "PoisonArrowTower";
            ArrowSprite = "PoisonArrow";
            ArrowSpeed += 0.5;
        }
        public void Pierce()// An upgrade.
        {
            // This upgrade will make Arrows fired by this Tower pierce through targets, increase the rate at which Arrows are fired, makes Arrows move faster, and changes how
            // this Tower and Arrows it fires look.
            HasPierce = true;
            MaxArrowInterval -= 20;
            ArrowSprite = "PierceArrow";
            Sprite = "PierceArrowTower";
            ArrowSpeed += 1;
        }
    }
    [Serializable]
    public class MissileTower : Tower
    {
        public int MissileInterval;// The number of frames that must pass before this Tower can fire another missile.
        public int MaxMissileInterval = 300;// The minimum number of frames that must pass between missile fires.
        public bool HasUranium;// If true, this Tower has the uranium upgrade.
        public string MissileSprite = "Missile";// The key of the Texture2D of the Missiles fired by this Tower.
        // Calls the inherited constructor, enqueue's this Tower's upgrades, and sets this Tower's element and range.
        public MissileTower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Element = Game1.E.Normal;
            Range = 160;
            Upgrades.Enqueue(new Upgrade(Artillery,200,"Artillery - Dramatically increases frequency of missiles fired."));
            Upgrades.Enqueue(new Upgrade(Uranium, 200,"Uranium - Dramatically increases damage and explosion radius of missiles."));
        }
        public override void Update()
        {
            MissileInterval -= 1;// Reduces the number of frames that must pass before the next Missile is fired by 1.
            TargetInstance = GetNearest(true);// Sets the target as the nearest Enemy.
            if (TargetInstance != null)// If there is an Enemy present in the playing area.
            {
                if (GetDistance(TargetInstance) < Range)// If the nearest Enemy lies within range of this Tower.
                {
                    RadianRotation = PointDirection(TargetInstance);// Point at the Enemy.
                    if (MissileInterval <= 0)// If this Tower is able to fire a Missile.
                    {
                        Missile shooty = new Missile(MissileSprite, X, Y);// Fire a missile.
                        if (HasUranium)// If this Tower has the uranium upgrade.
                        {
                            shooty.Damage += 1000;// Missile deals more damage to target.
                            shooty.MaxDamage = 2000;// Missile deals more damage to nearby Enemies on impact.
                            shooty.DistMultiplier = 31.25;// Missile will damage Enemies in a larger radius.
                            shooty.Radius = 64;
                        }
                        MissileInterval = MaxMissileInterval;// Sets the number of frames that must pass before the next Missile is fired.
                    }
                }
                else
                {
                    RadianRotation += 0.01;// This tower slowly rotates so that the user knows it is searching for a target.
                }
            }

            base.Update();
        }
        public void Artillery()// An upgrade.
        {
            // This upgrade dramatically increases the rate at which Missiles are fired, and changes how this Tower looks.
            MaxMissileInterval -= 250;
            Sprite = "ArtilleryTower";
        }
        public void Uranium()// An upgrade
        {
            // This upgrade gives this Tower the uranium upgrade, and changes how this Tower and Missiles it fires look.
            HasUranium = true;
            Sprite = "UraniumTower";
            MissileSprite = "UraniumMissile";
        }
    }
    [Serializable]
    public class LightTower : Tower
    {
        public int LightInterval;// The number of frames that must pass before this Tower can damage an Enemy.
        public bool HasLaserBeam;// Determines whether this Tower has the laser beam upgrade.
        public bool HasStun;// Determines whether this Tower has the stun upgade.
        // Calls the inherited constructor, enqueue's this Tower's upgrades, and sets this Tower's element and range.
        public LightTower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Element = Game1.E.Light;
            Range = 128;
            Upgrades.Enqueue(new Upgrade(LaserBeam,200,"Laser Beam - Tower will shoot a Laser Beam instead of light shots, which can reflect between enemies."));
            Upgrades.Enqueue(new Upgrade(Stun, 200,"Solar Stun - Enemies will be blinded by the laser beam and become briefly unable to move."));
        }
        public override void Update()
        {
            LightInterval -= 1;// Reduces the number of frames that must pass before this Tower can damage an Enemy by 1.
            TargetInstance = GetNearest(true);// Gets the nearest Enemy.
            if (TargetInstance != null)// If there is anEnemy present in the playing area.
            {
                if (GetDistance(TargetInstance) < Range)// If the nearest Enemy lies within the range of this Tower.
                {
                    if (LightInterval <= 0)// If this Tower is able to damage an Enemy.
                    {
                        if (HasLaserBeam)// If this Tower has the laser beam upgrade.
                        {
                            new LaserBeam("WhitePixel", X, Y, HasStun)// Creates a laser beam, passing whether it can stun or not as an argument.
                            {
                                Colour = HasStun ? Color.Yellow : Color.Red// If the laser beam can stun, it is yellow, otherwise it is red.
                            };
                            LightInterval = 20;// Sets the number of frames that must pass before this Tower can damage an Enemy.
                            
                        }
                        else// Otherwise if this Tower has not been upgraded.
                        {
                            new LightShot("LightShot", X, Y);// Fires a light shot.
                            LightInterval = 10;// Sets the number of frames that must pass before this Tower can damage an Enemy.
                        }
                        
                    }
                }
                RadianRotation += 0.03;
            }

            base.Update();
        }
        public void LaserBeam()// An upgrade.
        {
            // This upgrade gives the Tower the laser beam upgrade, and changes how it looks.
            HasLaserBeam = true;
            Sprite = "LaserTower";
        }
        public void Stun()//An upgrade.
        {
            // This upgrade gives this Tower the stun upgrade and changes how it looks.
            HasStun = true;
            Sprite = "StunTower";
        }
    }
    [Serializable]
    public class WaterTower : Tower
    {
        public int WaterInterval;// The number of frames that must pass before this Tower can damage an Enemy.
        public bool HasBubble;// Determines whether this Tower has the bubble upgrade.
        public bool HasWave;// Determines whether this Tower has the wave upgrade.
        // Calls the inherited constructor, enqueue's this Tower's upgrades, and sets this Tower's element and range.
        public WaterTower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Element = Game1.E.Water;
            Range = 384;
            Upgrades.Enqueue(new Upgrade(BubbleUpgrade,200,"Bubble trap - Targets hit will now drop a bubble, which will damage enemies who cross it."));
            Upgrades.Enqueue(new Upgrade(WaveUpgrade, 200,"Tsunami - Tower will now additionally fire projectiles in all directions."));
        }
        public override void Update()
        {
            WaterInterval -= 1;// Reduces the number of frames that must pass before this Tower can damage an Enemy by 1.
            TargetInstance = GetNearest(true);// Sets the target to the nearest Enemy.
            if (TargetInstance != null)// If there is another Enemy present in the playing area.
            {
                if (GetDistance(TargetInstance) < Range)// If the nearest Enemy lies within the range of this Tower.
                {
                    if (WaterInterval <= 0)// If this Tower is able damage an Enemy.
                    {
                        if (HasWave)// If this Tower has the wave upgrade.
                        {
                            for (double i = 0; i < Math.PI * 2; i += Math.PI * 2 / 8)// For each eighth of a circle
                            {
                                // Fire a WaterShot in the current iterated direction.
                                WaterShot waveShooty = new WaterShot("WaterShot", X, Y, HasBubble, true);
                                // This part ensures that the target Enemy can only be hit by the WaterShot aimed at it, as it would be unfair if it could be hit by multiple.
                                Instance temp = GetNearest(true);
                                if (temp != null)
                                {
                                    waveShooty.TargetInstance = temp;
                                }
                                // Point the WaterShot in the currently iterated direction.
                                waveShooty.RadianRotation = i;
                                
                            }
                        }
                        // Creates a new WaterShot which will fire at the NearestEnemy.
                        new WaterShot("WaterShot", X, Y, HasBubble, false);
                        // Sets the number of frames that must pass until the next WaterShot is fired.
                        WaterInterval = 500;
                    }
                }
            }

            base.Update();
        }
        public void BubbleUpgrade()// An upgrade.
        {
            // This upgrade gives this Tower the bubble upgrade and changes how it looks.
            HasBubble = true;
            Sprite = "BubbleTower";
        }
        public void WaveUpgrade()// An upgrade.
        {
            // This upgrade changes how the tower looks and gives it the wave upgrade.
            Sprite = "WaveTower";
            HasWave = true;
        }
    }
    [Serializable]
    public class WhirlwindTower : Tower
    {
        public bool HasBarbedWire;// Determines whether this Tower has the barbed wire upgrade.
        public int SuctionPowerDivider = 20;// The higher this number, the less powerful this Tower's pull on Enemies is.
        // Calls the inherited constructor, enqueue's this Tower's upgrades, and sets this Tower's element and range.
        public WhirlwindTower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Element = Game1.E.Wind;
            Range = 96;
            Upgrades.Enqueue(new Upgrade(BarbedWire,200,"Razor Wire - Enemies will be sliced on spinning razor wire when pulled near the tower."));
            Upgrades.Enqueue(new Upgrade(PowerVacuum, 200,"Vacuum Tube - Increases suction power of tower."));
        }
        public override void Update()
        {
            bool activityDone = false;// Determines whether this Tower has pulled at least one Enemy this frame.
            // For each Enemy in this room.
            foreach (Instance i in Game1.TheGame.Rooms[Room].Values)
            {
                if (i.IsEnemy)
                {
                    Enemy temp = (Enemy)i;
                    if (temp.SuctionTimeout < 60)// If the iterated Enemy is able to be pulled, as it has not recently been pulled.
                    {
                        if (GetDistance(temp) < Range)// If the Enemy lies within this Tower's range.
                        {
                            // Pull the Enemy towards this Tower, increasing the power of the pull the longer the Enemy is pulled for.
                            temp.MoveTowards(this, (60 - temp.SuctionTimeout)/SuctionPowerDivider);
                            // Reduces the amount of frames that have passed since the Enemy has been pulled by 1.
                            temp.SuctionTimeout -= 1;
                            // Allows the Tower to know it has pulled an Enemy this frame.
                            activityDone = true;
                        }
                        // If the Enemy is within the barbed wire area of the Tower, damage it.
                        if (GetDistance(temp) <= 32 && HasBarbedWire)
                        {
                            DamageInstance(temp);
                        }
                    }
                }
            }
            
            if (activityDone)// If this Tower is pulling an Enemy, notify the user by making this Tower's range circle colour red, and rotating it.
            {
                RangeColour = Color.Red;
                RadianRotation -= 0.1;
            }
            else// Otherwise, if it is not pulling an Enemy, make this Tower's range circle colour black.
            {
                RangeColour = Color.Black;
            }
            base.Update();
        }
        public void BarbedWire()// An upgrade.
        {
            // This upgrade changes how this Tower looks, adjusts its collision box and source box to correspond with its new Texture2D, gives this Tower the barbed wire upgrade,
            // increases its damage dealt, increases its range, and increases the effect of its pull on Enemies.
            Sprite = "BarbedWireTower";
            CollisionBox = new Rectangle((int)X, (int)Y, Game1.SpriteList[Sprite].Width, Game1.SpriteList[Sprite].Height);
            SourceBox = new Rectangle(0, 0, Game1.SpriteList[Sprite].Width, Game1.SpriteList[Sprite].Height);
            HasBarbedWire = true;
            Damage += 35;
            Range += 8;
            SuctionPowerDivider -= 3;
        }
        public void PowerVacuum()// An upgrade.
        {
            // This upgrade increases the effect of this Tower's pull on Enemies, and increases its damage and range.
            SuctionPowerDivider -= 5;
            Damage += 10;
            Range += 32;
        }
    }
    [Serializable]
    public class BeamTower : Tower
    {
        public int BeamInterval;// The number of frames that must pass before this Tower can create another Beam.
        public BeamTower Partner;// The partner BeamTower that the Beam will be created between.
        public bool Paired;// Determines whether this Tower has a partner.
        public Beam TheBeam;// A reference to the active Beam created by this Tower.
        public bool HasPlasma;// Determines whether this Tower has the plasma upgrade.
        public int TimeAlive = 30;// The number of frames that must pass before a created Beam is destroyed.
        public double IntervalDivider = 1;// The higher this number is, the shorter the interval between Beam creation.
        public String BeamSprite = "Beam";// The key of the Texture2D that is drawn by the Beam.
        // Calls the inherited constructor, enqueue's this Tower's upgrades, and sets this Tower's element and range.
        public BeamTower(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            
            RangeColour = Color.FromNonPremultiplied(100,100,200,128);// Changes the colour of the range circle of this Tower so that the user know it does not represent
            // range. It is also translucent to be less distracting for the user.
            if (Game1.TheGame.BeamPair)// If there is another BeamTower placed that is waiting to be paired.
            {
                foreach (Instance i in Game1.TheGame.Rooms[Room].Values)// Iterates through each Instance in this Instance's room, and tests that it is a BeamTower, is not
                    // this Instance, and is not already Paired with a BeamTower.
                {
                    if (i.GetType() == GetType())
                    {
                        if (i != this)
                        {
                            BeamTower temp = (BeamTower)i;
                            if (temp.Paired == false)
                            {
                                // Sets this Tower's partner to the iterated BeamTower.
                                Partner = temp;
                                // Marks both Towers as already having a Partner.
                                Paired = true;
                                temp.Paired = true;
                                // Makes the Towers point towards each other.
                                RadianRotation = PointDirection(Partner);
                                Partner.RadianRotation = RadianRotation + Math.PI;
                                // Sets the damage of this Tower.
                                Damage = 40;
                                // Enqueues the upgrades for this Tower.
                                Upgrades.Enqueue(new Upgrade(Plasma,200,"Plasma Beam - Enemies will now be dealt the same damage again using Wind element."));
                                Upgrades.Enqueue(new Upgrade(HeatDissipation, 200,"Heat Dissipators - Dramatically increases duration of beam, and reduces beam cooldown."));
                                break;
                            }
                        }
                    }
                }
            }
            Game1.TheGame.BeamPair = !Game1.TheGame.BeamPair;
        }
        public override void Update()
        {
            Range = (BeamInterval/4) + 16;
            if (Partner != null)
            {
                if (TheBeam != null)
                {
                    if (TheBeam.TimeAlive > 0)
                    {
                        foreach (Instance i in Game1.TheGame.Rooms[Room].Values)
                        {
                            if (i.IsEnemy)
                            {
                                
                                int x0 = (int)i.X;
                                int y0 = (int)i.Y;

                                int x1 = (int)X;
                                int y1 = (int)Y;

                                int x2 = (int)Partner.X;
                                int y2 = (int)Partner.Y;
                                int dividingSection = (int)Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                                if (dividingSection > 0)
                                {

                                    int disttoline = Math.Abs(((x2 - x1) * (y1 - y0) - (x1 - x0) * (y2 - y1)) / dividingSection);
                                    if (disttoline <= 16)
                                    {
                                        if (i.X >= Math.Min(X, Partner.X) - 16 && i.Y >= Math.Min(Y, Partner.Y) - 16 && i.X <= Math.Max(X, Partner.X) + 16 && i.Y <= Math.Max(Y, Partner.Y) + 16)
                                        {
                                            DamageInstance((Enemy)i, Game1.E.Fire, Damage);
                                            
                                            if (HasPlasma)
                                            {
                                                DamageInstance((Enemy)i, Game1.E.Wind, Damage);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        TheBeam.DestroyInstance();
                        TheBeam = null;
                    }
                }
                BeamInterval -= 1;
                if (BeamInterval <= 0)
                {
                    TheBeam = new Beam(BeamSprite, X, Y,TimeAlive);
                    double dist = GetDistance(Partner);
                    TheBeam.CollisionBox = new Rectangle(0, 0, (int)dist, TheBeam.CollisionBox.Height);
                    BeamInterval = (int)(dist/IntervalDivider);
                    TheBeam.RadianRotation = PointDirection(Partner);
                }
                
            }
            base.Update();
        }
        public void Plasma()
        {
            HasPlasma = true;
            BeamSprite = "PlasmaBeam";
            Sprite = "PlasmaBeamTower";
            Partner.Sprite = "PlasmaBeamTower";
        }
        public void HeatDissipation()
        {
            TimeAlive += 50;
            IntervalDivider += 0.3;
            Sprite = "HeatDissipationTower";
            Partner.Sprite = "HeatDissipationTower";
        }
        public override void OnDestroy()
        {
            if (Partner != null)// When one BeamTower is deleted, so is its partner.
            {
                // Refunds money for the partner BeamTower.
                Game1.TheGame.TowerMoney += (int)(Partner.Cost * 0.9);
                // Destroys the partner BeamTower.
                Partner.DestroyInstance();
                // Removes reference between the partner and this BeamTower so that they are quickly processed by the garbage collector.
                Partner.Partner = null;
                Partner = null;
            }
            base.OnDestroy();
        }
    }
    [Serializable]
    public class Wall : Tower
    {
        // Calls the inherited constructor, and sets this Wall's element and damage.
        public Wall(String spriteName, double aX, double aY, int room = 0) : base(spriteName, aX, aY, room)
        {
            Element = Game1.E.Normal;
            Damage = 0;
            // Marks the node of the map that this Wall occupies so that enemies cannot walk on it and Towers can be placed on it.
            Game1.TheGame.Map[(int)X, (int)Y] = false;
            // Stores this Wall's coordinates in a Vector2.
            Vector2 me = new Vector2((int)X, (int)Y);
            // For better performance, it is tested if this Wall blocks the path actually used by Enemies before attempting to repair the path.
            if (Game1.TheGame.Path.Contains(me))
            {
                if (Game1.RegeneratePath(me) == false)// Repairs the path to avoid this Wall. If the path cannot be repaired, destroy this Wall, and make the coordinates that
                    // this Wall lies upon usable by Enemies again.
                {
                    Destroyed = true;
                    Game1.TheGame.ToBeDestroyed.Add(this);
                    Game1.TheGame.Map[(int)X, (int)Y] = true;
                }
            }
        }
        public override void OnDestroy()// When Walls are destroyed, the area that the Wall was is made usable by Enemies again, and the path is generated again in case
            // the path can be made shorter with the absence of this Wall.
        {
            Game1.TheGame.Map[(int)X, (int)Y] = true;
            Game1.GeneratePath();
            base.OnDestroy();
        }
    }
    [Serializable]
    public abstract class Projectile : Instance// The base class for all towers.
    {
        public int Range;// The number of pixels that this Instance can travel before being destroyed.
        public Instance TargetInstance;// The Enemy this Instance is targetting. May not apply to all Projectiles.
        // Calls the inherited constructor.
        public Projectile(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {

        }

    }

    [Serializable]
    public class Arrow : Projectile
    {
        public double DistanceTravelled;// The number of pixels this Instance has travelled since creation.
        public bool HasPoison;// Determines whether the Tower that created this Instance has had the poison upgrade.
        public List<Instance> Hitters = new List<Instance>();// A List of all Enemies hit, so that if this Projectile can pierce through Enemies, it can only hit each Enemy once.
        public bool HasPierce;// Determines whetherthe Tower that created this Instance has had the pierce upgrade.
        // Calls the inherited constructor.
        public Arrow(String spriteName, double aX, double aY, bool poison, bool pierce, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            // Targets the nearest Enemy. If none are present, this Instance will be destroyed.
            TargetInstance = GetNearest(true);
            if (TargetInstance == null)
            {
                DestroyInstance();
            }
            else// If there is an Enemy present.
            {
                Range = 128;// Sets the range of this Projectile.
                RadianRotation = PointDirection(TargetInstance);// Points this projectile towards its target.
                
                Damage = 1000;// Sets the damage, element, and upgrades of this Projectile.
                Element = Game1.E.Earth;
                HasPoison = poison;
                HasPierce = pierce;
            }
        }
        public override void Update()
        {
            // Does not home in on its target, so moves at the initial angle set when pointing at the target enemy.
            MoveAtAngle(RadianRotation, Speed);
            // Adds to the distance travelled. If this Instance has travelled farther than its range, it will be destroyed.
            DistanceTravelled += Speed;
            if (DistanceTravelled > Range)
            {
                DestroyInstance();
            }
            if (HasPierce)// If the Tower that created this Instance has the pierce upgrade.
            {
                foreach (Instance i in GetColliders())// For every Enemy it is currently colliding with.
                {
                    if (i.IsEnemy)
                    {
                        if (!Hitters.Contains(i))// If it has not already been damaged.
                        {
                            DamageEvent((Enemy)i);// Damage the iterated Enemy, and add it to the List of Enemies that have already been damaged by this Instance.
                            Hitters.Add(i);
                        }
                    }
                }
            }
            else if (Collision(TargetInstance))// Otherwise, this Projectile will damage and be destroyed by the Enemy it first targeted, if it hits it.
            {
                DamageEvent((Enemy)TargetInstance);
                DestroyInstance();
            }
            base.Update();
        }
        public void DamageEvent(Enemy toBeDamaged)
        {
            DamageInstance(toBeDamaged);// Damage an Enemy with this projectile's damage and element.
            if (HasPoison)// If the Tower that created this Projectile has had the poison upgrade.
            {
                ((Enemy)TargetInstance).PoisonTime = 100;// Mark the Enemy to be poisoned for 100 frames.
            }
        }
    }
    [Serializable]
    public class Missile : Projectile
    {
        public double DistanceTravelled;
        public static bool SeekingEnabled = true;
        public int MaxDamage = 1000;
        public double DistMultiplier = 20;
        public int Radius = 50;
        public Missile(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Range = 160;
            TargetInstance = GetNearest(true);
            if (TargetInstance == null)
            {
                DestroyInstance();
            }
            else
            {
                RadianRotation = PointDirection(TargetInstance);
                Speed = 0;
                Damage = 100;
                Element = Game1.E.Normal;
            }
        }
        public override void Update()
        {
            Speed += 0.04;
            MoveAtAngle(RadianRotation, Speed);
            if (SeekingEnabled)
            {
                RadianRotation = PointDirection(TargetInstance);
            }
            DistanceTravelled += Speed;
            if (DistanceTravelled > Range)
            {
                MissileDamage();
                DestroyInstance();
            }
            if (Collision(TargetInstance))
            {
                if (DistanceTravelled > 10)
                {
                    DamageInstance((Enemy)TargetInstance);
                    MissileDamage();
                    DestroyInstance();
                }
            }
            base.Update();
        }
        public void MissileDamage()
        {
            foreach (Instance i in Game1.TheGame.Rooms[Room].Values)
            {
                if (i.IsEnemy)
                {
                    double dist = GetDistance(i);
                    if (dist < Radius)
                    {
                        DamageInstance((Enemy)i, Game1.E.Normal, (int)(MaxDamage - (dist * DistMultiplier)));
                    }
                }
            }
        }
    }
    [Serializable]
    public class LightShot : Projectile
    {

        public double DistanceTravelled;// Holds the number of pixels this instance has travelled through, which can be processed to test if the instance
        // has exceeded the maximum distance it can travel.
        public LightShot(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            Range = 128;// Sets the maximum number of pixels this projectile can travel before being destroyed.
            TargetInstance = GetNearest(true);// Sets the targeted instance to the nearest instance to this projectile.
            if (TargetInstance == null)// If a nearest enemy could not be found.
            {
                DestroyInstance();// Destroy this projectile, as it cannot aim at an enemy that does not exist.
            }
            else// If a target has been found.
            {
                RadianRotation = PointDirection(TargetInstance);// Orients this projectile towards the targeted instance.
                Speed = 6;// Sets the number of pixels this projectile will travel per frame.
                Damage = 100;// Sets the amount of health(modified by the element) that an enemy will lose upon contact with this projectile.
                Element = Game1.E.Light;// Sets the element of this projectile, so that an element damage modifier can be applied upon contact with an enemy.
            }
        }
        public override void Update()
        {
            // By not changing the radianrotation to point at the target each frame, faster enemies and enemies further from the tower that created this 
            // projectile will have an higher chance of not being hit. This allows for the user to use interesting tactics.
            MoveAtAngle(RadianRotation, Speed);// Moves in the direction that the target was in when the projectile was created.
            DistanceTravelled += Speed;// If the distance travelled by this projectile exceeds its range, it is destroyed.
            if (DistanceTravelled > Range)
            {
                DestroyInstance();
            }
            if (Collision(TargetInstance))// If this projectile collides with its target, damage the target and destroy this projectile.
            {
                DamageInstance((Enemy)TargetInstance);
                DestroyInstance();
            }
            base.Update();
        }
    }
    [Serializable]
    public class Bubble : Projectile
    {
        public int Timeout = 600;// The number of frames that can pass before this bubble is destroyed.
        // The bubble has a limited lifespan as if a large number of bubbles accumulated indefinitely, this would give the user an unfair advantage
        // as enemies could be damaged excessively when passing through the large number of bubbles, and also performance could decrease significantly.
        public Instance Immune;// The enemy that cannot be hit by this bubble, as it has already been hit by the watershot which has produced this bubble.
        // I cannot allow the enemy hit by the watershot that created this bubble to be hit by this bubble because it would happen instantly, which would render
        // this bubble pointless(as I could've just increased the damage of the watershot instead of the bubble upgrade). It also means that the bubble upgrade
        // is a tactic used against chains of enemies, as subsequent enemies will collide with bubbles.
        public Bubble(String spriteName, double aX, double aY, Instance immuneInstance, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {
            Element = Game1.E.Water;// Sets the element of this projectile, so that an element damage modifier can be applied upon contact with an enemy.
            Damage = 1000;// Sets the amount of health(modified by the element) that an enemy will lose upon contact with this projectile.
            Immune = immuneInstance;// Sets the instance that is immune to being hit by this bubble.
        }
        public override void Update()
        {
            Timeout -= 1;// Each frame, brings this bubble one frame closer to being destroyed due to excessive lifespan.
            if (Timeout <= 0)// If the bubble has exceeded its lifespan.
            {
                DestroyInstance();// Destroy this bubble.
            }
            foreach (Instance i in GetColliders())// For each instance that is colliding with this bubble.
            {
                if (i.IsEnemy)// If the colliding instance is an enemy.
                {
                    if (i != Immune)// If the colliding enemy has not been hit by this bubble's respective watershot.
                    {
                        DestroyInstance();// Destroy this bubble.
                        DamageInstance((Enemy)i);// Damage the colliding enemy.
                        break;// Breaks from the loop, so no further colliding enemies are damaged.
                    }
                }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class WaterShot : Projectile
    {
        public double DistanceTravelled;// Holds the number of pixels this instance has travelled through, which can be processed to test if the instance
        // has exceeded the maximum distance it can travel.
        public bool HasBubble;// If true, this projectile was made by a tower with the bubble upgrade, and will create a bubble upon impact with an enemy.
        public bool HasWave;// If true, this projectile was made by a tower with the wave upgrade, and will not aim at a specific enemy.
        public WaterShot(String spriteName, double aX, double aY, bool bubble, bool wave, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {
            Range = 384;// Sets the maximum distance that this projectile can travel.
            Speed = 18;// Sets the number of pixels that this projectile travels each frame.
            Damage = 2000;// Sets the amount of health(modified by the element) that an enemy will lose upon contact with this projectile.
            Element = Game1.E.Water;// Sets the element of this projectile, so that an element damage modifier can be applied upon contact with an enemy.
            HasBubble = bubble;// Holds the bubble value in the instance.
            HasWave = wave;// Holds the wave value in the instance.
            if (!wave)// If the projectile is to be targeted at an enemy.
            {
                TargetInstance = GetNearest(true);// Get the nearest enemy and set it as the target.
                if (TargetInstance == null)// If there is not a target.
                {
                    DestroyInstance();// Destroy this projectile, as it cannot aim at a target that does not exist.
                }
                else// If the target does exist.
                {
                    RadianRotation = PointDirection(TargetInstance);// Holds the direction that the target is in in this instance, so that the sprite can
                    // face the target, and so that the projectile moves towards the target.
                }
            }
        }
        public override void Update()
        {
            // By not changing the radianrotation to point at the target each frame, faster enemies and enemies further from the tower that created this 
            // projectile will have an higher chance of not being hit. This allows for the user to use interesting tactics.
            MoveAtAngle(RadianRotation, Speed);// Moves in the direction that the target was in when the projectile was created.
            DistanceTravelled += Speed;// Increases the distance travelled by the speed.
            if (DistanceTravelled > Range)// If this projectile has exceeded the maximum distance that it is allowed to travel.
            {
                DestroyInstance();// Destroy this projectile.
            }
            if (HasWave)// If this projectile was not targeted at an enemy.
            {
                foreach (Instance i in GetColliders())// Iterates over each instance that this projectile's collisionbox is currently intersecting.
                {
                    if (i.IsEnemy)// If the currently iterated instance is an enemy.
                    {
                        // This prevents an enemy from being hit by multiple of these projectiles when the tower fires, which would cause an unfair
                        // amount of damage on the enemy, especially if the enemy is at close range.
                        if (i != TargetInstance)// If the currently iterated instance is not the targeted instance.
                        {
                            if (DestroyInstance() && HasBubble)// If the projectile was destroyed and was created by a tower with the bubble upgrade.
                            {// If validation is not performed on whether this projectile has already been destroyed, multiple bubbles may spawn from one
                                // instance of this projectile.
                                new Bubble("Bubble", X, Y, i);// Create a bubble at this projectile's location.
                            }
                            DamageInstance((Enemy)i);// Damage the enemy that has been collided with.
                            break;// Break from the loop, as it is no longer necessary to run.
                        }
                    }
                }
            }
            else// If this projectile was made with a targeted enemy.
            {
                // This will not collide with any enemy except for the targeted enemy.
                if (Collision(TargetInstance))// If there if a collision with the targeted enemy.
                {
                    if (DestroyInstance() && HasBubble)// If the projectile was destroyed and was created by a tower with the bubble upgrade.
                    {
                        new Bubble("Bubble", X, Y, TargetInstance);// Create a bubble at this projectile's location.
                    }
                    DamageInstance((Enemy)TargetInstance);// Damage the targeted enemy.
                }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class Beam : Projectile
    {
        public int TimeAlive;// The number of frames that will pass until this instance is destroyed.
        public Beam(String spriteName, double aX, double aY, int alive, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            TimeAlive = alive;
            CollisionBox = new Rectangle(0, 0, CollisionBox.Width, CollisionBox.Height);
        }
        public override void Update()
        {
            TimeAlive -= 1;
        }
        public override void Draw()
        {
            Game1.SpriteBatch.Draw(Game1.SpriteList[Sprite], new Rectangle((int)X, (int)Y, CollisionBox.Width, CollisionBox.Height), SourceBox, Colour, (float)RadianRotation, new Vector2(0,Game1.SpriteList[Sprite].Height/2), SpriteEffects.None, 0);
        }
    }
    [Serializable]
    public class LaserBeam : Projectile
    {
        readonly List<Vector2> _collisionPoints = new List<Vector2>();
        readonly List<String> _hitters = new List<String>();
        int _boomTime = 10;
        public LaserBeam(String spriteName, double aX, double aY, bool stun = false, int room = 0)
            : base(spriteName, aX, aY, room)
        {
            _collisionPoints.Add(new Vector2((float)aX, (float)aY));
            Element = Game1.E.Light;
            Damage = 200;
            Instance returnNearest = Game1.TheGame.Towerplacer;
            int MaxHits = 4;
            while (returnNearest != null && MaxHits > 0)
            {
                MaxHits -= 1;
                returnNearest = null;
                double distance = 128;
                foreach (Instance i in Game1.TheGame.Rooms[Room].Values)
                {
                    double iteratedDistance = GetDistance(i);
                    if (iteratedDistance < distance)
                    {

                        if (i.IsEnemy)
                        {

                            if (i.Room == Game1.TheGame.Room)
                            {

                                if (((Enemy)i).CurrentHealth > 0)
                                {

                                    if (!_hitters.Contains(i.InstanceName))
                                    {
                                        returnNearest = i;
                                        distance = iteratedDistance;

                                    }
                                }
                            }
                        }
                    }
                }
                if (returnNearest != null)
                {
                    _collisionPoints.Add(new Vector2((float)returnNearest.X, (float)returnNearest.Y));
                    _hitters.Add(returnNearest.InstanceName);
                    X = returnNearest.X;
                    Y = returnNearest.Y;
                    DamageInstance((Enemy)returnNearest);
                    if (stun && ((Enemy)returnNearest).SuctionTimeout == 59)
                    {
                        ((Enemy)returnNearest).StunTime = 8;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        public override void Update()
        {
            _boomTime -= 1;
            if (_boomTime <= 0)
            {
                DestroyInstance();
            }
        }
        public override void Draw()
        {
            for (int i = 0; i < _collisionPoints.Count - 1; i++)
            {
                Game1.SpriteBatch.DrawLine(new Vector2(_collisionPoints[i].X, _collisionPoints[i].Y), new Vector2(_collisionPoints[i + 1].X, _collisionPoints[i + 1].Y), Colour, 4);
            }
        }
    }

    
    [Serializable]
    public abstract class MenuButton : Instance// The base class for all the menu buttons. Is abstract as only derived buttons should be instantiated.
    {
        protected MenuButton(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y))// If the user's cursor is over this button.
            {
                if (!Sprite.Contains("Down"))// If the sprite has not already been changed to the highlited sprite
                {
                    Sprite = Sprite + "Down";// Changes the sprite to the sprite used for when the button is highlighted.
                }
            }
            else// Otherwise if the user has not highlighted this button.
            {
                if (Sprite.Contains("Down"))// If the sprite is the highlighted sprite(if the user had highlighted this button last frame).
                {
                    Sprite = Sprite.Remove(Sprite.Length - 4, 4);// Changes the sprite to the sprite used for when the button is not highlighted.
                }
            }
            base.Update();// Calls the inherited event.
        }
    }

    [Serializable]
    public class SinglePlayer : MenuButton// Button which, when pressed, changes the current room to room 4(the level select room) in single player mode.
    {
        public SinglePlayer(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.TheGame.LastRoom = Game1.TheGame.Room;// Sets the previous room as the current room.
                Game1.TheGame.Room = 4;// Sets the current room to the level selected room.
                Game1.TheGame.IsMultiplayer = false;// The user is playing single player.
                Game1.TheGame.GameScreenTime = 0;// Reset the time spent in the game room.
                Game1.TheGame.Level = 0;
                Game1.TheGame.CurrentLevelTime = 0;
                Game1.TheGame.LastWaveObject = null;// Set to null so a new lastwaveobject is generated.
                Game1.TheGame.CurrentHealth = 1000;// Sets the castle current health back to the default of 1000.
                Game1.TheGame.MaxHealth = 1000;// Sets the castle maximum health back to the default of 1000.
                Game1.TheGame.TowerMoney = 10000;// Sets the money of tower production back to the default of 1000.
                for (int i = 0; i < 101; i++)// Generates one hundred waves for the user to play, and stores them in the game data instance. The first wave has no enemies.
                {
                    Game1.TheGame.Waves.Add(Game1.GenerateWave(i));
                }
            }

            base.Update();
        }
    }
    [Serializable]
    public class AddNewMap : MenuButton// Button which, when pressed, loads a user specified image for use as a level map image.
    {
        
        public AddNewMap(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (I.MousePressed(I.Mouses.Right) && CollisionBox.Contains(I.NewMouse.X,I.NewMouse.Y))// If the user right clicks this button, show them the required criteria for their loaded map image.
            {
                MessageBox.Show("Images created for use as maps must have the following properties:\n An alpha colour as the path\n A non-alpha colour as the walls\n A navigable path from the start to the end position\n A width of 1088 pixels and a height of 768 pixels.\n\nPath nodes are occur every 32 pixels in both the x and y directions starting from position (16,16).");
            }
            else if (MousePressedInMe())
            {
                Bitmap temp = Game1.TheGame.LoadImage();// Loads an image as a bitmap that the user selects from a dialog.
                if (temp != null)// If the user has selected an image.
                {
                    Game1.TheGame.AddLevelButton.Map = temp;// Use this map as the level image when the user presses the add level button. The image is stored in the add level button.
                }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class AddLevel : MenuButton// Button which, when pressed, creates a new level, with the user specified map image, start location, and end location.
    {
        Vector2 _startLocation;// The user specified location where enemies will be created on the map.
        Vector2 _endLocation;// The user specified location of the castle that the enemies will attempt to reach on the map.
        public Bitmap Map;// The user specified map image, in a serializable format that cannot be drawn by the sprite batch.
        public AddLevel(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the default constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                    if (Map != null)// If the user has loaded a map image.
                    {
                        if (Game1.Maps.Count < 10)// If there is fewer that 10 maps. 
                        {
                            try// An error can occur when attempting to parse the user entered locations, if they have entered one or more invalid strings.
                            {
                                // These integers round the user entered locations to the centre of the nearest 32x32px square, as the path enemies follow is generated based on 32x32px squares.
                                int startx = (int)(Math.Round((double)((int.Parse(((TextBox)Game1.TheGame.Rooms[Room]["StartXPosition"]).TextBoxString) - 16) / 32)) * 32) + 16;
                                int starty = (int)(Math.Round((double)((int.Parse(((TextBox)Game1.TheGame.Rooms[Room]["StartYPosition"]).TextBoxString) - 16) / 32)) * 32) + 16;
                                int endx = (int)(Math.Round((double)((int.Parse(((TextBox)Game1.TheGame.Rooms[Room]["EndXPosition"]).TextBoxString) - 16) / 32)) * 32) + 16;
                                int endy = (int)(Math.Round((double)((int.Parse(((TextBox)Game1.TheGame.Rooms[Room]["EndYPosition"]).TextBoxString) - 16) / 32)) * 32) + 16;
                                _startLocation = new Vector2(startx, starty);// Sets the map start location to the rounded version of the user entered start location.
                                _endLocation = new Vector2(endx, endy);// Sets the map end location to the rounded version of the user entered end location.
                                // If the rounded versions of the user specified start and end locations lie within the map, and the start and end locations are not the same location.
                                if (_startLocation.X < 1088 && _startLocation.Y < 768 && _startLocation.X > 0 && _startLocation.Y > 0 && _endLocation.X < 1088 && _endLocation.X > 0 && _endLocation.Y > 0 && _endLocation.Y < 768 && _startLocation != _endLocation)
                                {
                                    // Here it is checked that there is a navigable route for enemies between the start and end locations.
                                    bool[,] testMap = new bool[Game1.TheGame.PathMaxX, Game1.TheGame.PathMaxY];// Creates a 2d array of booleans to hold whether each node is navigable.
                                    Game1.GenerateNavigableNodes(Map, ref testMap);// Uses the user specified map to fill the array of booleans with whether each node has 0 alpha or not.
                                    List<Vector2> tempNodes = Game1.FindPath(_startLocation, _endLocation, testMap);// Finds the most efficient path from the start to the end location, and stores it as
                                    // a list of vector2s.
                                    if (tempNodes != null && tempNodes.Count > 0)// If a path can be found(meaning the map fulfills all of the criteria for a valid map).
                                    {
                                        Game1.MapTextures.Add(Game1.TheGame.ConvertBitmapToTexture2D(Map));// Converts the map to a texture2D(which can be drawn by the sprite batch).
                                        Map temp = new Map(Map, _startLocation, _endLocation);
                                        Game1.Maps.Add(temp);
                                        string fileName = "Level" + Directory.GetFiles("LevelFiles").Length;
                                        Game1.WriteToBinaryFile("LevelFiles/" + fileName + ".tdl", temp);

                                        MessageBox.Show("Level successfully added!");// Notifies the user that their level has been added.
                                        // Sets the text boxes to empty, so that another level can be added if the user wants to.
                                        ((TextBox)Game1.TheGame.Rooms[Room]["StartXPosition"]).TextBoxString = "";
                                        ((TextBox)Game1.TheGame.Rooms[Room]["StartYPosition"]).TextBoxString = "";
                                        ((TextBox)Game1.TheGame.Rooms[Room]["EndXPosition"]).TextBoxString = "";
                                        ((TextBox)Game1.TheGame.Rooms[Room]["EndYPosition"]).TextBoxString = "";
                                        // Resets start and end locations so another level can be added.
                                        _startLocation = Vector2.Zero;
                                        _endLocation = Vector2.Zero;
                                        Map.Dispose();// Disposes of the map bitmap, freeing its resources.
                                        Map = null;// Nullifies the variable, so that the user must add another map image to use this button.
                                    }
                                    else
                                    {
                                        // Tells the user that there is not a valid path between their specified start and end locations.
                                        MessageBox.Show("A valid path for enemies to traverse cannot be found. You must ensure that there is a valid route of 0 alpha colour from the start to the end position, where the path consists of 32x32px blocks of 0 alpha colour corresponding with a 32x32 grid on the map image.");
                                    }
                                }
                                else
                                {
                                    // Tells the user that they have entered invalid start and end locations.
                                    MessageBox.Show("The x components of the start and end locations must be between 0 and 1088 inclusive.\nThe y components of the start and end locations must be between 0 and 768 inclusive.\nThe start and end locations must not be equal.");
                                }
                            }
                            catch
                            {
                                // Ignored.
                            }
                        }
                        else
                        {
                            // Tells the user that they cannot add any more levels.
                            MessageBox.Show("You cannot have more than 10 levels. Delete some before adding more.");
                        }
                    }
                    else
                    {
                        // Tells the user that they have not loaded a map, or have loaded an invalid map image.
                        MessageBox.Show("You have not opened a valid map image. Level not added.");
                    }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class StartGame : MenuButton// Button which, when pressed, changes the current room to room 4(the level select room).
    {
        public static bool Show;
        public StartGame(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe() && Game1.TheGame.Connection == Game1.ConnectionState.Server && Show)// If the user has pressed this button, and is hosting,
                // and a client is connected.
            {
                Game1.TheGame.LastRoom = Game1.TheGame.Room;// Sets the previous room as the current room.
                Game1.TheGame.Room = 4;// Sets the current room to room 4(the level select room).
                
            }
            base.Update();// Calls the inherited subroutine.
        }
        public override void Draw()// Only draws this button if the user is the server; the client cannot start the game. Also client must be connected.
        {
            if (Game1.TheGame.Connection == Game1.ConnectionState.Server && Show)
            {
                base.Draw();
            }
        }
    }
    [Serializable]
    public class MultiPlayer : MenuButton// Button which, when pressed, changes the current room to room 2(the multiplayer room), and sets
        // any default values required for the multiplayer room.
    {
        public MultiPlayer(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {
            
        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());// Gets an array of the ip addresses of this device.
                foreach (IPAddress address in localIPs)// For each of the ip addresses in the array.
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)// If the iterated address is the IPv4 address.
                    {
                        Game1.TheGame.ServerBox.TextBoxString = address.ToString();// Set the server box text box text to the ip address of this device,
                        // so that the user can view it and give it to another player on their LAN, so that the other player can connect.
                        break;// Breaks from the loop, as no further ip addresses are required.
                    }
                }
                StartServer.On = false;
                JoinServer.On = false;
                Game1.TheGame.LastRoom = Game1.TheGame.Room;// Sets the previous room to the current room.
                Game1.TheGame.Room = 2;// Sets the current room to the multiplayer room.
                Game1.TheGame.IsMultiplayer = true;// The user is playing multiplayer.
                Game1.TheGame.CurrentHealth = 1000;// Sets the castle current health back to the default of 1000.
                Game1.TheGame.MaxHealth = 1000;// Sets the castle maximum health back to the default of 1000.
                Game1.TheGame.EnemyMoney = 1000;// Sets the money for enemy production back to the default of 1000.
                Game1.TheGame.TowerMoney = 1000;// Sets the money of tower production back to the default of 1000.
                Game1.TheGame.GameScreenTime = 0;// Reset time spent in game.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class StartServer : MenuButton// Button which, when pressed, begins a server with the specified port, so that a client can connect.
    {
        public static bool On;// If on is true, the device is currently hosting the game, or is ready to host a game.
        public StartServer(String spriteName, double aX, double aY, int room = 0) : base(spriteName,aX,aY,room)
        {
        }
        public override void Update()
        {
            if (MousePressedInMe() && JoinServer.On == false)
            {
                if (On == false)// If the user is not already hosting a server.
                {
                    try// An error can occuring when parsing the port text from the port text box to an integer. This will suppress the error and pass
                        // control to the catch statement.
                    {
                        Game1.Multiplayer.StartServer(int.Parse(Game1.TheGame.PortBox.TextBoxString));// Start a server with the port specified by the port text box.
                        On = true;// Sets on to true, so show that a server is started.
                        Game1.TheGame.Connection = Game1.ConnectionState.Server;// Sets the game connection state to server.
                    }
                    catch// Catches all errors, such as the parsing error, but also any errors that can occur when attempting to start a server.
                    {
                        MessageBox.Show("Invalid port.");// Tell the user that they have entered an invalid string.
                    }
                }
                else// If the user is hosting a server, and they have pressed this button.
                {
                    Game1.Multiplayer.Disconnect();// Stop the server.
                    On = false;// Set on to false, so that the user may start a server again.
                }
            }
            
            base.Update();// Calls the inherited subroutine.
            if (On)// If the user has started a server, force the button sprite to be down, so the user knows that they are hosting the server.
            {
                if (!Sprite.Contains("Down"))
                {
                    Sprite = Sprite + "Down";
                }
            }
        }
    }
    [Serializable]
    public class JoinServer : MenuButton// Button which, when pressed, attempts to connect to a server with the ip and port specified by the user.
    {
        public static bool On;// If on is true, the device is connected to a server.
        public JoinServer(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {
        }
        public override void Update()
        {
            if (MousePressedInMe() && StartServer.On == false)// If the button is pressed and the user is not currently hosting a game.
            {
                if (On == false)// If the user is not already connected to a server.
                {
                    try// An error can occur when parsing the textboxes, if the user does not enter a correctly formatted ip address or string.
                    {
                        if (Game1.Multiplayer.ConnectToServer(IPAddress.Parse(Game1.TheGame.ServerBox.TextBoxString),
                            int.Parse(Game1.TheGame.PortBox.TextBoxString)))// If the device successfully connects to the server.
                        {
                            Game1.TheGame.Connection = Game1.ConnectionState.Client;// Set the connection state to client.
                            On = true;// Set On to true, so that the user cannot start a server or join another server.
                        }
                    }
                    catch// Catches all errors, such as the parsing error, but also any errors that can occur when attempting to connect to a server.
                    {
                        MessageBox.Show("Invalid IP address or port. Please write IP address in the format x.x.x.x, where each x is up to 3 numbers long.");
                        // Tell the user that they have entered an invalid string.
                    }
                }
                else// If the user is connected to a server, then pressing the join server button again will end the connection.
                {
                    On = false;// Allows the user to join a server again.
                    Game1.Multiplayer.Disconnect();// Ends all network connections.
                }
            }
            base.Update();// Calls the inherited subroutine.
            if (On)// If the user is connected to a server, force the button sprite to be down, so the user knows that they are connected.
            {
                if (!Sprite.Contains("Down"))
                {
                    Sprite = Sprite + "Down";
                }
            }
            
        }
    }
    [Serializable]
    public class Back : MenuButton// Button which, when pressed, changes the current room back to the previous room.
    {
        public Back(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.TheGame.Room = Game1.TheGame.LastRoom;// Returns to the previous room(set by other buttons when changing the current room).
                Game1.TheGame.LastRoom = 1;
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class MainMenu : MenuButton// Button which, when pressed, changes the current updated/drawn room to room 1(main menu), resetting the game room.
    {
        public MainMenu(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.ReturnToMenu("MainMenu");// Sets the current updated/drawn room to room1, resetting any default values required to start a new game.
            }
            base.Update();
        }
    }
    [Serializable]
    public class PaintMode : MenuButton// Button which, when pressed, allows the user to set the pixels of squares on the map to be black.
    {
        public PaintMode(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.TheGame.IsPainting = true;// When the user clicks the map, they will paint the 32x32px square that the cursor is on black, 
                // if the square has 0 alpha.
            }
            base.Update();// Calls the inherited constructor.
        }
    }
    [Serializable]
    public class EraseMode : MenuButton// Button which, when pressed, allows the user to set the pixels of squares on the map to have 0 
        // as their alpha components.
    {
        public EraseMode(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.TheGame.IsPainting = false;// When the user clicks the map, they will erase the 32x32px square that the cursor is on, if the 
                // square is painted.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class Clear : MenuButton// Button which, when pressed, clears the map that the user is currently drawing, so a new map can be drawn.
    {
        public Clear(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.PaintingLevel = new Texture2D(Game1.TheTowerDefenceGame.GraphicsDevice, 1088, 768);// Creates a new, empty map,
                
                // with the correction dimensions of 1088px by 768px, as a Texture2D with the base game's graphics device.
                Game1.TheGame.PaintingMap = new bool[34, 24];// Creates a new 2D array of booleans to store whether each 32x32px
                // square on the map is black or transparent.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class LevelEditor : MenuButton// Button which, when pressed, changes the currently updated/drawn room to room 5(the level editor),
        // clearing the map that the user draws to.
    {
        public LevelEditor(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.CurrentMap = new Texture2D(Game1.TheTowerDefenceGame.GraphicsDevice, 1088, 768);// Creates a new, empty map,
                // with the correction dimensions of 1088px by 768px, as a Texture2D with the base game's graphics device.
                Game1.TheGame.PaintingMap = new bool[34, 24];// Creates a new 2D array of booleans to store whether each 32x32px
                Game1.PaintingLevel = new Texture2D(Game1.TheTowerDefenceGame.GraphicsDevice, 1088, 768);
                // square on the map is black or transparent.
                Game1.TheGame.LastRoom = Game1.TheGame.Room;// Holds the current room, that the back button navigates to.
                Game1.TheGame.Room = 5;// Sets the current room to the level editor room.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class SaveMap : MenuButton// Button which, when pressed, allows the user to enter the file path to save the map image.
    {
        public SaveMap(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                MemoryStream streamFile = new MemoryStream();// Creates a new memory stream to hold the serialized map.
                Game1.PaintingLevel.SaveAsPng(streamFile, 1088, 768);// Saves the current map as a PNG to the memory stream.
                Game1.TheGame.SaveImageFile(new Bitmap(streamFile));// Calls the Game1 procedure to save the bitmap version of the
                // current map to a file.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class OpenMap : MenuButton// Button which, when pressed, allows the user to select an image for loading as a map.
    {
        public OpenMap(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (I.MousePressed(I.Mouses.Right) && CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y))// If the user right clicks this button.
            {
                MessageBox.Show("Images opened for use as maps must have the following properties:\n An alpha colour as the path\n A non-alpha colour as the walls\n A navigable path from the start to the end position\n A width of 1088 pixels and a height of 768 pixels.\n\nPath nodes are occur every 32 pixels in both the x and y directions starting from position (16,16).");
                // Notifies the user of the conditions required for the image they select, for use as a map image.
            }
            else if (MousePressedInMe())// Otherwise if the user has left clicked the button.
            {
                Texture2D LoadedImage = Game1.TheGame.ConvertBitmapToTexture2D(Game1.TheGame.LoadImage());// Loads the selected map image as the
                // current map if it is valid(as specified by the previous message box), otherwise will load null. If it is loaded
                // successfully, it is converted into a Texture2D, which can be drawn by the sprite batch, and stored in the game data
                // instance.
                if (LoadedImage != null)
                {
                    Game1.PaintingLevel = LoadedImage;
                }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class Instructions : MenuButton// Button which, when pressed, changes the currently updated/drawn room to room 3(the instructions room).
    {
        public Instructions(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.TheGame.LastRoom = Game1.TheGame.Room;// Holds the current room, that the back button navigates to.
                Game1.TheGame.Room = 3;// Sets the current room to the instructions room.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class ViewEnemyMap : MenuButton// Button which, when hovered over with the cursor, displays the progress of the enemies that
        // the user has sent to the other user's screen, if the user is playing multiplayer.
    {
        public ViewEnemyMap(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (Game1.TheGame.IsMultiplayer)
            {
                Game1.TheGame.ShowEnemyMap = CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y);// Marks the game data to draw the
                // DrawingEnemies if the cursor location lies within the CollisionBox(/sprite rectangle) of the button.
                base.Update();// Calls the inherited subroutine.
            }
        }
        public override void Draw()
        {
            if (Game1.TheGame.IsMultiplayer)
            {
                base.Draw();
            }
        }
    }
    [Serializable]
    public class ExitGame : MenuButton// Button which, when pressed, closes the game.
    {
        public ExitGame(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                Game1.TheTowerDefenceGame.Exit();// Calls the built-in Exit procedure in the base game instance.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class AccessLevelFiles : MenuButton// Button which, when pressed, opens the file location where the level files are stored, in
        // case the user wants to quickly rearrange, add or delete one or more level files.
    {
        public AccessLevelFiles(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (CollisionBox.Contains(I.NewMouse.X, I.NewMouse.Y) && I.MousePressed(I.Mouses.Right))
            {
                MessageBox.Show("Opens the level files directory so that files can be deleted, rearranged, imported, or exported. Levels will not be reloaded until the game is restarted.");
                // Levels aren't reloaded until the game restarts as it may cause delay if they were loaded repeatedly during the game.
            }
            if (MousePressedInMe())
            {
                Process.Start(@"LevelFiles");// Opens the level files folder in windows explorer.
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class SaveGame : MenuButton// Button which, when pressed, saves game data files.
    {
        public SaveGame(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)
        {

        }// Calls the inherited constructor.
        public override void Update()
        {
            if (MousePressedInMe())
            {
                SaveFileDialog saveGameDialog = new SaveFileDialog();// Creates a new save file dialog so the user can select where to save the file.
                if (Game1.TheGame.IsMultiplayer == false)// If the user is playing single player,
                {
                    saveGameDialog.Filter = "Single Player Tower Defence Game File(*.stg)|*.stg";// Allow the user to load single player files.
                }
                else if (Game1.TheGame.Connection != Game1.ConnectionState.Client)// If the user is playing multiplayer and they are not the client.
                {
                    saveGameDialog.Filter = "Multiplayer Tower Defence Game File(*.mtg)|*.mtg";// Allow the user to load multiplayer files.
                }
                else// Otherwise if they are the client.
                {
                    MessageBox.Show("Cannot save on client machine. The host(the computer which started the server) must save the game.");
                    // Notify the user that only the host can save the game.
                    return;// return from subroutine, skipping any remaining code.
                }
                saveGameDialog.Title = "Save Game File";// Sets the save file dialog title.
                if (saveGameDialog.ShowDialog() == DialogResult.OK)// Shows the dialog; if the user has entered a valid save path and has pressed the OK button.
                {
                    if (Game1.TheGame.IsMultiplayer)
                    {
                        Game1.Multiplayer.SendObject("Save");// Sends a save message to the other user, so their device will send their game data back.
                        Game1.SavePath = saveGameDialog.FileName;// Used to hold the selected path until the other user's game data is recieved.
                        Game1.GameForStorage = Game1.TheGame.Duplicate();// Used to hold a duplicate of the current game data instance, that will not be updated/changed
                        // until stored. This is done because the server's game will continue to run whilst waiting for the game data instance from the client, so a 
                        // frozen copy of the game data is needed, which will be the game data that is stored.
                    }
                    else
                    {
                        Game1.WriteToBinaryFile(saveGameDialog.FileName, Game1.TheGame);
                    }
                }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
    [Serializable]
    public class OpenGame : MenuButton// Button which, when pressed, loads game data files.
    {

        public OpenGame(String spriteName, double aX, double aY, int room = 0)
            : base(spriteName, aX, aY, room)// Calls the inherited constructor.
        {

        }
        public override void Update()
        {
            if (MousePressedInMe())
            {
                OpenFileDialog openGameDialog = new OpenFileDialog// Creates a new open file dialog, so the user can select a file to load.
                {
                    Filter =// Specifies the file extensions that the user can open.
                        Game1.TheGame.IsMultiplayer == false// If the game is not multiplayer.
                            ? "Single Player Tower Defence Game File(*.stg)|*.stg"// The user can open single player files.
                            : "Multiplayer Tower Defence Game File(*.mtg)|*.mtg",// otherwise the user can open multiplayer files.
                    Title = "Open Game File",// Sets the title of the dialog.
                    Multiselect = false// The user is not allowed to open multiple files at once.
                };
                if (openGameDialog.ShowDialog() == DialogResult.OK)// Opens the open file dialog; if the user presses ok, and has selected a valid file.
                {
                    if (Game1.TheGame.IsMultiplayer)// If the user is playing multiplayer.
                    {
                        Game1[] tempGames = Game1.ReadFromBinaryFile<Game1[]>(openGameDialog.FileName);//An array with 2 elements to store the game data for the
                        // current player and the other player.
                        tempGames[0].GameDataForOpening = true;// Marks the game data for the other player so that their game knows that the game data is to be
                        // loaded.
                        Game1.Multiplayer.SendObject(tempGames[0]);// Sends the game data for the other player to them.
                        TowerDefenceGame.GameDataInstance = tempGames[1];// Sets the current game data instance to the loaded game data instance, to restore the game
                        // to its previous state.
                        
                    }
                    else// If the user is playing single player.
                    {
                        TowerDefenceGame.GameDataInstance = Game1.ReadFromBinaryFile<Game1>(openGameDialog.FileName);// Loads the game data from the file path that the
                        // user selected in the dialog.
                    }
                    Game1.TheGame = TowerDefenceGame.GameDataInstance;// Sets the static reference in the game data class to the loaded game data instance.
                    Game1.CurrentMap = Game1.TheGame.ConvertBitmapToTexture2D(Game1.TheGame.CurrentMapBitmap);// Converts the loaded map image to a Texture2D, and
                    // sets the game data instance current map to it, as the Texture2D version cannot be loaded.
                }
            }
            base.Update();// Calls the inherited subroutine.
        }
    }
[Serializable]
public class DrawingEnemy// Used when the user sends an enemy to the other user on Multiplayer, to represent an Enemy using minimal data
    // for faster sending and minimal memory usage.
    {
        readonly string _sprite;// The key of the sprite in the SpriteList to be drawn.
        readonly short _x;// The x location to draw the _sprite.
        readonly short _y;// The y location to draw the _sprite.
        readonly float _radianRotation;// The rotation in radians to draw the sprite at.
    /// <summary>
    /// Creates a new DrawingEnemy, with the specified sprite key, position, and rotation.
    /// </summary>
    /// <param name="spriteName">The key of the sprite in the SpriteList to be drawn for this instance.</param>
    /// <param name="aX">The x position of this instance, to draw the sprite at.</param>
    /// <param name="aY">The y position of this instance, to draw the sprite at.</param>
    /// <param name="rotation">The rotation to draw the sprite at, in radians.</param>
        public DrawingEnemy(String spriteName, short aX, short aY, float rotation)
        {
            _sprite = spriteName;
            _x = aX;
            _y = aY;
            _radianRotation = rotation;
        }
    /// <summary>
    /// Draws the _sprite at the _x and _y position on the screen, rotated by _radianRotation, about the centre of the sprite.
    /// </summary>
        public void Draw()
        {
            Game1.SpriteBatch.Draw(Game1.SpriteList[_sprite], new Rectangle(_x, _y, Game1.SpriteList[_sprite].Width, Game1.SpriteList[_sprite].Height), new Rectangle(0, 0, Game1.SpriteList[_sprite].Width, Game1.SpriteList[_sprite].Height), Color.FromNonPremultiplied(255,255,255,175), _radianRotation, new Vector2(Game1.SpriteList[_sprite].Width / 2, Game1.SpriteList[_sprite].Height / 2), SpriteEffects.None, 0);
        }
    }

[Serializable]
public class Upgrade// Used to upgrade towers at a cost, and are stored as a List in the tower that it can upgrade.
    {
        public Action UpgradeEvent;// The anonymous subroutine that is run to upgrade the tower.
        public int Cost;// The amount of TowerMoney the user spends to run the UpgradeEvent.
        public string UpgradeMessage;// A description displayed when the user selects a tower with available upgrades.
    /// <summary>
    /// Create a new upgrade for a tower, which calls an event when the user presses the upgrade button and has sufficient money.
    /// </summary>
    /// <param name="upgradeevent">The Action to run when the user upgrades the tower and has sufficient money.</param>
    /// <param name="cost">The amount of money that the user requires to upgrade.</param>
    /// <param name="message">A message to display to the user when the tower of this upgrade is selected.</param>
        public Upgrade(Action upgradeevent, int cost, string message)
        {
            UpgradeEvent = upgradeevent;
            Cost = cost;
            UpgradeMessage = message;
        }
    /// <summary>
    /// Upgrades the tower which holds this upgrade, only if the user can afford it, and negates the Cost away from Game1.TowerMoney.
    /// </summary>
        public bool TryUpgrade()
        {
            if (Game1.TheGame.TowerMoney >= Cost)// If the user can afford this upgrade.
            {
                Game1.TheGame.TowerMoney -= Cost;// Subtracts the cost of the upgrade from the user's TowerMoney.
                UpgradeEvent();// Runs the upgrade event.
                return true;
            }
            return false;
        }

    }
[Serializable]
public class Map// Used to store map images with their respective start and end locations. Stored as a Bitmap as a Texture2D
    // is not Serializable.
    {
        public Bitmap MapImage;// The Bitmap of the image of this map.
        public Vector2 StartPosition;// Where enemies are placed on the map when instantiated.
        public Vector2 EndPosition;// The location of the castle, where enemies will travel to, and make the player lose lives.
    /// <summary>
    /// Creates a Map with the specified Bitmap and start and end locations.
    /// </summary>
    /// <param name="mapimage">The Bitmap of the image of the map to be stored.</param>
    /// <param name="startposition">The location that enemies will start in when created.</param>
    /// <param name="endposition">The location that enemies will travel to, and where the castle is, thus causing the player to lose lives.</param>
        public Map(Bitmap mapimage, Vector2 startposition, Vector2 endposition)
        {
            MapImage = mapimage;
            StartPosition = startposition;
            EndPosition = endposition;
        }
    public Map(Texture2D mapimage, Vector2 startposition, Vector2 endposition)
        {
            MemoryStream streamFile = new MemoryStream();
            mapimage.SaveAsPng(streamFile, 1088, 768);
            MapImage = new Bitmap(streamFile);
            StartPosition = startposition;
            EndPosition = endposition;
        }
    }
public static class TextureContent
{
    /// <summary>
    /// Loads all the resources of specified or implicit type T using the specified content manager and root folder to load from.
    /// </summary>
    /// <typeparam name="T">The type of the files that will be loaded.</typeparam>
    /// <param name="contentmanager">The content manager for the game.</param>
    /// <param name="contentfolder">The root folder where the content is stored.</param>
    /// <returns>A dictionary where the keys are the file names, and the values is the file contents.</returns>
    public static Dictionary<string, T> LoadListContent<T>(this ContentManager contentmanager, string contentfolder)
    {
        // DirectoryInfo used to iterate through all files in a directory.
        DirectoryInfo directory = new DirectoryInfo(contentmanager.RootDirectory + "/" + contentfolder);
        // If the directory does not exist.
        if (!directory.Exists)
            throw new DirectoryNotFoundException();// Throw an error, specifying that the directory was not found.
        // The directory should always exist as it is part of the code. It only would not exist if the value for the contentfolder was
        // changed in the code, or if the user deleted the content folder; either of which means the game could not run.
        Dictionary<String, T> filepaths = new Dictionary<String, T>();// Creates a dictionary to contain all of the file paths and their
        // respective files, to be loaded.
        FileInfo[] files = directory.GetFiles("*.*");// Creates an array of the info of all the files in the directory.
        foreach (FileInfo file in files)// Iterates through each of the FileInfo in the array.
        {
            string name = Path.GetFileNameWithoutExtension(file.Name);// Gets the file name of the currently iterated file, without the
            // file extension(for example without the .png at the end).
            filepaths[name] = contentmanager.Load<T>(contentfolder + "/" + name);// Loads the currently iterated file using the root 
            // content folder followed by the file name, with the key as its file name, using the contentmanager parameter.
        }
        return filepaths;// Returns the loaded dictionary of files.
    }
}
public class SearchParameters// Stores the data that varies between maps for the path finder.
{
    public Vector2 StartLocation;
    public Vector2 EndLocation;
    public bool[,] Map;
    // Gets the data for use when constructing the object.
    public SearchParameters(Vector2 startlocation, Vector2 endlocation, bool[,] map)
    {
        StartLocation = startlocation;
        EndLocation = endlocation;
        Map = map;
    }
}
public class PathFinder// Finds paths for enemies to take.
{
    private int _width;// The width of the map used.
    private int _height;// The height of the map used.
    public Node[,] Nodes;// The nodes on the map.
    public Node Startnode;// The node to start finding a path from.
    public Node Endnode;// The node to find a path to.
    private readonly SearchParameters _searchParameters;// The data that varies between maps.
    // Gets the search parameters, initialises the nodes, and sets the start and end nodes.
    public PathFinder(SearchParameters searchParameters)
    {
        _searchParameters = searchParameters;
        InitializeNodes(searchParameters.Map);
        Startnode = Nodes[(int)searchParameters.StartLocation.X, (int)searchParameters.StartLocation.Y];
        Startnode.State = TestState.Open;
        Endnode = Nodes[(int)searchParameters.EndLocation.X, (int)searchParameters.EndLocation.Y];
    }
    // Finds a path from the start position to the end position.
    public List<Vector2> FindPath()
    {

        List<Vector2> path = new List<Vector2>();// Creates an empty path.
        bool success = Search(Startnode);// Searches to see if there is a path available from the start to the end positions.
        if (success)// If there is a valid path.
        {
            Node node = Endnode;// Starting with the end node as the "iterated node", add each iterated node to the path and then repeat for that node's parents, until it reaches the start node.
            while (node.ParentNode != null)// 
            {
                path.Add(node.Location);
                node = node.ParentNode;
            }
            path.Reverse();// Reverse the path, so it goes from the start to the end.
        }

        return path;// Returns the finished path back to the caller.
    }
    /// <summary>
    /// Creates the nodes for the map and uses the specified 2d bool array as a map to determine whether each node on the map can be used by Enemies.
    /// </summary>
    /// <param name="map">A 2D bool array to represent the map.</param>
    private void InitializeNodes(bool[,] map)
    {
        _width = map.GetLength(0);// Sets the total size of the map as the size of the bool array specified.
        _height = map.GetLength(1);
        Nodes = new Node[_width, _height];// Sets the 2D array of nodes to the width and height of the map, so it can contain them all.
        for (int y = 16; y < _height; y+= 32)// For the position of each node to be created (each node is on a 32x32 grid).
        {
            for (int x = 16; x < _width; x+= 32)
            {
                Nodes[x, y] = new Node(x, y, map[x, y], _searchParameters.EndLocation);// Create a node at the iterated position, using the specified map to determine whether
                // it can be used as a path by Enemies.
            }
        }

    }
    /// <summary>
    /// Iterates through nodes from the start position to the end position to find the shortest path, storing the path in the nodes.
    /// </summary>
    /// <param name="currentNode">The node currently being searched.</param>
    /// <returns>Returns true if there is a path from the start to the end positions.</returns>
    private bool Search(Node currentNode)
    {
        currentNode.State = TestState.Closed;// Sets the node that is about to be processed to closed, so that it is only processed once.
        List<Node> nextNodes = GetAdjacentWalkableNodes(currentNode);// Gets all eligible nearby nodes.
        nextNodes.Sort((node1, node2) => node1.EstimatedTotalDistance.CompareTo(node2.EstimatedTotalDistance));
        // Sorts the eligible nodes so the one with the least distance to the end and with the least distance through its parent nodes from the start is first.
        foreach (var nextNode in nextNodes)// Iterates through each eligible node.
        {
            if (nextNode.Location == Endnode.Location)// If the end has been found, return true, as a path to the end has been found.
            {
                return true;
            }
            if (Search(nextNode))// If the next node is not the end node, however it connects to the end node through other nodes, return true.
                return true;
        }
        return false;// Returns false if no path to the end has been found through this node.
    }
    /// <summary>
    /// Get nearby nodes that are yet to be processed and set their attributes to store data about the path found.
    /// </summary>
    /// <param name="fromNode">The nodes found will be adjacent to this specified nodes.</param>
    /// <returns>A list of eligible nodes to search.</returns>
    private List<Node> GetAdjacentWalkableNodes(Node fromNode)
    {
        List<Node> walkableNodes = new List<Node>();// Creates an empty list to store all the eligible nodes in.
        Vector2[] nextLocations = GetAdjacentLocations(fromNode.Location);// The locations where adjacent nodes can be found.
        foreach (var location in nextLocations)// Iterates over each adjacent location.
        {
            int x = (int)location.X;// Converts the location to integers.
            int y = (int)location.Y;

            if (x < 0 || x >= _width || y < 0 || y >= _height)// If the currently iterated location lies outside of the maps bounds, skip over it, as nodes cannot be found outside
                // the map.
                continue;

            Node node = Nodes[x, y];// Gets the node at the currently iterated location.
            if (!node.IsWalkable)// If the node cannot be used in the path, skip it.
                continue;

            if (node.State == TestState.Closed)// If the node is closed, it has already been processed, so it does not need processing again, so is skipped.
                continue;

            if (node.State == TestState.Open)// If the node is open, it has already got a route to it, though a shorter one may be available.
            {
                float traversalCost = Node.GetTraversalCost(node.Location, node.ParentNode.Location);// Gets the distance between the iterated node and its parent node.
                float gTemp = fromNode.DistanceFromStart + traversalCost;// Gets the distance covered by travelling to the iterated node through this node's route to the start
                // (its route is found by going through this node's parent nodes).
                if (gTemp < node.DistanceFromStart)// If the route used by the parameter node to reach the iterated node is shorter than the route already used to reach the
                    // iterated node.
                {
                    node.ParentNode = fromNode;// Set the parent node of the iterated to the parameter node, so the shortest route is used to get to the iterated node.
                    walkableNodes.Add(node);// Adds the node to the list of eligible nodes for seaching.
                }
            }
            else// If a route to the iterated node has not been found at all, so no processing has been done on it.
            {
                node.ParentNode = fromNode;// Set its parent node to this node, as this node is the only available route.
                node.State = TestState.Open;// Set the node to open, so that if it is processed again by another node, the node will know there is already a route to it.
                walkableNodes.Add(node);// Adds the node to the list of eligible nodes for searching.
            }
        }
        // Returns the eligible nodes.
        return walkableNodes;
    }
    /// <summary>
    /// Gets the 8 nearest locations to the specified location on the grid.
    /// </summary>
    /// <param name="fromLocation">The location to find adjacent locations for.</param>
    /// <returns>Returns an array of possible locations.</returns>
    private static Vector2[] GetAdjacentLocations(Vector2 fromLocation)
    {
        return new Vector2[]
            {
                new Vector2(fromLocation.X-32, fromLocation.Y-32),
                new Vector2(fromLocation.X-32, fromLocation.Y  ),
                new Vector2(fromLocation.X-32, fromLocation.Y+32),
                new Vector2(fromLocation.X,   fromLocation.Y+32),
                new Vector2(fromLocation.X+32, fromLocation.Y+32),
                new Vector2(fromLocation.X+32, fromLocation.Y  ),
                new Vector2(fromLocation.X+32, fromLocation.Y-32),
                new Vector2(fromLocation.X,   fromLocation.Y-32)
            };
    }
}
public enum TestState
{
    Untested,
    Open,
    Closed
}
public class Node
{
    private Node _parentnode;
    public Vector2 Location;
    public bool IsWalkable;
    public float DistanceFromStart;
    public float DistanceToEnd;
    public TestState State;
    public float EstimatedTotalDistance
    {
        get { return DistanceFromStart + DistanceToEnd; }
    }
    public Node ParentNode
    {
        get { return _parentnode; }
        set
        {
            _parentnode = value;
            DistanceFromStart = _parentnode.DistanceFromStart + GetTraversalCost(Location, _parentnode.Location);
        }
    }
    public Node(int x, int y, bool isWalkable, Vector2 endLocation)
    {
        Location = new Vector2(x, y);
        State = TestState.Untested;
        IsWalkable = isWalkable;
        DistanceToEnd = GetTraversalCost(Location, endLocation);
        DistanceFromStart = 0;
    }
    internal static float GetTraversalCost(Vector2 location, Vector2 otherLocation)
    {
        float deltaX = otherLocation.X - location.X;
        float deltaY = otherLocation.Y - location.Y;
        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
[Serializable]
public class StaticEnemy// Used in the waves, to represent an enemy but use up less memory than storing an actual Enemy.
{
    public string Sprite;// The key of the Texture2D in the game data instance sprite list that is drawn to the screen for this instance.
    public Type EnemyType;// One of the six types of enemies that will be created from this instance.
    /// <summary>
    /// Creates a new StaticEnemy with the specified sprite key and enemy type.
    /// </summary>
    /// <param name="spriteName">A key in Game1.SpriteList which holds the </param>
    /// <param name="enemytype"></param>
    public StaticEnemy(string spriteName,Type enemytype)// The constructor which sets the sprite and enemytype when instantiated.
    {
        Sprite = spriteName;
        EnemyType = enemytype;
    }
}
[Serializable]
public class Game1
{
    public static Game1 GameForStorage;// Frozen game state used when saving on multiplayer.
    public static string SavePath;// The location to save the game when the client's game data has been recieved.
    public static ConnectionHandler Multiplayer = new ConnectionHandler();// Creates a new instance to handles multiplayer connections.
    public static Game1 TheGame;// The current instance of the game data in use.
    public static TowerDefenceGame TheTowerDefenceGame;// The main base of the game.
    public static GraphicsDeviceManager Graphics;// The graphics device manager handles window display and drawing.
    public static SpriteBatch SpriteBatch;// The SpriteBatch is used to draw all textures and primitives to the screen.
    public static ContentManager Resources;// The ContentManager is used to import all textures at the beginning of the game.
    public int LastRoom = 1, Room = 1;// The LastRoom is the room which will be navigated to when pressing the back button. 
    // The Room is an index in Rooms which indicates which dictionary of Instances is current being updated and drawn.
    public List<SortedDictionary<string, Instance>> Rooms = new List<SortedDictionary<string, Instance>>();// Where all Instances are kept, listed by room.
    public Random Rand = new Random();// The variable which all random numbers are generated using.
    public static Dictionary<string, Texture2D> SpriteList = new Dictionary<string, Texture2D>();
    // The SpriteList is a list of all sprites loaded from the content manager at the start of the game.
    // Although other data types such as a byte, short or guid offer better performance, the performance gain is marginal, especially considering that
    // the length of the strings used as keys in the dictionary are quite short. It is much more readable and memorable to use strings as keys.
    public ulong NextInstanceId;// The number appended to the end of the InstanceName of Instances to ensure it is a unique key. Can only be positive 
    // and can contain large values so large quantities of instances can be created. Also as it is only one variable it uses only 4 extra bytes so
    // is well worth the maginal cost in memory usage.
    public int GameWidth = 1366;// The width of the drawing area inside the game window.
    public int GameHeight = 768;// The height of the drawing area inside the game window.
    public List<Instance> ToBeCreated = new List<Instance>();
    public List<Instance> ToBeDestroyed = new List<Instance>();// A List of Instances to be destroyed after the room has finished being processed.
    // Instances cannot be removed/added from Rooms whilst being processed in the Update subroutine, as it will throw an error as it would change the
    // value of the List while being iterated over; so a reference of the Instance must be put into ToBeDestroyed and have its reference removed/put
    // into ToBeCreated and have its reference added after iteration.
    public int PathMaxX = 1088, PathMaxY = 768;//The width and height of the area of the screen which could potentially be used by Enemy and Tower instances.
    public static SpriteFont Arial10;// The only font used by the game.
    
    // Some Instances have references in addition to being contained in Rooms, as they are directly called upon by code, or their variables need to be 
    //accessed directly. I created references for them as referring to them using their InstanceName may not work, as the Instance ID part of the 
    //Instance name can change depending upon what order Instances are created. I have marked these with a *.

    public TowerPlacer Towerplacer;// Used to place towers at the cursor's position.*
    public TextBox ServerBox, PortBox;// The TextBoxes where the user will input the IP and port of the server they are trying to create/join.*
    public int TowerMoney, EnemyMoney;// The amount of money the user will start the game with.
    public int CurrentHealth = 1000, MaxHealth = 1000;// The amount of health the user's base will start the game with.
    public bool BeamPair;// A boolean used when creating BeamTowers to determine whether it is the first in a pair of BeamTowers to be placed.
    public static double[,] ElementCompare = new double[6, 6];// The 2d array of damage multipliers to modify the damage output of towers against
    // specific enemies by.
    // Used in the format of ElementCompare[OffensiveInstance,DefendingInstance].
    public UpgradeButton Upgradebutton;// The button which appears when selecting a tower for the user to upgrade the tower.*
    public DeleteButton Deletebutton;// The button which appears when selecting a tower for the user to delete the tower.*
    public double EnemyMultiplier = 1;// The multiplier of the cost, speed and health of enemies produced and sent to the other player
    // on multiplayer.
    public bool[,] Map;// A 2d array of which 32x32 areas of the game are available for use for Towers and Enemies.
    public List<Vector2> Path;// A List of positions which incoming Enemies will take to get to the user's base.
    public static List<Map> Maps;// A List of Maps; one of which can be chosen when beginning the game.
    public static List<Texture2D> MapTextures;// A List of the image data of the Maps.
    public AddLevel AddLevelButton;// The button that, when pressed, will add a new level to memory and storage.
    public int SelectedMap;// The index of the Map selected from Maps and MapTextures.
    public Vector2 StartPos;// The position where incoming enemies are created, and the beginning of the Path.
    public Vector2 EndPos;// The position where incoming enemies will attempt to reach, and the end of the Path. Health will be lost if Enemies reach this position.
    public bool IsPainting = true;// If true, the player will draw in the level editor. If false, the player will erase.
    public bool[,] PaintingMap = new bool[34, 24];// The 2d array which stores if 32x32 areas of the level editor are painted or not.
    public static Texture2D PaintingLevel;// The game usable image version of the PaintingMap.
    public ConnectionState Connection = ConnectionState.NoConnection;// Determines whether the user's device is a client, server, or has no connection.
    public bool ShowEnemyMap;// If true, the mouse is over the ViewEnemyMap button and the user can see the progress of enemies they have sent to the
    // other player, if on multiplayer.
    public bool IsMultiplayer;// If true, the user is playing networked with another user. If false, the user is playing single player.
    public List<DrawingEnemy> DrawingEnemies = new List<DrawingEnemy>();// The List of enemies that can be displayed that are sent by the other player.
    public List<DrawingEnemy> IncomingDrawingEnemies = new List<DrawingEnemy>();// The List of enemies that can be displayed by the other player of
    // enemies that have been sent.
    // These buffers are used to create smoothness in the drawing of the enemies, as if there is a temporary connection problem then the drawing of the enemies will
    // continue at the same rate. Without a buffer, on some frames, no enemies may be drawn at all as the enemies may be processed faster than they are recieved.
    // Also, if enemies are recieved faster than they are processed, then any extra lists stored in the buffer can be erased, as this can create a larger gap between
    // the position that enemies are displayed at, and the position that the enemies actually are on the other player's device
    public Queue<List<DrawingEnemy>> IncomingDrawingEnemiesBuffer = new Queue<List<DrawingEnemy>>();// A temporary store for enemies sent by the other player.
    public Queue<List<DrawingEnemy>> DrawingEnemiesBuffer = new Queue<List<DrawingEnemy>>();// A temporary store for enemies sent to the other player.
    public static Texture2D CurrentMap;// The map currently in use. The Path and arrangement of places that towers can be placed upon depend on this.
    public Bitmap CurrentMapBitmap;// The bitmap version of the CurrentMap.
    //The CurrentMapBitmap is used as only the CurrentMap is in a form usable for drawing by the SpriteBatch, however the CurrentMap cannot be serialized.
    //The CurrentMapBitmap is not usable for drawing by the SpriteBatch, but can be serialized.
    public ulong GameScreenTime;// The number of Update()s that have been called since the start of the game. Can only be positive and is large. Must be large
    // so that the game can be played for a very long time.
    public int CurrentLevelTime;// The Update()s that have been called since the start of a wave on single player.
    public int Level;// The index of the current wave of enemies from Waves on single player.
    public StaticEnemy LastWaveObject;// The last enemy of the current wave, used to indicate when the wave should end.
    public List<SortedDictionary<int, StaticEnemy>> Waves = new List<SortedDictionary<int, StaticEnemy>>();// The List of all the enemies that will be
    // sent on single player, as well as when they will be sent.
    public bool GameDataForOpening;// If true, the instance of Game1 recieved will replace the current TheGame. Otherwise, it will be saved in a mtg file.
    public int MoneyTimer = 250;// Times when income is recieved on multiplayer
    /// <summary>
    /// Create a new instance of the game data, where all the game data is stored and processed.
    /// </summary>
    /// <param name="tdg">The main base of the game where the main subroutines are called from.</param>
    public Game1(TowerDefenceGame tdg)
    {
        TheTowerDefenceGame = tdg;// Sets the reference for the main base of the game.
        Graphics = new GraphicsDeviceManager(TheTowerDefenceGame);// Sets the graphics device manager to handle the graphics of the given TowerDefenceGame.
        TheTowerDefenceGame.Content.RootDirectory = "Content";// Specifies the folder where all the sprites are stored, as png files.
    }
    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// This is where all the rooms are created and populated, the levels are created, the sprites are loaded, the maps are loaded, the graphics settings are set,
    /// and any other processing that needs to occur before the game is used.
    /// </summary>
    public void LoadContent()
    {
        TheGame = this;// Sets the reference for the instance of the game data to this(the current game data instance).
        SetElementCompare();// Sets the multipliers for the damage dealt to other instances, dependant on their element.
        
        TheTowerDefenceGame.IsMouseVisible = true;// Sets the cursor to visible; as it is false by default.
        Resources = TheTowerDefenceGame.Content;// Sets the reference for all of the content of the game, i.e. sprites and fonts.
        Arial10 = Resources.Load<SpriteFont>("Arial10");// Sets the only font used in the game to load Arial10 from content.
        // Create a new SpriteBatch, which can be used to draw textures.
        SpriteBatch = new SpriteBatch(TheTowerDefenceGame.GraphicsDevice);

        SpriteList = Resources.LoadListContent<Texture2D>("Sprites");// Loads all PNGs from the content tree and stores them in the sprite dictionary.
        // TODO: use this.Content to load your game content here

        Graphics.PreferredBackBufferWidth = 1366;// Sets the game width.
        Graphics.PreferredBackBufferHeight = 768;// Sets the game height.

        Graphics.ApplyChanges();// Applies the changes made to the graphics device.
        for (int i = 0; i < 6; i++)// Adds 7 rooms to the list of rooms.
        {
            Rooms.Add(new SortedDictionary<string, Instance>());
        }
        // This is where any instances that are present at the start of the game are loaded.
        //////////////////////
        //////MENU ROOM///////
        new SinglePlayer("SinglePlayer", 683, 64, 1);
        new MultiPlayer("Multiplayer", 683, 192, 1);
        new Instructions("Instructions", 683, 320, 1);
        new LevelEditor("LevelEditor", 683, 448, 1);
        new AccessLevelFiles("AccessLevelFiles", 683, 576, 1);
        new ExitGame("ExitGame", 683, 704, 1);
        //////////////////////
        //MULTIPLAYER ROOM////
        ServerBox = new TextBox("Textbox", 683, 256, 2);
        PortBox = new TextBox("Textbox", 683, 336, 2);
        new Instance("Server", 455, 256, 2);
        new Instance("Port", 465, 336, 2);
        new StartServer("StartServer", 342, 512, 2);
        new JoinServer("JoinServer", 1024, 512, 2);
        new StartGame("Start", 406, 672, 2);
        new MainMenu("MainMenu", 960, 672, 2);
        //////////////////////
        //////GAME ROOM///////
        Towerplacer = new TowerPlacer("ArrowTower", 1, 1);

        new TowerButton("ArrowTowerButton", 1120, 32, typeof(ArrowTower), "ArrowTower", 70);
        new TowerButton("BeamTowerButton", 1184, 32, typeof(BeamTower), "BeamTower", 110);
        new TowerButton("WhirlwindTowerButton", 1248, 32, typeof(WhirlwindTower), "WhirlwindTower", 90);
        new TowerButton("LightTowerButton", 1312, 32, typeof(LightTower), "LightTower", 80);
        new TowerButton("WaterTowerButton", 1120, 96, typeof(WaterTower), "WaterTower", 95);
        new TowerButton("MissileTowerButton", 1184, 96, typeof(MissileTower), "MissileTower", 100);
        new TowerButton("WallButton", 1248, 96, typeof(Wall), "Wall", 60);

        new EnemyButton("WaterButton", 1184, 608, typeof(WaterEnemy), "Water", 100);
        new EnemyButton("OrbButton", 1120, 608, typeof(Orb), "Orb", 100);
        new EnemyButton("FireballEnemyButton", 1248, 608, typeof(FireballEnemy), "FireballEnemy", 100);
        new EnemyButton("FlowerEnemyButton", 1312, 608, typeof(FlowerEnemy), "FlowerEnemy", 100);
        new EnemyButton("WindEnemyButton", 1120, 544, typeof(WindEnemy), "WindEnemy", 100);
        new EnemyButton("NormalEnemyButton", 1184, 544, typeof(NormalEnemy), "NormalEnemy", 100);

        Upgradebutton = new UpgradeButton("UpgradeTower", 1, 1);
        Deletebutton = new DeleteButton("DeleteTower", 1, 1);
        new MainMenu("MainMenu", 1238, 704);
        new ViewEnemyMap("ViewEnemyMap", 1227, 373);
        new SaveGame("SaveGame", 1227, 181);
        //////////////////////
        /////INSTRUCTIONS/////
        new Instance("Rules", 342, 284, 3);
        new Instance("ElementalDamageMultipliers", 1024, 384, 3);
        new Back("Back", 342, 620, 3);
        //////////////////////
        /////LEVEL SELECT/////
        TextBox startXPosition = new TextBox("Textbox", 315, 13, 4, false) {InstanceName = "StartXPosition"};
        Rooms[4].Add("StartXPosition", startXPosition);
        TextBox startYPosition = new TextBox("Textbox", 825, 13, 4, false) {InstanceName = "StartYPosition"};
        Rooms[4].Add("StartYPosition", startYPosition);
        TextBox endXPosition = new TextBox("Textbox", 315, 71, 4, false) {InstanceName = "EndXPosition"};
        Rooms[4].Add("EndXPosition", endXPosition);
        TextBox endYPosition = new TextBox("Textbox", 825, 71, 4, false) {InstanceName = "EndYPosition"};
        Rooms[4].Add("EndYPosition", endYPosition);
        new Instance("StartXPosition", 60, 13, 4);
        new Instance("StartYPosition", 570, 13, 4);
        new Instance("EndXPosition", 53, 71, 4);
        new Instance("EndYPosition", 563, 71, 4);
        new AddNewMap("AddNewMap", 256, 148, 4);
        AddLevelButton = new AddLevel("AddLevel", 768, 148, 4);
        new OpenGame("OpenGame", 1164, 159,4);
        new MainMenu("MainMenu", 1164, 53,4);
        try// If an error occurs, suppress the error and goto the catch statement.
        {
            Maps = new List<Map>();// Creates the List of Maps for use for appending to.
            string[] files = Directory.GetFiles("LevelFiles", "*.tdl", SearchOption.AllDirectories);// Gets a List of all the file paths of all
            // the usable level files in the level files directory.
            if (files.Any())// If the directory contains any usable level files.
            {
                int count = 0;// Set the count to 0. This is used to limit the maximum number of level files that can be loaded.
                foreach (string i in files)// Iterates over each of the tdl file paths in the directory.
                {
                    count++;// Increments count by 1.
                    Maps.Add(ReadFromBinaryFile<Map>(i));// Reads the file at the currently iterated file path, and then adds it the the list of maps.
                    if (count >= 10)// The limit of the number of level files is 10.
                    {
                        break;// Breaks from loading any more level files after the limit is reached.
                    }
                }
            }
            else// If the directory doesn't contain any usable level files.
            {
                GenerateDefaultMap();// Generate a default map, so the user can still play even if they have deleted all of the level files.
            }
        }
        catch// This normally occurs if the LevelFiles directory does not exist.
        {
            try// If an error occurs, suppress the error and goto the catch statement.
            {
                Directory.CreateDirectory("LevelFiles");// Create a new level files directory to store the levels in.
                GenerateDefaultMap();// Generate a default map for the user to play in the new level files directory.
            }
            catch// This normally occurs if the user cannot create the level files directory, probably because they do not have write permissions
                // in the application directory.
            {
                // Alerts the user that the game cannot function with level files.
                MessageBox.Show("Cannot create initial level files directory. Please ensure the game has read/write permissions in its directory. Game exiting now.");
                Environment.Exit(0);// Terminates the game.
            }
        }
        finally// This statement occurs whether the try statement succeeds or fails.
        {
            MapTextures = new List<Texture2D>();// Creates the List of maps that are in a format that can be drawn by the sprite batch.
            foreach (Map t in Maps)// For each of the maps that were loaded in the try/catch statement.
            {
                MapTextures.Add(ConvertBitmapToTexture2D(t.MapImage));// Convert the map from the bitmap format they were loaded in the the usable Texture2D format.
            }
        }
        //////////////////////
        /////LEVEL EDITOR/////
        new PaintMode("Paint", 1238, 64, 5);
        new EraseMode("Erase", 1238, 192, 5);
        new Clear("Clear", 1238, 320, 5);
        new OpenMap("OpenMap", 1238, 448, 5);
        new SaveMap("SaveMap", 1238, 576, 5);
        new Back("SmallBack", 1238, 704, 5);
    }
    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public void Update(GameTime gameTime)
    {
        I.UpdateStates();// Updates the state of the keyboard and mouse to their current states.
        if (I.KeyDown(Keys.Escape))// The game exits if the escape key is pressed.
        {
            TheTowerDefenceGame.Exit();
        }
        if (Room == 5)// If the current room is the level editor room.
        {
            if (I.MouseDown(I.Mouses.Left) && I.NewMouse.X < 1088)// If the mouse is left clicked and on the drawable area of the screen.
            {
                int reducedMouseX = I.NewMouse.X / 32;// The x index of the 32x32 square the cursor is in.
                int reducedMouseY = I.NewMouse.Y / 32;// The y index of the 32x32 sqiare the cursor is in.
                if (IsPainting != PaintingMap[reducedMouseX, reducedMouseY])// If the user is attempting to paint, and the current square is not painted:
                    // Or if the user is attempting to erase, and the current square is not erased:
                {
                    int maximisedMouseX = reducedMouseX * 32;// Gets the cursor's x location, rounded to the nearest 32x32 square.
                    int maximisedMouseY = reducedMouseY * 32;// Gets the cursor's y location, rounded to the nearest 32x32 square.
                    Color[] currentPaintingLevel = new Color[835584];// An array of the color data of the level currently being painted, sized at 1088x768.
                    PaintingLevel.GetData(currentPaintingLevel);// Gets the colour data of the level currently being painted, and stores it into the array.
                    if (PaintingMap[reducedMouseX, reducedMouseY] == false && IsPainting)// If the user is trying to paint and the square at the cursor is empty.
                    {
                        PaintingMap[reducedMouseX, reducedMouseY] = true;// Mark the square as filled in.
                        // Iterate over the pixels in the square and fill them in as black.
                        for (int x = maximisedMouseX; x < maximisedMouseX + 32; x++)
                        {
                            for (int y = maximisedMouseY; y < maximisedMouseY + 32; y++)
                            {
                                currentPaintingLevel[y * 1088 + x] = Color.Black;
                            }
                        }
                    }
                    else// Else if erasing:
                    {
                        PaintingMap[reducedMouseX, reducedMouseY] = false;// Mark the square as empty.
                        // Iterate over the pixels in the square and fill them in with transparency.
                        for (int x = maximisedMouseX; x < maximisedMouseX + 32; x++)
                        {
                            for (int y = maximisedMouseY; y < maximisedMouseY + 32; y++)
                            {
                                currentPaintingLevel[y * 1088 + x] = Color.FromNonPremultiplied(0, 0, 0, 0);
                            }
                        }
                    }
                    PaintingLevel = new Texture2D(TheTowerDefenceGame.GraphicsDevice, 1088, 768);// Sets the current painting level to a new texture2D of the same size.
                    PaintingLevel.SetData(currentPaintingLevel);// Set the colour data of the painting level to the updated colour data of the colour array.
                }
            }
        }
        if (Room == 4)// If the current room is the level select room.
        {
            if (I.MousePressed(I.Mouses.Left) && I.NewMouse.Y > 256)// If the left mouse button is pressed and the cursor lies within the list of levels displayed.
            {
                for (int y = 0; y < 2; y++)// Iterates over the location where the levels can be selected.
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (MapTextures.Count <= x + y * 5)// If attempting to process a square without a level displayed in it
                        {
                            goto BreakOuterLoop;// Break out of the nested for loops iterating over the levels that can be selected.
                        }
                        Rectangle buttonBox = new Rectangle(x * 256 + 43, y * 256 + 256, 256, 256);// Create a rectangle at the currently iterated position.
                        if (buttonBox.Contains(I.NewMouse.X, I.NewMouse.Y))// If the rectangle contains the cursor position.
                        {
                            SelectedMap = x + y * 5;// Set the index of the selected map to the selected level.
                            CurrentMap = MapTextures[SelectedMap];// Set the current map texture to the selected map.
                            MemoryStream streamFile = new MemoryStream();// Creates a new memory stream to store the current map in as a serializable bitmap,
                            // as the Texture2D cannot be serialized.
                            CurrentMap.SaveAsPng(streamFile, 1088, 768);// Saves the current map image to the stream in bitmap form.
                            CurrentMapBitmap = new Bitmap(streamFile);// Sets current map bitmap to the bitmap that was stored in the stream.
                            streamFile.Dispose();// Disposes of no longer needed memory stream, to free up resources.

                            Room = 0;// Sets the current room to that of the game room.
                            StartPos = Maps[SelectedMap].StartPosition;// Sets the start position of the map to that of the selected map.
                            EndPos = Maps[SelectedMap].EndPosition;// Sets the end position of the map to that of the selected map.
                            if (IsMultiplayer)// If the game is multiplayer.
                            {
                                Multiplayer.SendObject(new Map(MapTextures[SelectedMap], Maps[SelectedMap].StartPosition, Maps[SelectedMap].EndPosition));
                                //Sends the selected map image, and its start and end positions, to the other user.
                            }
                            GeneratePath();// Generates a viable path for incoming enemies to traverse.
                            new Castle("Castle", EndPos.X, EndPos.Y);// Creates the castle at the end position.
                            goto BreakOuterLoop;// Break out of the nested for loops iterating over the levels that can be selected.
                        }
                    }
                }
            BreakOuterLoop:
                ;
            }
        }
        if (IsMultiplayer)// If the game is multiplayer.
        {
            if (Room == 0)
            {
                MoneyTimer -= 1;
                if (MoneyTimer <= 0)
                {
                    MoneyTimer = 120;
                    EnemyMoney += 10;
                    TowerMoney += 1;
                }
            }
            object incomingObject;// Creates an object to store the object being recieved from the other user.
            do// Ensures the loop is run at least once.
            {
                incomingObject = Multiplayer.GetRecievedObject();// Gets the oldest object recieved from the other user, that has not yet been dequeued.
                if (incomingObject != null)// If there actually is an object recieved from the other user, and they have not sent null.
                {
                    if (incomingObject.GetType() == typeof(List<DrawingEnemy>))// If the object sent by the other user is the list of enemies sent by the user:
                    {
                        if (((List<DrawingEnemy>)incomingObject).Count > 0)// If there were enemies present on the other user's screen at the time of sending.
                        {
                            IncomingDrawingEnemiesBuffer.Enqueue((List<DrawingEnemy>)incomingObject);// Add the List to the buffer, to be drawn later.
                        }
                    }
                    else if (incomingObject is string)// If the incoming object is a string.
                    {
                        string incomingMessage = (string)incomingObject;// Explicitly defines the incoming object as a string.
                        if (incomingMessage.Length > 0)// If the incoming message is not a zero length string.
                        {
                            if (incomingMessage == "MainMenu")// Ends multiplayer connection and sends the user back to the main menu.
                            {
                                ReturnToMenu();
                            }
                            else if (incomingMessage == "Lose")// Tells the player that they have won, and ends connection and return to the main menu.
                            {
                                MessageBox.Show("You win! :D");
                                ReturnToMenu();
                            }
                            else if (incomingMessage.StartsWith("Earn"))
                            {
                                string[] delimitedEarnings = incomingMessage.Split(';');// Splits the incoming message into an array of strings. Each string is separated in the
                                // incoming message by a semicolon (;).
                                // Adds to the users money the int version of the money indicated by the strings at the 1 and 2 indexes of the array of strings.
                                EnemyMoney += int.Parse(delimitedEarnings[1]);
                                TowerMoney += int.Parse(delimitedEarnings[2]);
                            }
                            else if (incomingMessage == "Save")// Requests the user's device to send a copy of the game data to the host for saving.
                            {
                                Multiplayer.SendObject(this);
                            }
                            else if (incomingMessage == "Connected")
                            {
                                StartGame.Show = true;
                            }
                            else if (incomingMessage == "Empty")
                            {
                                IncomingDrawingEnemies.Clear();
                                IncomingDrawingEnemiesBuffer.Clear();
                            }
                            else if (incomingMessage == "Nothing")
                            {
                                ;// Does nothing.
                            }
                            else // Any other string is used for the creation of an enemy sent by the other user.
                            {
                                string[] enemyCreate = incomingMessage.Split(';');
                                // Split the string into its data, in an array of strings, with ; as the delimiter.
                                object[] arguments =
                                    {
                                        enemyCreate[1], StartPos.X, StartPos.Y, 0,
                                        double.Parse(enemyCreate[3])
                                    };
                                // Sets the arguments of the constructor of the enemy to be instantiated
                                // to the incoming data's sprite, and the start position, and the room 0.
                                Enemy d = (Enemy)Activator.CreateInstance(Type.GetType(enemyCreate[0]), arguments);
                                // Uses the activator to create an instance of any type.
                                // Here the activator is used to create one of the types of enemies, using the type specified by the incoming data and the arguments
                                // specified on the previous line.
                                d.Cost = int.Parse(enemyCreate[2]);
                                // Sets the cost of the enemy to the integer version to the 3rd item in the incoming array.
                            }
                        }
                    }
                    else if (incomingObject.GetType() == typeof(Map))// If the incoming object is a map, with the start and end locations attached.
                        // This occurs when the host selected a level, and the client requires the level to match.
                    {
                        Map temp = (Map)incomingObject;// Explicity stores the incoming object as a Map.
                        StartPos = temp.StartPosition;// Sets the start location of the current map to that of the recieved map.
                        EndPos = temp.EndPosition;// Sets the end location of the current map to that of the recieved map.
                        CurrentMapBitmap = temp.MapImage;// Gets the Serializable Bitmap of the recieved map.
                        CurrentMap = ConvertBitmapToTexture2D(temp.MapImage);// Sets the current map to that of the recieved map.
                        
                        Room = 0;// Sets the room to the game room.
                        GameScreenTime = 0;// Sets the amount of time spent in game to 0.
                        GeneratePath();// Generates the navigable path that enemies will follow for the recieved map.
                        new Castle("Castle", EndPos.X, EndPos.Y);// Creates the castle at the end position of the recieved map.
                    }
                    else if (incomingObject.GetType() == typeof(Game1))// If the incoming object is an instance of game data.
                    {
                        if (((Game1)incomingObject).GameDataForOpening)// If the incoming object has been sent to the client for the purpose of resuming a saved game.
                        {
                            TowerDefenceGame.GameDataInstance = (Game1)incomingObject;// Replaces the reference to the current game data in the main base of the game 
                            // with a reference to the game data recieved.
                            TheGame = (Game1)incomingObject;// Replaces the reference to the current game data in this class with a reference to the game data recieved.
                            CurrentMap = ConvertBitmapToTexture2D(((Game1)incomingObject).CurrentMapBitmap);// Sets the current map texture2d to the recieved map.
                            // This has to be done as the CurrentMap from the saved game cannot be serialized and sent between devices, so is stored as static. Instead
                            // the bitmap version of the map is sent.
                            TheGame.GameDataForOpening = false;// If the game data is sent to the host, it is marked to be saved, not to be opened.
                        }
                        else// If the incoming object has been sent to the host for the purpose of saving the current game.
                        {
                            Game1[] games = { (Game1)incomingObject, GameForStorage};//Creates an array of the host's game data and the client's game data,
                            // which can later be loaded by the host for resumption. It is stored on the host's device.
                            WriteToBinaryFile(SavePath, games);// Writes the array of game data to the filepath specified when the host pressed the save game button.
                            GameForStorage = null;// Erases the game data that was frozen upon pressing the save game button, to free up resources.
                            SavePath = null;// Clears the file path chosen by the host as it is no longer needed.
                        }
                    }
                }
            } while (incomingObject != null);// Continue processing incoming data while there is data to be processed.
            if (IncomingDrawingEnemiesBuffer.Count >= 2)// If there is at least 2 lists of enemies in the buffer.
            {
                IncomingDrawingEnemies = IncomingDrawingEnemiesBuffer.Dequeue();// Gets the recieved List of enemies and sets them as the ones to be drawn.
            }
        }
        else if (Room == 0)// If single player and in room 0.
        {
            
            ExecuteWaves();// Send waves of enemies to the player to defend from them.
        }
        DrawingEnemies.Clear();// Clear out the outgoing drawing enemies List.
        bool collectDrawingEnemies = Multiplayer.BufferLength < 6;// Only prepare a new List of drawing enemies to be sent if there is not a long queue of objects already to be sent.
        // Some sources online say that a for statement is more efficient that a foreach statement, so for large iterations, such as the update loop, the instance destruction loop, and the draw loop. I have used a for
        // statement instead of a foreach statement.
        // The Update loop and the loop to refresh the drawingenemies list are combined into one, for better performance.
        for (int i = 0; i < Rooms[Room].Count; i += 1)// For each index of an Instance in the current room.
        {
            if (IsMultiplayer && collectDrawingEnemies && Rooms[Room].ElementAt(i).Value.IsEnemy)// If the game is multiplayer, and there is not already a long queue of objects
                // to be sent, and if the currently processed instance is an enemy.
            {
                // Add the currently processed instance to the List of drawing enemies.
                DrawingEnemies.Add(new DrawingEnemy(Rooms[Room].ElementAt(i).Value.Sprite, (short)Rooms[Room].ElementAt(i).Value.X, (short)Rooms[Room].ElementAt(i).Value.Y, (float)Rooms[Room].ElementAt(i).Value.RadianRotation));
                
            }
            Rooms[Room].ElementAt(i).Value.Update();// Update the currently processed instance.
        }
        if (collectDrawingEnemies)
            // If there is not a long queue of objects to be sent
        {
            if (DrawingEnemies.Count > 0)// Only send the drawing enemies if there are enemies on the screen to be drawn.
            {
                Multiplayer.SendObject(DrawingEnemies);
                // Send the List of drawing enemies to the other player, which they can view using the ViewEnemyMap button.
            }
            else// otherwise ensure the other user's screen is cleared.
            {
                Multiplayer.SendObject(("Empty"));// Send a message to the other player so their game clears the list of
                // drawing enemies.
            }
    }
        // Destroying an Instance refers to removing all references of that instance from the game, thus allowing the garbage collector to free its resources.
        // Enemies cannot be removed directly from Rooms during the update loop as the List cannot be edited during iteration, so they are stored in the ToBeDestroyed List.
        // The same applies to enemy creation too, however if the ToBeCreated List was not used so enemies were instantly added to Rooms then errors would only rarely be
        // thrown.
        if (ToBeCreated != null)// If any instances need to be added to the game update and draw loops
        {
            foreach (Instance createinstance in ToBeCreated)
            {
                Rooms[createinstance.Room].Add(createinstance.InstanceName, createinstance);// Add the instance to the main game loop
            }
            ToBeCreated.Clear();// Clears the ToBeCreated List, so that enemy references 
        }
        if (ToBeDestroyed != null)// If any enemies have been marked for destruction.
        {
            for (int destroyInstance = 0; destroyInstance < ToBeDestroyed.Count; destroyInstance ++)// Iterate over each of the indexes in the List of instances to be destroyed.
            {
                Rooms[ToBeDestroyed[destroyInstance].Room].Remove(ToBeDestroyed[destroyInstance].InstanceName);// Remove the instance from Rooms, so it will no longer be updated, drawn, or referred to through it.
                ToBeDestroyed[destroyInstance] = null;// Nullifies the Instance.
            }
            ToBeDestroyed.Clear();// Clears the ToBeDestroyed List, removing all instance references in the ToBeDestroyed List.
        }
        // If an instance has a reference through another object, such as how two beam towers are connected through their Partner variable, it will no longer be drawn or updated, however it can still be referenced
        // through that variable and will not be freed by the garbage collector. These variables must be nullified by setting them to null.
        if (Room == 0)// If the current room is the game room.
        {
            GameScreenTime += 1;
            if (CurrentHealth <= 0)// If the user's base is equal to or less than 0.
            {
                MessageBox.Show("Game Over!");// Tell the user that they have lost.
                ReturnToMenu("Lose");// Return to the menu, using a string to identify why they have returned.
            }
            // Lower enemy multipliers will reduce the cost, speed and health of enemies.
            // The multiplier is limited as enemies can become too powerful at very high multipliers, and useless at low multipliers.
            if (I.KeyPressed(Keys.Left))// If the left arrow key was up, but is now down.
            {
                if (EnemyMultiplier > 0.5)// If the enemy multiplier is greater than 0.5.
                {
                    EnemyMultiplier -= 0.1;// Lower the enemy multiplier by 0.1.
                }
            }
            else if (I.KeyPressed(Keys.Right))// If the right arrow key was up, but is now down.
            {
                // Enemies have a limit on their difficulty as they may become too powerful to defeat.
                if (EnemyMultiplier < 10 && EnemyMultiplier < (1f + (TheGame.GameScreenTime / 5000f)))
                    // If the enemy multiplier is smaller than 10(and also very difficult enemies cannot be created early in the game).
                {
                    EnemyMultiplier += 0.1;// Increase the enemy multiplier by 0.1.
                }
            }
            else if (I.KeyPressed(Keys.Down))// If the down arrow key was up, but is now down.
            {
                if (EnemyMultiplier >= 1.5)// If the enemy multiplier is at least 1.5.
                {
                    EnemyMultiplier -= 1;// Decrease the enemy multiplier by 1.
                }
                else// if decreasing the enemy multiplier by 1 would mean the enemy multiplier would go below the minimum multiplier value of 0.5.
                {
                    EnemyMultiplier = 0.5;// Set the enemy multiplier to the minimum multiplier.
                }
            }
            else if (I.KeyPressed(Keys.Up))// If the up arrow key was up, but now is down.
            {
                if (EnemyMultiplier <= 9 && EnemyMultiplier < (1f + (TheGame.GameScreenTime / 5000f)))
                // If the enemy multiplier is smaller than 9(and also very difficult enemies cannot be created early in the game).
                {
                    EnemyMultiplier += 1;// Increase the enemy multiplier by 1.
                }
                else// if increasing the enemy multiplier by 1 would mean that the enemy multiplier would be higher than the maximum multiplier value.
                {
                    if ((1f + (TheGame.GameScreenTime / 1000f)) < 10)// If the user is not yet able to create maximum difficulty enemies.
                    {
                        // Set the enemy multiplier to the maximum multiplier.
                        EnemyMultiplier = (1f + (TheGame.GameScreenTime / 5000f));
                    }
                    else
                    {
                        EnemyMultiplier = 10;// Set the enemy multiplier to the maximum multiplier.
                    }
                }
            }
        }
    }
    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public void Draw(GameTime gameTime)
    {
        TheTowerDefenceGame.GraphicsDevice.Clear(Color.DarkGray);// Clear the graphics device, so the background is dark gray.
        // Generally, the fewer calls to spritebatch.begin, the better. This is because when drawing textures within the same Begin to End statement, the sprite batch will group
        // them together, and only make one call to the graphics device at the end statement. If the Begin to End statement was called for each texture that was drawn, a call to
        // the graphics device would be made for each texture, which would drastically reduce performace. As I do not change any of the SpriteBatch.Begin() parameters, I only need
        // to call it once.
        SpriteBatch.Begin();// Begin the sprite batch, so enemies can be drawn to the screen.
        if (Room == 0)// If the current room is the game room.
        {
            SpriteBatch.Draw(CurrentMap, Vector2.Zero, Color.White);// Draw the current map textures to the screen as the background.
            SpriteBatch.FillRectangle(new Rectangle(0, 0, 512, 32), Color.FromNonPremultiplied(0, 0, 0, 170));// Draw a translucent black box around the area where text will be
            // drawn, so it is still visible if the current map texture would've otherwise rendered the text unreadable.
            
            SpriteBatch.DrawString(Arial10, "Tower Money: £" + TowerMoney, new Vector2(0, 14), Color.White);// Draw the money used to create towers.
            if (IsMultiplayer)// If the game is single player.
            {
                SpriteBatch.DrawString(Arial10, "Unit Money: £" + EnemyMoney, new Vector2(0, 0), Color.White);// Draw the money used to create enemies.
                if (ShowEnemyMap)// If multiplayer and the user is trying to show the enemies that they have sent.
                {
                    foreach (DrawingEnemy t in IncomingDrawingEnemies)// For each of the most recent drawing enemies recieved.
                    {
                        if (t != null)// If the drawing enemy is not null.
                        {
                            t.Draw();// Draw it to the screen.
                        }
                    }

                }
            }
            else
            {
                SpriteBatch.DrawString(Arial10, "Level: " + Level, new Vector2(0, 0), Color.White);// Draw the wave index of the current level.
            }
            
        }
        else if (Room == 4)// If the room is the level selection room.
        {
            DisplayMaps();// Draw the maps that can be selected.
        }
        if (Room == 5)// If the room is the level editor room.
        {
            SpriteBatch.Draw(SpriteList["BlackSquare"], new Vector2(I.NewMouse.X / 32 * 32, I.NewMouse.Y / 32 * 32), Color.White);// Draw a black square at the cursor location, to
            // show where the user will be editing the map.
            SpriteBatch.Draw(PaintingLevel, new Vector2(0, 0), Color.White);// Draw the map that the user is currently editing to the screen.
        }
        for (int i = 0; i < Rooms[Room].Count; i += 1)// For each of the indexes of the enemies in the Dictionary of Instances in the current room.
        {
            Rooms[Room].ElementAt(i).Value.Draw();// Draw the enemy at the current iterated index.
        }
        // TODO: Add your drawing code here
        SpriteBatch.End();// Sends all textures that were drawn to the graphics device.
    }
    /// <summary>
    /// Generates the Path for the CurrentMap that enemies will take to reach the Castle when created.
    /// </summary>
    public static void GeneratePath()
    {
        TheGame.Map = new bool[TheGame.PathMaxX, TheGame.PathMaxY];// Creates the 2D array of booleans that will hold where the walkable nodes are, based on the width
        // and height of the playing area.

        MemoryStream streamFile = new MemoryStream();// Creates a new memory stream.
        CurrentMap.SaveAsPng(streamFile, 1088, 768);// Saves the current map texture as a PNG file.
        Bitmap fullMap = new Bitmap(streamFile);// Sets the bitmap version of the current map to the PNG file created in the memory stream.
        GenerateNavigableNodes(fullMap, ref TheGame.Map);// Sets the 2D array of booleans so that true means a node is walkable, if the current map texture pixel at that location
        // has 0 alpha, and false means a node is not walkable, if the current map texture pixel at that location has an alpha greater than 0.
        fullMap.Dispose();// Disposes of the bitmap version of the CurrentMap, freeing up resources.
        TheGame.Path = FindPath(TheGame.StartPos, TheGame.EndPos, TheGame.Map);// Generates a path from the start and end position of the current map, using the 2D array of booleans
        // to determine which nodes are usable.
    }
    /// <summary>
    /// Uses a specified bitmap to generate a 2D array of booleans determining which nodes are walkable and non-walkable, dependant on the alpha component of the pixels at each of
    /// the node locations, starting from coordinate (16,16) and increasing by 32 in both directions until the end of the bitmap is reached.
    /// </summary>
    /// <param name="mapImage">The bitmap to generate the walkable nodes from.</param>
    /// <param name="nodesToSet">The 2D array of booleans to store whether the nodes are walkable or not in.</param>
    public static void GenerateNavigableNodes(Bitmap mapImage, ref bool[,] nodesToSet)
    {
        for (int xpixel = 16; xpixel < mapImage.Width; xpixel += 32)// Iterates over the index of each pixel across the width of the bitmap.
        {
            for (int ypixel = 16; ypixel < mapImage.Height; ypixel += 32)// Iterates over the index of each pixel across the height of the bitmap.
            {
                if (mapImage.GetPixel(xpixel, ypixel).A == 0)// If the alpha component of the currently processed pixel is 0 (if the current pixel is transparent).
                {
                    nodesToSet[xpixel, ypixel] = true;// Mark the currently iterated node as walkable, so can be traversed by enemies.

                }
                else// If the iterated pixel has an alpha component greater than 0 (if the iterated pixel is not transparent).
                {
                    nodesToSet[xpixel, ypixel] = false;// Mark the current iterated node as not walkable, so cannot be traversed by enemies.
                }
            }
        }
        // Wall towers prevent enemies from travelling via certain nodes that they would have otherwise been able to travel via.
        foreach (Instance i in TheGame.Rooms[0].Values)// For each Instance in the game room.
        {
            if (i.GetType() == typeof(Wall) && i.Destroyed == false)// If the instance is a wall.
            {
                nodesToSet[(int)i.X, (int)i.Y] = false;// Set the currently iterated node as not walkable.
            }
        }
    }
    /// <summary>
    /// Finds a path from the specified start location to the specified end location of a map, using the map as a 2D array of booleans where can be used in
    /// the path and false cannot. Returns the path in the form of an ordered List of Vector2s.
    /// </summary>
    /// <param name="startLocation">The start position to find a route from.</param>
    /// <param name="endLocation">The end position to attempt to reach from the start position via walkable nodes.</param>
    /// <param name="mapToSearch">The 2D array of booleans which represents the map, where true is walkable, and false is not.</param>
    /// <returns>The ordered List of Vector2s which represent the nodes of the path.</returns>
    public static List<Vector2> FindPath(Vector2 startLocation, Vector2 endLocation, bool[,] mapToSearch)
    {
        SearchParameters searchparameters = new SearchParameters(startLocation, endLocation, mapToSearch);// Creates the search parameters used to find the map.
        PathFinder pathfinder = new PathFinder(searchparameters);// Creates a path finder to find a path based on the search parameters.
        return pathfinder.FindPath();// Returns the path generated by the path finder when called to find a path.
    }
    /// <summary>
    /// Regenerates a path after a node has been walkable, and is now not walkable; or was not walkable, and is now walkable.
    /// </summary>
    /// <param name="bypassPoint">The node which has had its state changed.</param>
    /// <returns>Returns true if a new path could be found.</returns>
    public static bool RegeneratePath(Vector2 bypassPoint)// This is used when the user creates or destroys a Wall tower. I could not regenerate an entire new
        // path each time a wall was placed as the game would temporarily freeze as it spent so long calculating a new route. Instead this method is used, which
        // is much more efficient. The game does not normally freeze when this method is used.
    {
        int index = TheGame.Path.IndexOf(bypassPoint);// Gets the index of the Vector2 to either bypass or now include in the path.
        List<Vector2> tempPath = FindPath(TheGame.Path[index - 1], TheGame.Path[index + 1], TheGame.Map);// Generates a path from the node previous to the 
        // bypass node to the node succeeding the bypass node
        if (tempPath.Count == 0)// If there is not a path between these two points (this occurs because the user has blocked the last usable route for enemies).
        {
            return false;// Return false; the path has not been changed.
        }
        // If another route can be found.
        TheGame.Path.RemoveRange(index, 2);// Remove bypass node and succeeding node after it.
        TheGame.Path.InsertRange(index, tempPath);// Insert the newly generated portion of the path.
        foreach (Instance i in TheGame.Rooms[0].Values)//Go through each instance
        {
            if (i.GetType().BaseType == typeof(Enemy) && i.GetType() != typeof(WindEnemy))// Only process instance if it is an enemy who follows the path
            {
                Enemy iEnemy = (Enemy)i;// Convert currently iterated instance to an enemy so enemy variables can be accessed.
                if (iEnemy.NextNodeIndex >= index)// The the next node the enemy would visit is a node that will be affected by the change of route.
                {
                    iEnemy.NextNodeIndex += tempPath.Count;// Increase the next node index so that the next node visited by the enemy stays the same.
                }
            }
        }
        return true;// Return true; the path has been repaired.
    }
    /// <summary>
    /// The enumeration of elements that the Element property of Towers, Enemies and Projectiles can be.
    /// </summary>
    public enum E
    {
        Fire,
        Water,
        Earth,
        Normal,
        Wind,
        Light
    }
    /// <summary>
    /// The enumeration of connection states the game data can have.
    /// </summary>
    public enum ConnectionState
    {
        NoConnection,
        Client,
        Server
    }
    public void SetElementCompare()// Sets the List of damage multipliers based on elements.
    {
        ElementCompare[0, 0] = 1;
        ElementCompare[0, 1] = 0.5;
        ElementCompare[0, 2] = 2;
        ElementCompare[0, 3] = 2;
        ElementCompare[0, 4] = 0.5;
        ElementCompare[0, 5] = 1;

        ElementCompare[1, 0] = 2;
        ElementCompare[1, 1] = 1;
        ElementCompare[1, 2] = 0.5;
        ElementCompare[1, 3] = 1;
        ElementCompare[1, 4] = 2;
        ElementCompare[1, 5] = 0.5;

        ElementCompare[2, 0] = 0.5;
        ElementCompare[2, 1] = 2;
        ElementCompare[2, 2] = 1;
        ElementCompare[2, 3] = 0.5;
        ElementCompare[2, 4] = 1;
        ElementCompare[2, 5] = 2;

        ElementCompare[3, 0] = 0.5;
        ElementCompare[3, 1] = 1;
        ElementCompare[3, 2] = 2;
        ElementCompare[3, 3] = 1;
        ElementCompare[3, 4] = 0.5;
        ElementCompare[3, 5] = 2;

        ElementCompare[4, 0] = 2;
        ElementCompare[4, 1] = 0.5;
        ElementCompare[4, 2] = 1;
        ElementCompare[4, 3] = 2;
        ElementCompare[4, 4] = 1;
        ElementCompare[4, 5] = 0.5;

        ElementCompare[5, 0] = 1;
        ElementCompare[5, 1] = 2;
        ElementCompare[5, 2] = 0.5;
        ElementCompare[5, 3] = 0.5;
        ElementCompare[5, 4] = 2;
        ElementCompare[5, 5] = 1;
    }
    /// <summary>
    /// Writes the specified object to the file at the specified file path, serialised in binary format.
    /// </summary>
    /// <typeparam name="T">The type of the object to save.</typeparam>
    /// <param name="fileName">The file path of the file to save the object in.</param>
    /// <param name="saveInstance">The object to be saved.</param>
    public static void WriteToBinaryFile<T>(string fileName, T saveInstance)
    {
        // The using statement will ensure the stream is disposed of whether the game errors or if the statement ends; freeing up resources.
        using (Stream openingStream = File.Open(fileName, FileMode.Create))// Sets the stream as the contents of the file at the specified file path, so that the
        // file can be created or overwritten.
        {
            BinaryFormatter formatter = new BinaryFormatter();// Creates a new binary formatter to format the object so it can be saved. I have used a binary formatter
            // and not an xml formatter as it is more difficult for the user to cheat and edit game files.
            formatter.Serialize(openingStream, saveInstance);// Serialises the object to the opened file stream.
        }
    }
    /// <summary>
    /// Reads from the specified file path (so long as the file is in binary format), and returns the deserialised object.
    /// </summary>
    /// <typeparam name="T">The type of the object to read.</typeparam>
    /// <param name="fileName">The file path of the file to read the object from.</param>
    /// <returns>The deserialised object that was read from the file.</returns>
    public static T ReadFromBinaryFile<T>(string fileName)
    {
        // The using statement will ensure the stream is disposed of whether the game errors or if the statement ends; freeing up resources.
        using (Stream openingStream = File.Open(fileName, FileMode.Open))// Sets the stream as the contents of the file at the specified file path, so that the
        // file can be read from.
        {
            BinaryFormatter formatter = new BinaryFormatter();// Creates a new binary formatter to read from the binary format of the file to be read.
            return (T)formatter.Deserialize(openingStream);// Deserialises and returns the object read from the opened file stream.
        }
    }
    /// <summary>
    /// Called in the level selection room to display the maps that can be selected and their respective buttons.
    /// </summary>
    public static void DisplayMaps()
    {
        for (int y = 0; y < 2; y++)// Iterate over a height of 2 maps, before the width iteration, so that maps are displayed left to right then top to bottom.
        {
            for (int x = 0; x < 5; x++)// Iterate over a width of 5 maps.
            {
                if (x + y * 5 >= Maps.Count)// If the iteration passes the 10th map, return. Only 10 maps can be displayed at once.
                {
                    return;
                }
                Rectangle temp = new Rectangle(x * 256 + 43, y * 256 + 256, 256, 256);// Creates a rectangle to see if it contains the cursor's location.
                SpriteBatch.Draw(//Draw a texture.
                    temp.Contains(I.NewMouse.X, I.NewMouse.Y)// If the currently iterated map button contains the cursor location.
                        ? SpriteList["LevelSelectorDown"]// Draw the map button as down (selected).
                        : SpriteList["LevelSelector"], temp, Color.White);// Otherwise draw the map button as up.
                //Draw the map textures at the currently iterated coordinates inside its respective map button.
                SpriteBatch.Draw(MapTextures[x + y * 5], new Rectangle(x * 256 + 59, y * 256 + 272, 224, 224), Color.White);
            }
        }
    }
    /// <summary>
    /// Converts a Bitmap to a Texture2D, so it can be drawn by a sprite batch.
    /// </summary>
    /// <param name="imageToConvert">The Bitmap that will be converted to a Texture2D and then returned.</param>
    /// <returns>The Bitmap which was converted to a Texture2D.</returns>
    public Texture2D ConvertBitmapToTexture2D(Bitmap imageToConvert)
    {
        if (imageToConvert != null)
        {
            MemoryStream streamFile = new MemoryStream();// Creates a memory stream to hold the bitmap.
            imageToConvert.Save(streamFile, ImageFormat.Png);// Saves the specified Bitmap to the memory stream in PNG format.
            return Texture2D.FromStream(TheTowerDefenceGame.GraphicsDevice, streamFile);// Returns the Texture2D convert from the memory stream using the game GraphicsDevice.
        }
        return null;// If a null Bitmap is supplied, a null Texture2D is returned.
    }
    /// <summary>
    /// Opens a dialog to allow the user to select an image file path, and returns the file path they have chosen.
    /// </summary>
    /// <returns>Returns the user's chosen file path. Returns null if no file path is selected.</returns>
    public string OpenImageFilePath()// Used to open map images for level creation.
    {
        OpenFileDialog openMap = new OpenFileDialog// Creates a new open file dialog.
        {
            Filter = "Png Files(*.png)|*.png|All files (*.*)|*.*",// The filter of the open file dialog prefers PNG files, but allows for all file types as other
            // image file formats will be allowed, and the user may have PNG file with a different file extension.
            Title = "Open Map Image",// Changes the title displayed in the open file dialog.
            Multiselect = false// The user may not open multiple images. Only one image is allowed for each level file.
        };
        if (openMap.ShowDialog() == DialogResult.OK)// If the user presses OK in the file dialog with a valid file selected.
        {
            return openMap.FileName;// Return the path of the user chosen file.
        }
        return null;// Under all other circumstances, such as if the user has not selected the file or they have pressed cancel, returns null.
    }
    /// <summary>
    /// Loads an image as a Bitmap from a file location specified by the user in an open file dialog, for use as a map. Returns null if not possible.
    /// </summary>
    /// <returns>Returns the loaded image as a Bitmap. Returns null for invalid image dimensions, or an invalid file or file path</returns>
    public Bitmap LoadImage()
    {
        string filePath = OpenImageFilePath();// Retrives a file path of an image that the user selected with an open file dialog.
        if (!string.IsNullOrEmpty(filePath))// If the file path is valid and if the user actually selected a file path.
        {
            try// If an error occurs, suppress the error and goto the catch statement.
            {
                Bitmap loadedImage = new Bitmap(filePath);// Loads the image at the file path as a Bitmap.
                if (loadedImage.Width != 1088 || loadedImage.Height != 768)// If the loaded image is not the correct dimensions for use as a map.
                {
                    MessageBox.Show("Invalid image dimensions. Image must be 1088px wide and 768px high. No image loaded.");// Tell the user what the correct dimensions are.
                    return null;// Returns null for invalid image.
                }
                MessageBox.Show("Image file loaded successfully!");// Tells the user that the image has been successfully loaded.
                return loadedImage;// Returns the successfully loaded image.
            }
            catch// If the file cannot be loaded for any reason, such as if the file is corrupt, or it is not a format which can be loaded, or the file path doesn't exist.
            {
                MessageBox.Show("Invalid image file or path. No image loaded.");// Tells the user that their image or image file path is invalid.
            }
        }
        return null;// Returns null for invalid image.
    }
    /// <summary>
    /// Saes an image as a PNG to a file location specified by the user in a save file dialog, for use as a map.
    /// </summary>
    /// <param name="imageToSave">The Bitmap to save as a PNG file.</param>
    public void SaveImageFile(Bitmap imageToSave)
    {
        SaveFileDialog saveMap = new SaveFileDialog// Creates a new save file dialog for the user to select the path where the file will be saved.
        {
            Filter = "Png File(*.png)|*.png",// Allows the user to only overwrite and save the file as PNG files.
            Title = "Save Map Image"// Changes the title of the save file dialog.
        };
        if (saveMap.ShowDialog() == DialogResult.OK)// If the user enters a valid path and presses the OK button.
        {
            FileStream saveMapFileStream = (FileStream)saveMap.OpenFile();// Creates a new file stream, which can be written to, to write to the file at the selected file
            // path.
            PaintingLevel.SaveAsPng(saveMapFileStream, 1088, 768);// Saves the user drawn map to the stream/to the selected file path.
        }
    }
    /// <summary>
    /// Generates a level so the user can play, if the levels folder has not yet been generated or the user deleted the level files/folder.
    /// </summary>
    public void GenerateDefaultMap()
    {
        MemoryStream streamFile = new MemoryStream();// Creates a new memory stream to hold the PNG format of the default map.
        SpriteList["Map"].SaveAsPng(streamFile, SpriteList["Map"].Width, SpriteList["Map"].Height);// Saves the default map to the memory stream.
        Bitmap fullMap = new Bitmap(streamFile);// Creates a new Bitmap of the saved PNG file in the memory stream.
        // Writes the default map, along with its start and end positions, to a level file.
        WriteToBinaryFile("LevelFiles\\Level0.tdl", new Map(fullMap, new Vector2(16, 80), new Vector2(272, 496)));
        streamFile.Dispose();// Disposes of the memorystream, freeing its resources.
        Maps = new List<Map> {ReadFromBinaryFile<Map>("LevelFiles\\Level0.tdl")};// Loads the level file that was saved into the List of maps, so that it can be used.
    }
    /// <summary>
    /// Returns to the main menu room, resetting any default values present at the start of the game and end multiplayer connections.
    /// </summary>
    /// <param name="multiplayerMessage">A message to send to the other player, if in multiplayer mode.</param>
    public static void ReturnToMenu(string multiplayerMessage = "Nothing")
    {
        TheGame.Room = 1;// Return to the main menu.
        TheGame.LastRoom = 1;// The back button will return the game to the main menu.
        
        TheGame.GameScreenTime = 0;// Sets the number of frames that have passed since starting the game back to 0.
        foreach (Instance i in TheGame.Rooms[0].Values)// Iterates through each instance on the game room.
        {
            if (i.IsTower || i.IsEnemy || i.GetType().BaseType == typeof(Projectile) || i.GetType() == typeof(Castle))// If the Instance is a tower, enemy, or projectile.
            {
                i.DestroyInstance();// Destroy it, resetting the game to have no enemies or towers present if played again.
            }
        }
        TheGame.Towerplacer.Show = false;// Hides buttons so they aren't preserved until the next game.
        TheGame.Deletebutton.Show = false;
        TheGame.Upgradebutton.Show = false;
        if (TheGame.IsMultiplayer)// If the game is multiplayer.
        {
            Multiplayer.SendObject(multiplayerMessage);// Send the multiplayer message to the other user.
            Multiplayer.Disconnect();// Ends all network connections.
        }
    }
    /// <summary>
    /// Generates a wave for use in single player mode. The wave will be more difficult if a higher level is specified.
    /// </summary>
    /// <param name="level">An integer which must be greater than 0 which specifies the level to be generated.</param>
    /// <returns>A Dictionary where the key is the number of frames that must pass from the start of the wave for the value to be created as an Enemy.</returns>
    public static SortedDictionary<int, StaticEnemy> GenerateWave(int level)
    {
        Type enemyType=null;// One of the 6 types of enemies to be generated.
        String enemySprite=null;// The sprite of the enemy to be generated.
        // The switch statement generates a random integer from 0 to 6 inclusive. Each number represents a type of enemy. If the number representing a type of enemy is
        // selected, then the enemyType and enemySprite will be set to one of the six types and sprites of enemies respectively. The case statements show which enemy will
        // be generated based on which number is generated. Cases 4 and 5 are different and explained further. If 6 is generated control is transferred to the default 
        // statement.
        bool MixedWave = false;
        switch (5)
        {
            case 0:
                enemyType = typeof(WaterEnemy);
                enemySprite = "Water";
                break;
            case 1:
                enemyType = typeof(Orb);
                enemySprite = "Orb";
                break;
            case 2:
                enemyType = typeof(NormalEnemy);
                enemySprite = "NormalEnemy";
                break;
            case 3:
                enemyType = typeof(FireballEnemy);
                enemySprite = "FireballEnemy";
                break;
            case 4:// Wind enemies can pass through terrain thus are more difficult, so are only generated after level 5 when the user should've established appropriate
                // defences for them.
                if (level > 5)// If the level is greater than 5.
                {
                    enemyType = typeof(WindEnemy);// Set the type of the enemy to wind enemy.
                    enemySprite = "WindEnemy";// Set the sprite of the enemy to wind enemy.
                    break;// Exit the switch statement.
                }
                goto case 2;// If the level is smaller than or equal to level 5, generate a normal enemy instead.
            case 5:// Case 5 generates a random mixture of enemies 
                //if (level > 5)// A mixture of enemies is difficult, so is only generated after level 5 when the user should've established appropriate defences for it.
                {
                    MixedWave = true;
                    break;// Exit the switch statement.
                }
                goto case 2;// If the level is smaller than or equal to level 5, generate a normal enemy instead.
            default:
                enemyType = typeof(FlowerEnemy);
                enemySprite = "FlowerEnemy";
                break;
        }
        int amount;// Holds the number of enemies that will be generated in the wave.
        int framesBetweenCreation;// Holds the number of frames that pass between the generation of the enemies in the waves.
        // For a greater variation in waves, to make the waves more interesting, the number and frequency of enemies generated is randomly chosen.
        switch (TheGame.Rand.Next(0, 4))
        {
            case 0:// There will be an average number of enemies generated, with an average frequency.
                amount = level * 3;// Up to 300 enemies generated at level 100.
                framesBetweenCreation = 120 - level;// Linear relationship between level and frequency, decreasing from 120 frames between creation to 20, with a decrement
                // of 1 per level.
                break;
            case 1:// There will be a high number of enemies generated, with a low frequency.
                amount = level * 5;// Up to 500 enemies generated at level 100.
                framesBetweenCreation = 240 - level;// Linear relationship between level and frequency, decreasing from 240 frames between creation to 140, with a decrement
                // of 1 per level.
                break;
            case 2:// There will be a low number of enemies generated, with a high frequency.
                amount = level;// Up to 100 enemies generated at level 100.
                framesBetweenCreation = 500 / (level + 1);// Frames between creation decreasing from 500 to 5. The number of frames between creation halves each level, for
                // examples levels 1, 2 and 3 would have 500->250->125 frames between enemy creation respectively. This makes the game exponentially more difficult, so level 100
                // is harder to reach, and earlier levels are easier so the user can establish effective defences.
                break;
            default:// There will be a below average number of enemies generated, with a high frequency.
                amount = level * 2;// Up to 200 enemies generated at level 100.
                framesBetweenCreation = 100 - (int)Math.Round(level * 0.95);// Frames between creation decreasing from 100 to 5, with a decrement of 0.95, though rounded to
                // the nearest whole number.
                break;

        }
        SortedDictionary<int, StaticEnemy> wave = new SortedDictionary<int, StaticEnemy>();// Creates a temporary dictionary to hold the enemies and times when they will be
        // creates.
        for (int i = 60; i < 60 + (framesBetweenCreation * amount); i += framesBetweenCreation)// Starting from 60 frames since the start of the wave, adds the enemies at their
            // to the dictionary, with the number of frames between creation as the increment, and going up to the amount of enemies to be created.
        {
            if (MixedWave)
            {
                int randnum = TheGame.Rand.Next(0, 6);// Generates a random number between 0 and 5 inclusive.
                // Selects one of the potential types of enemies that can be generated by selecting the enemy at the index equal to the randomly generated number from
                // a new List of the types of enemies.
                enemyType = new List<Type> { typeof(WaterEnemy), typeof(Orb), typeof(NormalEnemy), typeof(FireballEnemy), typeof(WindEnemy), typeof(FlowerEnemy) }[randnum];
                // Selects the sprite of the enemy generated based on the randomly generated index.
                enemySprite = new List<String> { "Water", "Orb", "NormalEnemy", "FireballEnemy", "WindEnemy", "FlowerEnemy" }[randnum];
            }
            wave.Add(i, new StaticEnemy(enemySprite, enemyType));
        }
        return wave;// Returns the wave dictionary.
    }
    /// <summary>
    /// Sends out waves of enemies, dependant on the List of Waves generated using the GenerateWave subroutine, and the level.
    /// </summary>
    public void ExecuteWaves()
    {
        if (GameScreenTime > 200)
        {
            if (LastWaveObject == null)// If the wave has just begun, so the last enemy in the wave has not yet been determined.
            {
                CurrentLevelTime = 0;// Set the number of frames passed since the start of the level to 0.
                Level += 1;// Increases the level by one, so that the game moves onto the next wave.
                if (Level > 100)// If the user has beaten level 100.
                {
                    MessageBox.Show("You win! :D");// Tell the user that they have won the game.
                    ReturnToMenu();// Return to the menu, resetting default values so the game can be played again.
                    return;
                }
                LastWaveObject = Waves[Level].Last().Value;// If the user has not beaten level 100, determine the last object in the wave, so the program knows
                // when the wave will end.
            }
            else// If the wave is being continued.
            {
                CurrentLevelTime++;// Increase the number of frames that has passed since the start of the level by 1.
                if (Waves[Level].ContainsKey(CurrentLevelTime))// If there is an enemy to be generated at the current number of frames that have passed since 
                // the start of the level (not during the delay between enemy creation).
                {
                    // Create a new enemy at the current map start location, with the type and sprite of the enemy held within the wave dictionary at the current
                    // number of frames that have passed since the start of the level.
                    Enemy SpawnedEnemy = (Enemy)Activator.CreateInstance(Waves[Level][CurrentLevelTime].EnemyType, Waves[Level][CurrentLevelTime].Sprite, StartPos.X, StartPos.Y, 0, 1 + (Level / 10));
                    SpawnedEnemy.Cost = 100;
                    if (Waves[Level][CurrentLevelTime] == LastWaveObject)// If the last enemy in the wave has been created.
                    {
                        LastWaveObject = null;// Set the last wave object as null, so the game knows that next frame the next wave will commence.
                    }
                }
            }
        }
    }
    /// <summary>
    /// Duplicates the current game data instance, so a copy of the game state can be held without it being updated, and the game can still run.
    /// </summary>
    /// <returns>The duplicated game data instance.</returns>
    public Game1 Duplicate()
    {
        using (MemoryStream currentStream = new MemoryStream())// Creates a new memory stream to hold the game data instance.
        {
            BinaryFormatter formatter = new BinaryFormatter();// Creates a new formatter to store the current game data instance as binary.
            formatter.Serialize(currentStream, this);// Serializes the current game data instances as binary and holds it in the memory stream.
            currentStream.Position = 0;// Sets the starting position to read/write from/to the stream.
            return (Game1)formatter.Deserialize(currentStream);// Deserializes the binary data of the serialized game data instance and returns it, which is a
            // copy of the current game data instance.
        }
    }
}

}