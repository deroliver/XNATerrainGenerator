using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNATerrain {
    /// <summary>
    /// Abstract class - Implemented
    /// by other camera types
    /// </summary>
    public abstract class Camera {
        Matrix view; //< The view matrix
        Matrix projection; //< The projection matrix

        public Matrix Projection {
            get { return projection; }
            protected set {
                projection = value;
                GenerateFrustum();
            }
        }

        public Matrix View {
            get { return view; }
            protected set {
                view = value;
                GenerateFrustum();
            }
        }

        // The frustum for the camera
        public BoundingFrustum Frustum { get; private set; }

        protected GraphicsDevice GraphicsDevice { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to draw to</param>
        public Camera(GraphicsDevice graphicsDevice) {
            this.GraphicsDevice = graphicsDevice;

            GeneratePerspectiveProjectionMatrix(MathHelper.PiOver4);
        }

        /// <summary>
        /// Generates a persepective matrix
        /// </summary>
        /// <param name="FieldOfView"></param>
        private void GeneratePerspectiveProjectionMatrix(float FieldOfView) {
            PresentationParameters pp = GraphicsDevice.PresentationParameters;

            float aspectRatio = (float)pp.BackBufferWidth /
                (float)pp.BackBufferHeight;

            this.Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45), aspectRatio, 50.0f, 1000000.0f);
        }

        /// <summary>
        /// Update function - Must be implemented
        /// By clases the inherit
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Generates a frustum
        /// </summary>
        private void GenerateFrustum() {
            Matrix viewProjection = View * Projection;
            Frustum = new BoundingFrustum(viewProjection);
        }

        /// <summary>
        /// Checks if a sphere is in view
        /// </summary>
        /// <param name="sphere">The sphere to check</param>
        /// <returns></returns>
        public bool BoundingVolumeIsInView(BoundingSphere sphere) {
            return (Frustum.Contains(sphere) != ContainmentType.Disjoint);
        }

        /// <summary>
        /// Checks if a box is in view
        /// </summary>
        /// <param name="box">The box to check</param>
        /// <returns></returns>
        public bool BoundingVolumeIsInView(BoundingBox box) {
            return (Frustum.Contains(box) != ContainmentType.Disjoint);
        }
    }
}
