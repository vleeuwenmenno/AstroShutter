using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System.Timers;
using AstroShutter.CliWrapper;

namespace AstroShutter
{	
	public class ConnectForm : Dialog
	{	
		ListBox cameraList;
		Button connBtn;
		public int selectedCamera = 0;

		List<Camera> cameras;
		Timer cameraWatch;

		public ConnectForm()
		{
			this.Title = "Select your camera";
			cameraList = new ListBox();
			cameraList.Width = 300;
			cameraList.Height = 300;
			
			cameraList.SelectedIndexChanged += new EventHandler<EventArgs>(cameraList_selectedIndexChanged);
			cameraList.MouseDoubleClick += new EventHandler<MouseEventArgs>(cameraList_mouseDoubleClick);

			foreach (Camera cam in Cli.AutoDetect())
			{
				cameraList.Items.Add($"{cam.model} (Port: {cam.port})");
			}

			connBtn = new Button { Text = "Connect", Enabled = false };
			connBtn.MouseUp += new EventHandler<MouseEventArgs>(connBtn_mouseUp);

			Button cancelBtn = new Button { Text = "Cancel" };
			Button refreshBtn = new Button { Text = "Refresh" };
			cancelBtn.MouseUp += new EventHandler<MouseEventArgs>(cancelBtn_mouseUp);
			refreshBtn.MouseUp += new EventHandler<MouseEventArgs>(refreshBtn_mouseUp);
			
			DynamicLayout layout = new DynamicLayout();

			layout.Spacing = new Size(8, 8);
			layout.Padding = new Padding(8, 8);

			layout.AddRow(new Label { Text = "Cameras:"});
			layout.AddSeparateRow(cameraList);
			layout.AddSeparateRow(null, connBtn, refreshBtn, cancelBtn, null);

			this.Content = layout;

			cameraWatch = new Timer();

			cameraWatch.Interval = 1000;
			cameraWatch.Elapsed += new ElapsedEventHandler(cameraWatch_elapsed);

			cameraWatch.Start();
		}

        private void cameraWatch_elapsed(object sender, ElapsedEventArgs e)
        {
            if (cameras.Count != Cli.AutoDetect().Count)
			{
				cameraList.Items.Clear();
				cameras = Cli.AutoDetect();
				foreach (Camera cam in cameras)
				{
					cameraList.Items.Add($"{cam.model} (Port: {cam.port})");
				}

				connBtn.Enabled  = false;
			}
        }

        private void refreshBtn_mouseUp(object sender, MouseEventArgs e)
        {
				cameraList.Items.Clear();
				cameras = Cli.AutoDetect();
				foreach (Camera cam in cameras)
				{
					cameraList.Items.Add($"{cam.model} (Port: {cam.port})");
				}
        }

        private void cameraList_selectedIndexChanged(object sender, EventArgs e)
        {			
			cameras = Cli.AutoDetect();
            if (cameraList.SelectedIndex != -1 && cameras.Count >= cameraList.SelectedIndex)
			{
				selectedCamera = cameraList.SelectedIndex;
				connBtn.Enabled  = true;
				Console.WriteLine(cameras[selectedCamera].model + " at " + cameras[selectedCamera].port);
			}
			else if (cameraList.SelectedIndex != -1)
				connBtn.Enabled  = false;
        }

        private void cancelBtn_mouseUp(object sender, MouseEventArgs e)
        {
			cameraWatch.Stop();
            this.Close();
        }

        private void connBtn_mouseUp(object sender, MouseEventArgs e)
        {
			cameraWatch.Stop();
			this.Close();
        }

        private void cameraList_mouseDoubleClick(object sender, MouseEventArgs e)
        {
			List<Camera> cameras = Cli.AutoDetect();
            if (cameraList.SelectedIndex != -1 && cameras.Count >= cameraList.SelectedIndex)
			{
				selectedCamera = cameraList.SelectedIndex;
				connBtn.Enabled  = true;
				Console.WriteLine(cameras[selectedCamera].model);

				cameraWatch.Stop();
				this.Close();
			}
			else
				connBtn.Enabled  = false;
        }
    }
}
