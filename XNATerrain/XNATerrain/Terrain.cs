using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System;

namespace XNATerrain {
    public interface IRenderable {
        void Draw(Matrix View, Matrix Projection, Camera camera);
    }

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Terrain : IRenderable {

        #region Member Variables
        // Vertex arrays
        private VertexPositionNormalTexture[] m_vertices; //< Verex array
        private int[] m_indices; //< Index array

        // Vertex buffers
        private VertexBuffer m_vertexBuffer; //< Vertex buffer      
        IndexBuffer m_indexBuffer; //< Index buffer

        private float[,] m_heightData; //< Array of height map data
        private float m_maxHeight; //< The highest point on the height map
        private float m_cellsize; //< Distance between vertices 
        private float m_textureTiling; //< Used in the shader
        private float m_detailDistance = 2500;
        private float m_detailTextureTiling = 100;

        private int m_width, m_height; //< Height and width of the height map
        private int m_numVertices, m_numIndices; //< The numveb of vertices and indices

        private Effect m_effect; //< The effect that is used for rendering

        private GraphicsDevice m_graphicsDevice; //< The device used for drawing

        private GraphicsDeviceManager m_graphics; //< The graphics manager

        private Texture2D m_heigtMap; //< The heightmap texture
        private Texture2D m_normalMap;
        private Texture2D m_texture; //< The texture used
        public Texture2D m_detailTexture;

        private Matrix m_World;

        private Vector3 m_lightDirection; //< The direction of the light

        private Random r = new Random();
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="HeightMap">The heightmap texture</param>
        /// <param name="CellSize">The size of each cell</param>
        /// <param name="Height">Maximum heightmap height</param>
        /// <param name="BaseTexture">The base texture for the terrain</param>
        /// <param name="TextureTiling">How many times the texture is repeated</param>
        /// <param name="LightDirection">The direction of the light</param>
        /// <param name="GraphicsDevice">The graphics device to draw to</param>
        /// <param name="Content">The content manager object</param>
        public Terrain(Texture2D HeightMap, float CellSize, float Height,
                       Texture2D Texture, float TextureTiling, Vector3 LightDirection,
                       GraphicsDevice GraphicsDevice, ContentManager Content) {

            m_heigtMap = HeightMap;
            m_width = HeightMap.Width;
            m_height = HeightMap.Height;
            m_cellsize = CellSize;
            m_maxHeight = Height;
            m_graphicsDevice = GraphicsDevice;
            m_texture = Texture;
            m_textureTiling = TextureTiling;
            m_lightDirection = LightDirection;

            m_detailTexture = Content.Load<Texture2D>("noise_texture"); 
            m_normalMap = Content.Load<Texture2D>("NormalMap");

            m_effect = Content.Load<Effect>("TerrainEffect");

            // 1 vertex per pixel
            m_numVertices = m_width * m_height;

            // (Width-1) * (Length-1) cells, 2 triangles per cell, 3 indices per triangle
            m_numIndices = (m_width - 1) * (m_height - 1) * 6;

            m_vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture),
                m_numVertices, BufferUsage.WriteOnly);

            m_indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits,
                m_numIndices, BufferUsage.WriteOnly);

            GetHeights();
            CreateVertices();
            CreateIndices();
            GetNormals();

            m_vertexBuffer.SetData<VertexPositionNormalTexture>(m_vertices);
            m_indexBuffer.SetData<int>(m_indices);

            m_World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up);
        }


        /// <summary>
        /// Calculates the height at each pixel on the 
        /// height map
        /// </summary>
        private void GetHeights() {
            Color[] heightMapData = new Color[m_height * m_width];
            m_heigtMap.GetData<Color>(heightMapData);

            m_heightData = new float[m_width, m_height];

            // For each pixel
            for (int y = 0; y < m_height; y++) {
                for (int x = 0; x < m_width; x++) {

                    // Get the color value (0 - 255)
                    float amt = heightMapData[y * m_width + x].R;

                    // Scale 0 - 1
                    amt /= 255.0f;

                    // Multiply by the max height
                    m_heightData[x, y] = amt * m_maxHeight;
                }
            }
        }


        /// <summary>
        /// Initializes the positions and uv
        /// vectors for the vertices
        /// </summary>
        private void CreateVertices() {
            m_vertices = new VertexPositionNormalTexture[m_numVertices];

            Vector3 offSetToCenter = -new Vector3(((float)m_width / 2.0f) * m_cellsize, 0, ((float)m_height / 2.0f) * m_cellsize);

            // Sets the position as well as the uv vectors
            for (int z = 0; z < m_height; z++) {
                for (int x = 0; x < m_width; x++) {
                    Vector3 position = (new Vector3(x * m_cellsize, m_heightData[x, z], z * m_cellsize) + offSetToCenter) * new Vector3(50);
                    Vector2 uv = new Vector2((float)x / m_width, (float)z / m_height);
                    m_vertices[z * m_width + x] = new VertexPositionNormalTexture(position, Vector3.Zero, uv);
                }
            }
        }


        /// <summary>
        /// Initializes the indicies for
        /// the vertices array
        /// </summary>
        private void CreateIndices() {
            m_indices = new int[m_numIndices];

            int i = 0;

            // For each cell
            for (int x = 0; x < m_width - 1; x++)
                for (int z = 0; z < m_height - 1; z++) {

                    // Get the corner indices (2 triangles make a square)
                    int upperLeft = z * m_width + x;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + m_width;
                    int lowerRight = lowerLeft + 1;

                    // Indices for the upper triangle
                    m_indices[i++] = upperLeft;
                    m_indices[i++] = upperRight;
                    m_indices[i++] = lowerLeft;

                    // Indices for the upper triangle
                    m_indices[i++] = lowerLeft;
                    m_indices[i++] = upperRight;
                    m_indices[i++] = lowerRight;
                }
        }

        /// <summary>
        /// Initializes the normals for the 
        /// vertex array
        /// </summary>
        private void GetNormals() {
            // For each triangle
            for (int i = 0; i < m_numIndices; i += 3) {
                // Get the position at each corner using the index array
                Vector3 v1 = m_vertices[m_indices[i]].Position;
                Vector3 v2 = m_vertices[m_indices[i + 1]].Position;
                Vector3 v3 = m_vertices[m_indices[i + 2]].Position;

                // Cross the vectors between the corners to get the normal
                // Gets the normal vector for the entire triangle
                Vector3 normal = Vector3.Cross(v1 - v2, v1 - v3);
                normal.Normalize();

                // Add the influence of the normal to each vertex in the
                // triangle - Set the normal for each vertex in the triangle
                m_vertices[m_indices[i]].Normal += normal;
                m_vertices[m_indices[i + 1]].Normal += normal;
                m_vertices[m_indices[i + 2]].Normal += normal;
            }

            // Average the influences of the triangles touching each
            // vertex - Normalize the normal vector
            for (int i = 0; i < m_numVertices; i++)
                m_vertices[i].Normal.Normalize();
        }

        /// <summary>
        /// Returns the height at any given (x, z) position
        /// </summary>
        /// <param name="X">The X coordinate</param>
        /// <param name="Z">The Z coordinate</param>
        /// <param name="Steepness">The steepness of the terrain</param>
        /// <returns></returns>
        public float GetHeightAtPosition(float X, float Z, out float Steepness) {
            // Make sure the coordinates are actually on the terrain
            X = MathHelper.Clamp(X, (-m_width / 2) * m_cellsize, (m_width / 2) * m_cellsize);
            Z = MathHelper.Clamp(Z, (-m_height / 2) * m_cellsize, (m_height / 2) * m_cellsize);

            // Map from (-m_width -> m_width, -m_height, m_height) to
            // (0 -> m_width, 0 -> m_height)
            // Remove the negative range
            X += (m_width / 2.0f) * m_cellsize;
            Z += (m_height / 2.0f) * m_cellsize;

            // Map the coordinates to the cell
            // Coordinates
            X /= m_cellsize;
            Z /= m_cellsize;

            // Cast to an int
            int x1 = (int)X;
            int z1 = (int)Z;

            // Attempt to get bottom right cell vertex coordinates
            int x2 = x1 + 1 == m_width ? x1 : x1 + 1;
            int z2 = z1 + 1 == m_height ? z1 : z1 + 1;

            // Get the heights from the height data
            float h1 = m_heightData[x1, z1];
            float h2 = m_heightData[x1, z2];

            // Determine the steepeness angle between the higher and lower vertex
            Steepness = (float)Math.Atan(Math.Abs((h1 - h2) / m_cellsize * Math.Sqrt(2)));

            // Find the average of the amount lost from casting to integers
            float leftover = ((X - x1) + (Z - z1)) / 2.0f;

            // Interpolate between the corner vertices' heights
            return MathHelper.Lerp(h1, h2, leftover);
        }


        /// <summary>
        /// Populates the terrain with clouds
        /// </summary>
        /// <param name="clousPos">The cloud positions</param>
        /// <param name="clouds">The clouds billboard system</param>
        /// <param name="Content">The content manager</param>
        public void PopulateWithClouds(ref List<Vector3> clousPos, ref BillboardSystem clouds, ContentManager Content) { 
            for (int i = 0; i < 40; i++) {
                Vector3 cloudLoc = new Vector3(
                    r.Next(-8000, 8000),
                    r.Next(5000, 7000),
                    r.Next(-8000, 8000)) * new Vector3(40);

                for (int j = 0; j < 1; j++) {
                    clousPos.Add(cloudLoc +
                        new Vector3(
                            r.Next(-3000, 3000),
                            r.Next(-300, 900),
                            r.Next(-1500, 1500)));
                }
            }

            clouds = new BillboardSystem(m_graphicsDevice, Content,
                Content.Load<Texture2D>("cloud2"), new Vector2(200000),
                clousPos.ToArray());

            clouds.Mode = BillboardSystem.BillboardMode.Spherical;
            clouds.EnsureOcclusion = false;
        }

        
        /// <summary>
        /// Draws the terrain
        /// </summary>
        /// <param name="View">The view matrix</param>
        /// <param name="Projection">The projection matrix</param>
        /// <param name="CameraPosition">The current camera position</param>
        public void Draw(Matrix View, Matrix Projection, Camera camera) {
            // Set the vertex and index buffer data
            m_graphicsDevice.SetVertexBuffer(m_vertexBuffer);
            m_graphicsDevice.Indices = m_indexBuffer;

            // Initialize all the shader parameters
            m_effect.Parameters["View"].SetValue(View);
            m_effect.Parameters["Projection"].SetValue(Projection);
            m_effect.Parameters["BaseTexture"].SetValue(m_texture);
            m_effect.Parameters["TextureTiling"].SetValue(m_textureTiling);
            m_effect.Parameters["LightDirection"].SetValue(m_lightDirection);

            m_effect.Parameters["DetailTexture"].SetValue(m_detailTexture);
            m_effect.Parameters["DetailDistance"].SetValue(m_detailDistance);
            m_effect.Parameters["DetailTextureTiling"].SetValue(m_detailTextureTiling);
            m_effect.Parameters["World"].SetValue(m_World);

            m_effect.Techniques[0].Passes[0].Apply();

            // Passes the vertex data to the GPU
            m_graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, m_numVertices, 0, m_numIndices / 3);
        }
    }
}
