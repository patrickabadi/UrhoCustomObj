using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Urho;
using Urho.Forms;
using Xamarin.Forms;

namespace UrhoCustomObj
{
    public class UrhoView : ContentView
    {
        public UrhoApp App => _urhoApp;
        public TaskCompletionSource<bool> LoadingUrhoTask;

        public Action<int, double, double> MouseWheelPressed;
        public Action<double, double> RightMouseClickMoved;

        private UrhoSurface _urhoSurface;
        private UrhoApp _urhoApp;

        public UrhoView()
        {
            LoadingUrhoTask = new TaskCompletionSource<bool>();
            _urhoSurface = new UrhoSurface();

            Content = new Grid
            {
                Children =
                {
                    _urhoSurface
                }
            };
        }

        protected override async void OnParentSet()
        {
            base.OnParentSet();

            if (this.Parent == null)
            {
                await StopUrhoApp();
            }
            else
            {
                //await StartUrhoApp();
            }
        }

        public async Task StartUrhoApp()
        {
            if (_urhoSurface == null)
                throw new System.Exception("Urho Surface not defined");

            if (_urhoApp == null)
            {
                //This will fail if called twice within an application
                _urhoApp = await _urhoSurface.Show<UrhoApp>(
                    new ApplicationOptions(assetsFolder: null)
                    {
                        Orientation = ApplicationOptions.OrientationType.LandscapeAndPortrait,
                        TouchEmulation = true
                    });

                LoadingUrhoTask.SetResult(true);
            }
        }

        private async Task StopUrhoApp()
        {
            if (_urhoApp != null)
            {
                await _urhoApp.Exit();

                _urhoSurface = null;
                _urhoApp = null;
                Content = null;
            }
        }
    }
}
