using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
            {
                UrhoView.StartUrhoApp();
                return false;
            });

            // have to wait for the loading task to complete before adding components
            await UrhoView.LoadingUrhoTask.Task;


            // BUGBUG: for whatever reason this doesn't work on android, I think it's a thread/context issue
            if (Device.RuntimePlatform == Device.Android)
                return;

            inputs = App.AddChild<WorldInputHandler>("inputs");

            var model = App.AddChild<ObjectModel>("model");

            model.LoadMesh("UrhoCustomObj.Meshes.Model.obj", true);
        }
    }
}
