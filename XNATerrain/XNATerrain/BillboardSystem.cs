using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace XNATerrain {
    public class BillboardSystem : IRenderable {
        // Vertex buffer and index buffer, particle
        // and index arrays
        VertexBuffer verts;
        IndexBuffer ints;
        VertexPositionTexture[] particles;
        int[] indices;

        Random r = new Random();

        Vector3 m_velocity;

        // Billboard settings
        int m_nBillboards;
        Vector2 m_billboardSize;
        Texture2D m_texture;

        // GraphicsDevice and Effect
        GraphicsDevice m_graphicsDevice;
        Effect effect;

        public bool EnsureOcclusion = true;

        public enum BillboardMode { Cylindrical, Spherical };
        public BillboardMode Mode = BillboardMode.Spherical;

        public BillboardSystem(GraphicsDevice graphicsDevice,
            ContentManager content, Texture2D texture,
            Vector2 billboardSize, Vector3[] particlePositions) {

            m_nBillboards = particlePositions.Length;
            m_billboardSize = billboardSize;
            m_graphicsDevice = graphicsDevice;
            m_texture = texture;

            effect = content.Load<Effect>("BillboardEffect");

            generateParticles(particlePositions);
        }

        void generateParticles(Vector3[] particlePositions) {
            // Create vertex and index arrays
            particles = new VertexPositionTexture[m_nBillboards * 4];
            indices = new int[m_nBillboards * 6];

            int x = 0;

            // For every billboard 
            for (int i = 0; i < m_nBillboards * 4; i += 4) {
                Vector3 pos = particlePositions[i / 4];

                // Add 4 vertices at the billboard's position
                particles[i + 0] = new VertexPositionTexture(pos, new Vector2(0, 0));
                particles[i + 1] = new VertexPositionTexture(pos, new Vector2(0, 1));
                particles[i + 2] = new VertexPositionTexture(pos, new Vector2(1, 1));
                particles[i + 3] = new VertexPositionTexture(pos, new Vector2(1, 0));

                // Add 6 indices to form two triangles
                indices[x++] = i + 0;
                indices[x++] = i + 3;
                indices[x++] = i + 2;
                indices[x++] = i + 2;
                indices[x++] = i + 1;
                indices[x++] = i + 0;
            }

            // Create and set the vertex buffer
            verts = new VertexBuffer(m_graphicsDevice, typeof(VertexPositionTexture), particles.Length, BufferUsage.WriteOnly | BufferUsage.None);
            verts.SetData<VertexPositionTexture>(particles);

            // Create and set the index buffer
            ints = new IndexBuffer(m_graphicsDevice, IndexElementSize.ThirtyTwoBits,
                 m_nBillboards * 6, BufferUsage.WriteOnly);
            ints.SetData<int>(indices);
        }

        /// <summary>
        /// Sets the effect parameters
        /// </summary>
        /// <param name="View">The view matrix</param>
        /// <param name="Projection">The projection matrix</param>
        /// <param name="Up">The up vector</param>
        /// <param name="Right">The right</param>
        void setEffectParameters(Matrix View, Matrix Projection, Vector3 Up, Vector3 Right) {
            effect.Parameters["ParticleTexture"].SetValue(m_texture);
            effect.Parameters["View"].SetValue(View);
            effect.Parameters["Projection"].SetValue(Projection);
            effect.Parameters["Size"].SetValue(m_billboardSize / 2f);
            effect.Parameters["Up"].SetValue(Mode == BillboardMode.Spherical ? Up : Vector3.Up);
            effect.Parameters["Side"].SetValue(Right);
        }

        /// <summary>
        /// Updates the positions of the billboards
        /// </summary>
        /// <param name="gameTime">The game time</param>
        public void Update(float gameTime) {
            for (int i = 0; i < indices.Length; i+=6) {

                m_velocity.X = (float)r.NextDouble() * 2.0f;
                m_velocity.Z = (float)r.NextDouble() * 2.0f;
                m_velocity.Y = -(float)r.NextDouble() * 25.0f;

                particles[indices[i]].Position += m_velocity;
                particles[indices[i + 1]].Position += m_velocity * gameTime;
                particles[indices[i + 2]].Position += m_velocity * gameTime;
                particles[indices[i + 3]].Position += m_velocity * gameTime;
                particles[indices[i + 4]].Position += m_velocity * gameTime;
                particles[indices[i + 5]].Position += m_velocity * gameTime;

                if(particles[indices[i]].Position.Y < 5000) {
                    particles[indices[i]].Position.Y = 100000;
                    particles[indices[i + 1]].Position.Y = 100000;
                    particles[indices[i + 2]].Position.Y = 100000;
                    particles[indices[i + 3]].Position.Y = 100000;
                    particles[indices[i + 4]].Position.Y = 100000;
                    particles[indices[i + 5]].Position.Y = 100000;
                }
            }
        }

        /// <summary>
        /// Draws the billboards
        /// </summary>
        /// <param name="View">The view matrix</param>
        /// <param name="Projection">The projection matrix</param>
        /// <param name="Up">The up vector</param>
        /// <param name="Right">The right</param>
        public void Draw(Matrix View, Matrix Projection, Camera camera) {
            // Set the vertex and index buffer to the graphics card
            m_graphicsDevice.SetVertexBuffer(verts);
            m_graphicsDevice.Indices = ints;

            m_graphicsDevice.BlendState = BlendState.AlphaBlend;

            setEffectParameters(View, Projection, ((FreeCamera)camera).Up, ((FreeCamera)camera).Right);

            if (EnsureOcclusion) {
                drawOpaquePixels();
                drawTransparentPixels();
            } else {
                m_graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                effect.Parameters["AlphaTest"].SetValue(false);
                drawBillboards();
            }

            // Reset render states
            m_graphicsDevice.BlendState = BlendState.Opaque;
            m_graphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Un-set the vertex and index buffer
            m_graphicsDevice.SetVertexBuffer(null);
            m_graphicsDevice.Indices = null;
        }


        /// <summary>
        /// Draws opaque parts
        /// </summary>
        void drawOpaquePixels() {
            m_graphicsDevice.DepthStencilState = DepthStencilState.Default;

            effect.Parameters["AlphaTest"].SetValue(true);
            effect.Parameters["AlphaTestGreater"].SetValue(true);

            drawBillboards();
        }

        /// <summary>
        /// Draws transparent parts
        /// </summary>
        void drawTransparentPixels() {
            m_graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            effect.Parameters["AlphaTest"].SetValue(true);
            effect.Parameters["AlphaTestGreater"].SetValue(false);

            drawBillboards();
        }

        /// <summary>
        /// Draws the billboards
        /// </summary>
        void drawBillboards() {
            // Set the effect technique
            effect.CurrentTechnique.Passes[0].Apply();

            // Pass the arrays to the GPU
            m_graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4 * m_nBillboards, 0, m_nBillboards * 2);
        }

        public void SetClipPlane(Vector4? Plane) {
        }     
    }
}
