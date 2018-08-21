using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Urho;
using UrhoCustomObj.Components;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace UrhoCustomObj
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : ContentPage
	{
        protected UrhoApp App => UrhoView.App;

        protected WorldInputHandler inputs;

        public MainPage()
		{
			InitializeComponent();
		}

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
            {
                UrhoView.StartUrhoApp();
                return false;
            });

        }

        private async void ColouredButton_Clicked(object sender, EventArgs e)
        {
            await InitializeUrho();

            var model = App.AddChild<ObjectModel>("model");

            await model.LoadMesh("UrhoCustomObj.Meshes.Model.obj", true);
        }

        private async void TexturedButton_Clicked(object sender, EventArgs e)
        {
            await InitializeUrho();

            var model = App.AddChild<ObjectModel>("model");

            await model.LoadTexturedMesh("UrhoCustomObj.Meshes.Model2.obj", "UrhoCustomObj.Meshes.Model2.png", true);
        }

        private async Task InitializeUrho()
        {
            // have to wait for the loading task to complete before adding components
            await UrhoView.LoadingUrhoTask.Task;

            App.RootNode.RemoveAllChildren();
            App.RootNode.SetWorldRotation(Quaternion.Identity);
            App.RootNode.Position = Vector3.Zero;
            App.Camera.Zoom = 1f;

            inputs = App.AddChild<WorldInputHandler>("inputs");

        }
    }
}
