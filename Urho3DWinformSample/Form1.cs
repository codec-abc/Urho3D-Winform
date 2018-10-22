using System;
using Urho;
using Urho.Desktop;
using Urho.Extensions.WinForms;
using Urho.Gui;
using System.Collections.Generic;

namespace Urho3DWinformSample
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        public static Form1 Instance;
        UrhoSurface surface = new UrhoSurface();
        private UrhoApp _app;

        public Form1()
        {
            InitializeComponent();
            Instance = this;
            surface.Dock = System.Windows.Forms.DockStyle.Fill;

            DesktopUrhoInitializer.AssetsDirectory = 
                System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets");

            urhoSurfacePlaceholder.Controls.Add(surface);
        }
        
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            var value = trackBar1.Value;
            var range = trackBar1.Maximum - trackBar1.Minimum;
            _app.PublishEvent(new SizeEvent(value + 1, range));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            surface.Show(typeof(UrhoApp), new ApplicationOptions(DesktopUrhoInitializer.AssetsDirectory));

            trackBar1_Scroll(null, null);
        }

        internal void SetApp(UrhoApp urhoApp)
        {
            _app = urhoApp;
        }
    }

    public class AppEvent
    {

    }

    public class SizeEvent : AppEvent
    {
        public SizeEvent(int value, int range)
        {
            Value = value;
            Range = range;
        }

        public int Value { get; private set; }
        public int Range { get; private set; }
    }

    public class CubeMover : Component
    {
        public CubeMover() : base()
        {
            this.ReceiveSceneUpdates = true;
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);
            this.Node.Position = 
                new Vector3
                (
                    (float) Math.Sin(Application.Time.ElapsedTime) + 6.0f, 
                    this.Node.Position.Y, 
                    this.Node.Position.Z
                );
            
        }
    }

    public class UrhoApp : Urho.Application
    {
        private Queue<AppEvent> Events = new Queue<AppEvent>();
        private object _eventLock = new object();

        private Node boxNode;

        public Viewport Viewport { get; private set; }

        public UrhoApp(ApplicationOptions options) : base(options)
        {
        }

        public UrhoApp(IntPtr options) : base(options)
        {
        }

        protected override void Start()
        {
            base.Start();
            CreateScene();
            Input.KeyDown += (e) =>
            {

            };

            Form1.Instance.SetApp(this);
            this.Update += App_Update;
        }

        private void App_Update(UpdateEventArgs obj)
        {
            AppEvent[] events;

            lock (_eventLock)
            {
                events = Events.ToArray();
                Events = new Queue<AppEvent>();
            }

            foreach (SizeEvent szEvent in events)
            {
                float size = (float)szEvent.Value / szEvent.Range;
                boxNode.SetScale(size);
            }
        }

        void CreateScene()
        {
            // UI text
            var helloText = new Text()
            {
                Value = "Hello World from MySample",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            helloText.SetColor(new Color(0f, 1f, 1f));
            helloText.SetFont(
                font: ResourceCache.GetFont("Fonts/Font.ttf"),
                size: 30);
            UI.Root.AddChild(helloText);

            // Create a top-level scene, must add the Octree
            // to visualize any 3D content.
            var scene = new Scene();
            scene.CreateComponent<Octree>();
            // Box
            boxNode = scene.CreateChild();
            boxNode.Position = new Vector3(0, 1, 5);
            boxNode.Rotation = new Quaternion(60, 0, 30);
            boxNode.SetScale(1f);
            StaticModel modelObject = boxNode.CreateComponent<StaticModel>();
            modelObject.Model = ResourceCache.GetModel("Models/Box.mdl");
            // Light
            Node lightNode = scene.CreateChild(name: "light");
            lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
            var light = lightNode.CreateComponent<Light>();
            light.Range = 100;

            // Camera
            Node cameraNode = scene.CreateChild(name: "camera");
            Camera camera = cameraNode.CreateComponent<Camera>();

            Node cubeMoverNode = scene.CreateChild(name: "cubeMover");
            StaticModel modelObject2 = cubeMoverNode.CreateComponent<StaticModel>();
            modelObject2.Model = modelObject.Model;

            cubeMoverNode.Position = new Vector3(0, 0, 10);
            cubeMoverNode.Rotation = new Quaternion(60, 0, 30);
            CubeMover cubeMover = new CubeMover();
            cubeMoverNode.AddComponent(cubeMover);


            // Viewport
            Renderer.SetViewport(0, new Viewport(scene, camera, null));
        }

        internal void PublishEvent(SizeEvent sizeEvent)
        {
            lock (_eventLock)
            {
                Events.Enqueue(sizeEvent);
            }
        }

    }
}
