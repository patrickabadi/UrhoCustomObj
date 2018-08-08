using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Urho;

namespace UrhoCustomObj.Components
{
    public class WorldInputHandler: Component
    {
        protected UrhoApp App => Application.Current as UrhoApp;

        public WorldInputHandler()
        {
            ReceiveSceneUpdates = true;
        }

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            
            //this.AddChild<RotationInput>("rotations");
        }

        public void Reset()
        {
            App.RootNode.SetWorldRotation(Quaternion.Identity);
            App.RootNode.Position = Vector3.Zero;
            App.Camera.Zoom = 1f;
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            var input = Application.Input;

            if ((input.GetMouseButtonDown(MouseButton.Left) || input.NumTouches == 1) && App.TouchedNode == null)
            {
                TouchState state = input.GetTouch(0);
                if (state.Pressure != 1.0)
                    return;

                App.RootNode.Rotate(new Quaternion(state.Delta.Y, -state.Delta.X, 0), TransformSpace.World);

            }
            else if (input.NumTouches == 2)
            {
                TouchState state1 = input.GetTouch(0);
                TouchState state2 = input.GetTouch(1);

                var distance1 = Distance(state1.Position, state2.Position);                

                if(distance1 < 120f)
                {
                    // doing a pan
                    float factor = 0.005f;
                    App.RootNode.Position += new Vector3(-state1.Delta.X*factor, -state1.Delta.Y*factor, 0);
                }
                else
                {
                    var distance2 = Distance(state1.LastPosition, state2.LastPosition);

                    var pos1 = new Vector3(state1.Position.X, state1.Position.Y, 0);
                    var pos2 = new Vector3(state2.Position.X, state2.Position.Y, 0);

                    var v = (pos1 + pos2) / 2;

                    // doing a zoom
                    Zoom((int)(distance1 - distance2), (int)v.X, (int)v.Y);
                    //App.Camera.Zoom += (distance1 - distance2) * 0.01f;
                }

                
            }

            if (input.GetKeyDown(Key.W) || input.GetKeyDown(Key.Up)) Pan(PanDirection.Up);
            if (input.GetKeyDown(Key.S) || input.GetKeyDown(Key.Down)) Pan(PanDirection.Down);
            if (input.GetKeyDown(Key.A) || input.GetKeyDown(Key.Left)) Pan(PanDirection.Left);
            if (input.GetKeyDown(Key.D) || input.GetKeyDown(Key.Right)) Pan(PanDirection.Right);
            if (input.GetKeyDown(Key.KP_Plus)) Zoom(ZoomDirection.In);
            if (input.GetKeyDown(Key.KP_Minus)) Zoom(ZoomDirection.Out);
        }

        float Distance(IntVector2 v1, IntVector2 v2)
        {
            return (float)Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y));
        }

        public void RightMouseClickMoved(double xDelta, double yDelta)
        {
            Debug.WriteLine($"RightMouseClickMoved {xDelta}, {yDelta}");

            float factor = 0.005f;
            App.RootNode.Position += new Vector3((float)xDelta * factor, (float)yDelta * factor, 0);
        }

        public void MouseWheelPressed(int delta, double x, double y)
        {
            Zoom(delta, (int)x, (int)y);
        }

        #region Zoom
        public enum ZoomDirection
        {
            In = 1,
            Out = 2
        }

        private const float _zoomInFactor = 1.1f;
        private const float _zoomOutFactor = (float)(1.0 / _zoomInFactor);
        private const float _zoomInFactorSmall = 1.02f;
        private const float _zoomOutFactorSmall = (float)(1.0 / _zoomInFactorSmall);
        public void Zoom(ZoomDirection dir, bool animate = false)
        {            

            if (animate)
            {
                var factor = (dir == ZoomDirection.In) ? _zoomInFactor : _zoomOutFactor;

                ValueAnimation zoomAnimation = new ValueAnimation();
                zoomAnimation.InterpolationMethod = InterpMethod.Linear;
                zoomAnimation.SetKeyFrame(0.0f, App.Camera.Zoom);
                zoomAnimation.SetKeyFrame(0.3f, App.Camera.Zoom * factor);

                ObjectAnimation cameraAnimation = new ObjectAnimation();
                cameraAnimation.AddAttributeAnimation("Zoom", zoomAnimation, WrapMode.Once, 1f);

                App.Camera.ObjectAnimation = cameraAnimation;
            }
            else
            {
                var factor = (dir == ZoomDirection.In) ? _zoomInFactorSmall : _zoomOutFactorSmall;

                App.Camera.Zoom *= factor;
            }
        }

        protected void Zoom(int delta, int x, int y)
        {
            var viewPort = App.Renderer.GetViewport(0);

            // 3d mouse location before the zoom
            var mouseV = viewPort.ScreenToWorldPoint(x, y, App.CameraPosition.Z);

            App.Camera.Zoom += delta * 0.001f;

            // zoom out no panning
            if (delta < 0)
                return;

            // 3d mouse location after the zoom
            var mouseV2 = viewPort.ScreenToWorldPoint(x, y, App.CameraPosition.Z);

            var diff = mouseV2 - mouseV;

            App.RootNode.Position += diff;
        }

        #endregion Zoom

        #region Pan
        public enum PanDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        private Vector3 GetVectorForDirection(PanDirection dir, bool animate)
        {
            Vector3 direction = new Vector3(0, 0, 0);

            float factor = animate ? 0.1f : 0.05f;
            switch (dir)
            {
                case PanDirection.Up:
                    direction = -factor * App.Scene.Up;
                    break;
                case PanDirection.Down:
                    direction = factor * App.Scene.Up;
                    break;
                case PanDirection.Left:
                    direction = -factor * App.Scene.Right;
                    break;
                case PanDirection.Right:
                    direction = factor * App.Scene.Right;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
            return direction;
        }

        public void Pan(PanDirection dir, bool animate = false)
        {
            var direction = GetVectorForDirection(dir, animate);

            if(animate)
            {
                ValueAnimation panAnimation = new ValueAnimation();
                panAnimation.InterpolationMethod = InterpMethod.Linear;
                panAnimation.SetKeyFrame(0.0f, App.RootNode.Position);
                panAnimation.SetKeyFrame(0.3f, App.RootNode.Position + direction);

                ObjectAnimation mainNodeAnimation = new ObjectAnimation();
                mainNodeAnimation.AddAttributeAnimation("Position", panAnimation, WrapMode.Once, 1f);

                App.RootNode.ObjectAnimation = mainNodeAnimation;
            }
            else
            {
                App.RootNode.Position += direction;
            }
            
        }
        #endregion Pan
    }
}
