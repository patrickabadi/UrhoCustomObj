using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Urho;
using Urho.Actions;
using Urho.Shapes;
using UrhoCustomObj.Components;


namespace UrhoCustomObj
{
    public class UrhoApp : Application
    {
        public Scene Scene => _scene;
        public Octree Octree => _octree;
        public Node RootNode => _rootNode;
        public Camera Camera => _camera;
        public Light Light => _light;
        public bool IsInitialized => _scene != null;
        public Node TouchedNode;

        private Scene _scene;
        private Octree _octree;
        private Node _rootNode;
        private Camera _camera;
        private Light _light;

        public Vector3 CameraPosition => new Vector3(0, 0, 6);        

        public UrhoApp(ApplicationOptions options = null) : base(options)
        {
            _scene = null;
        }

        public void Reset()
        {
            if (!IsInitialized)
                return;

            TouchedNode = null;
            _rootNode.RemoveAllChildren();
            _rootNode.SetWorldRotation(Quaternion.Identity);
            _camera.Zoom = 1f;
        }

        protected override void Start()
        {
            base.Start();

            InitScene();

            AddCameraAndLight();            

            SetupViewport();

            if (Xamarin.Forms.Device.RuntimePlatform == Xamarin.Forms.Device.Android)
                AddStuff();

            Input.TouchBegin += Input_TouchBegin;
            Input.TouchEnd += Input_TouchEnd;
        }

        #region Initialisation

        private void UrhoViewApp_UnhandledException(object sender, Urho.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
        }

        private void Engine_PostRenderUpdate(PostRenderUpdateEventArgs obj)
        {
            // If draw debug mode is enabled, draw viewport debug geometry, which will show eg. drawable bounding boxes and skeleton
            // bones. Note that debug geometry has to be separately requested each frame. Disable depth test so that we can see the
            // bones properly
            Renderer.DrawDebugGeometry(false);
        }

        private void InitScene()
        {
            _scene = new Scene();
            _octree = _scene.CreateComponent<Octree>();
            //_scene.CreateComponent<DebugRenderer>();
            _rootNode = _scene.CreateChild("rootNode");            
        }

        

        private void AddStuff()
        {
            this.AddChild<WorldInputHandler>("inputs");

            var model = this.AddChild<ObjectModel>("model");

            model.LoadMesh("UrhoCustomObj.Meshes.Model.obj", true);
        }

        private void AddCameraAndLight()
        {
            var cameraNode = _scene.CreateChild("cameraNode");
            _camera = cameraNode.CreateComponent<Camera>();
            _camera.OrthoSize = Graphics.Height * 0.01f/*PIXEL_SIZE*/; // Set camera ortho size (the value of PIXEL_SIZE is 0.01)

            cameraNode.Position = CameraPosition;

            Node lightNode = cameraNode.CreateChild("lightNode");
            _light = lightNode.CreateComponent<Light>();
            _light.LightType = LightType.Point;
            _light.Range = 100;
            _light.Brightness = 1f;


            cameraNode.LookAt(Vector3.Zero, Vector3.Up, TransformSpace.World);
        }

        void SetupViewport()
        {
            var renderer = Renderer;
            renderer.DefaultZone.FogColor = Color.Gray;
            renderer.SetViewport(0, new Viewport(Context, _scene, _camera, null));

            UnhandledException += UrhoViewApp_UnhandledException;
#if DEBUG
            Engine.PostRenderUpdate += Engine_PostRenderUpdate;
#endif
        }

        #endregion Initialisation

        private void Input_TouchEnd(TouchEndEventArgs obj)
        {
            TouchedNode = null;
        }

        private void Input_TouchBegin(TouchBeginEventArgs obj)
        {
            Debug.WriteLine($"Input_TouchBegin {obj.X},{obj.Y}");

            Ray cameraRay = Camera.GetScreenRay((float)obj.X / Graphics.Width, (float)obj.Y / Graphics.Height);
            var results = Octree.Raycast(cameraRay, RayQueryLevel.Triangle, 100, DrawableFlags.Geometry);

            TouchedNode = results.Select(x => x.Node).FirstOrDefault();

            if(TouchedNode != null)
            {
                Debug.WriteLine($"Input Touch");
            }
        }
    }

    public static class UrhoHelpers
    {
        public static T AddChild<T>(this UrhoApp app, string label) where T : Component
        {
            if (!app.IsInitialized)
                return null;

            return app.RootNode.AddChild<T>(label);
        }

        public static T AddChild<T>(this Component component, string label) where T : Component
        {
            return component.Node?.AddChild<T>(label);
        }

        public static T AddChild<T>(this Node node, string label) where T : Component
        {
            var childNode = node.CreateChild(label);
            return childNode.CreateComponent<T>();
        }
    }
}
