using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using SharpCamera;
using System.Timers;
using System.ComponentModel;

namespace AstroShutter
{	
	public class MainForm : Form
	{	
		private TetheredCamera camera;
		private Label statusBox;
		private TextBox connectionStatusBox;

		ButtonMenuItem connectBtn;

		ButtonMenuItem disconnectBtn;

		Timer cameraWatch;

		public MainForm()
		{
			XamlReader.Load(this);

			this.Closing += new EventHandler<CancelEventArgs>(form_OnClosing);

			ImageView imageView = new ImageView ();

			statusBox = new Label();

			connectionStatusBox = new TextBox { Text = "Disconnected", ReadOnly = true };
			connectionStatusBox.Width = 196;

			var layout = new TableLayout 
			{
				Padding = new Padding (8),
				Spacing = new Size (8, 8),
				Rows = 
				{
					imageView,
					null,
					new TableRow 
					{ 
						Cells = 
						{ 
							new TableCell 
							{ 
								Control = statusBox 
							}, 
							null, 
							new TableCell 
							{ 
								Control = connectionStatusBox
							} 
						} 
					}
				}
			};

			this.Content = layout;
		}

        private void form_OnClosing(object sender, CancelEventArgs e)
        {
            cameraWatch.Stop();
			camera.Exit();

			if (camera.Connected)
			{
				MessageBox.Show("Please disconnect the camera before closing.");
				e.Cancel = true;
			}
        }

        protected void HandleQuit(object sender, EventArgs e)
		{
			if (camera != null && camera.Connected)
				camera.Exit();

			Environment.Exit(0);
		}
		
		private void disconnectBtn_Click(object sender, EventArgs e)
		{
			camera.Exit();
			cameraWatch_elapsed(null, null);
		}

		private async void connectBtn_Click(object sender, EventArgs e)
		{
			ConnectForm cf = new ConnectForm();
			await cf.ShowModalAsync();
			
			try
			{
				camera = TetheredCamera.Scan()[cf.selectedCamera];
				camera.Connect();

				if (camera.Connected)
				{
					statusBox.Text = $"Connection established to {camera.Name}";
					connectionStatusBox.Text = $"Connected";

					cameraWatch = new Timer();

					cameraWatch.Interval = 1000;
					cameraWatch.Elapsed += new ElapsedEventHandler(cameraWatch_elapsed);

					cameraWatch.Start();

					connectBtn.Enabled = false;
					disconnectBtn.Enabled = true;
				}
			}
			catch (Exception ex)
			{
				connectionStatusBox.Text = $"Disconnected";
				statusBox.Text = $"Failed to connect to camera";
				Console.WriteLine(ex.StackTrace);
			}
		}

        private void cameraWatch_elapsed(object sender, ElapsedEventArgs e)
        {
            if (!camera.Connected)
			{
				statusBox.Text = $"Camera disconnected!";
				connectionStatusBox.Text = $"Disconnected";

				connectBtn.Enabled = true;
				disconnectBtn.Enabled = false;
				cameraWatch.Stop();
			}
        }
    }
}
