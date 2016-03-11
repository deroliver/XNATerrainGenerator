using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System;

namespace XNATerrain {
    public class MainGame : Microsoft.Xna.Framework.Game {

        #region Member Variables
        private GraphicsDeviceManager m_graphics;
        private SpriteBatch m_spriteBatch;
        private FreeCamera m_camera; //< The camera

        private MouseState m_lastMouseState; //< The last mouse staet

        private Terrain m_terrain; //< The terrain object

        private Random m_r = new Random();
        private BillboardSystem m_clouds; //< The clouds billboard object


        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainGame() {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            m_graphics.PreferredBackBufferWidth = 1280;
            m_graphics.PreferredBackBufferHeight = 800;
        }


        /// <summary>
        /// Loads all the game content
        /// </summary>
        protected override void LoadContent() {
            m_spriteBatch = new SpriteBatch(GraphicsDevice);

            m_graphics.IsFullScreen = true;

            // Initialize the terrain
            m_terrain = new Terrain(Content.Load<Texture2D>("heightmap"), 30, 3000,
                Content.Load<Texture2D>("grass2"), 12, new Vector3(1, -1, 0),
                GraphicsDevice, Content);

            // Initialize the camera using a free camera
            m_camera = new FreeCamera(new Vector3(8000, 1000000, 8000),
                MathHelper.ToRadians(45),
                MathHelper.ToRadians(-90),
                GraphicsDevice,
                ref m_terrain);

            // Lists of all the billboard objects positions
            List<Vector3> cloudPositions = new List<Vector3>();
            List<Vector3> orbPositions = new List<Vector3>();           


            Texture2D grassMap = Content.Load<Texture2D>("grass_map");
            Color[] grassPixels = new Color[grassMap.Width * grassMap.Height];
            grassMap.GetData<Color>(grassPixels);

            m_terrain.PopulateWithClouds(ref cloudPositions, ref m_clouds, Content);
         

            // Update the mouse state
            m_lastMouseState = Mouse.GetState();
        }


        /// <summary>
        /// Update the game
        /// </summary>
        /// <param name="gameTime">The game time</param>
        protected override void Update(GameTime gameTime) {
            updateCamera(gameTime);            
            base.Update(gameTime);
        }


        /// <summary>
        /// Updates the camera and handles
        /// movement
        /// </summary>
        /// <param name="gameTime">The game time</param>
        void updateCamera(GameTime gameTime) {
            // Get the new keyboard and mouse state
            MouseState mouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();

            // Determine how much the camera should turn
            float deltaX = (float)m_lastMouseState.X - (float)mouseState.X;
            float deltaY = (float)m_lastMouseState.Y - (float)mouseState.Y;

            // Rotate the camera
            ((FreeCamera)m_camera).Rotate(deltaX * .005f, deltaY * .005f);

            Vector3 translation = Vector3.Zero;

            // Determine in which direction to move the camera
            if (keyState.IsKeyDown(Keys.W)) translation += Vector3.Forward;
            if (keyState.IsKeyDown(Keys.S)) translation += Vector3.Backward;
            if (keyState.IsKeyDown(Keys.A)) translation += Vector3.Left;
            if (keyState.IsKeyDown(Keys.D)) translation += Vector3.Right;

            // Move 4 units per millisecond, independent of frame rate
            translation *= 150 * (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Move the camera
            ((FreeCamera)m_camera).Move(translation);

            // Update the camera
            m_camera.Update();

            // Update the mouse state
            m_lastMouseState = mouseState;
        }


        /// <summary>
        /// Draws the game
        /// </summary>
        /// <param name="gameTime">The game time</param>
        protected override void Draw(GameTime gameTime) {
           

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            RasterizerState rs = new RasterizerState();
            rs.CullMode = CullMode.CullCounterClockwiseFace;

            GraphicsDevice.RasterizerState = rs;

            GraphicsDevice.Clear(Color.Black);

            m_terrain.Draw(m_camera.View, m_camera.Projection, m_camera);

            m_clouds.Draw(m_camera.View, m_camera.Projection, m_camera);
            //m_orbs.Draw(m_camera.View, m_camera.Projection, m_camera);

            base.Draw(gameTime);
        }
    }
}
