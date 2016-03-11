using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNATerrain {
    public class FreeCamera : Camera {
        public float Yaw { get; set; }
        public float Pitch { get; set; }

        //public Vector3 Position { get; set; }
        public Vector3 Target { get; private set; }

        public Vector3 Up { get; private set; }
        public Vector3 Right { get; private set; }

        private Vector3 translation;

        public Vector3 Position;

        private Terrain m_terrain;


        public FreeCamera(Vector3 position, float yaw, float pitch,
            GraphicsDevice graphicsDevice, ref Terrain terrain)
            : base(graphicsDevice) {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
            m_terrain = terrain;

            translation = Vector3.Zero;
        }

        public void Rotate(float YawChange, float PitchChange) {
            Yaw += YawChange;
            Pitch += PitchChange;
        }

        public void Move(Vector3 Translation) {
            translation += Translation;
            //Position.Y = m_terrain.GetHeightAtPosition(Position.X / 512, Position.Z / 512, out steepness);
            
        }

        public override void Update() {
            // Calculate the rotation matrix
            Matrix rotation = Matrix.CreateFromYawPitchRoll(Yaw, Pitch, 0);

            // Offset the position and reset the translation
            translation = Vector3.Transform(translation, rotation);
            Position += translation;
            translation = Vector3.Zero;


            // Calculate the new target
            Vector3 forward = Vector3.Transform(Vector3.Forward, rotation);
            Target = Position + forward;

            // Calculate the up vector
            Vector3 up = Vector3.Transform(Vector3.Up, rotation);

            // Calculate the view matrix
            View = Matrix.CreateLookAt(Position, Target, up);

            Up = up;
            Right = Vector3.Cross(forward, up);
        }
    }
}
