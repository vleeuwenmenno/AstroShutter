using System.Threading;
using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System.Timers;
using System.ComponentModel;
using AstroShutter.CliWrapper;
using System.Collections.Generic;

namespace AstroShutter
{	
	public class MainForm : Form
	{	
		private Camera camera;
		private Program program;

		private string projectJsonPath;

		#region Controls

		private readonly ComboBox isoComboBox;
		private readonly ComboBox apertureComboBox;
		private readonly ComboBox shutterSpeedComboBox;
		private readonly ComboBox colorSpaceComboBox;
		// private readonly ComboBox capTargetComboBox;
		private readonly ComboBox imageFormatComboBox;

		private readonly TextBox bulbTimeTxt;
		private readonly TextBox projectDirectoryTxt;
		private readonly TextBox projectSubjectTxt;
		
		private readonly TableLayout mainTableLayout;

		private readonly ButtonMenuItem connectBtn;
		private readonly ButtonMenuItem disconnectBtn;

		private readonly Label lblBusyMessage;
		private readonly Label lblConnectionStatus;

		private readonly Button captureBtn;
		private readonly Button capturePreviewBtn;
		private readonly ButtonMenuItem newProjectBtn;
		private readonly ButtonMenuItem openProjectBtn;
		private readonly ButtonMenuItem saveProjectBtn;
		private readonly ButtonMenuItem saveAsProjectBtn;

		private readonly ImageView cameraPreviewImg;

		private readonly ListBox projectListBoxExp;
		private readonly ListBox projectListBoxShut;
		private readonly ListBox projectListBoxDur;
		private readonly ListBox projectListBoxIso;

		private readonly CheckBox downloadAftChk;
		private readonly CheckBox downloadImChk;
		private readonly CheckBox requestUserInputChk;

		#endregion


 		System.Timers.Timer cameraWatch;

		public MainForm()
		{
			XamlReader.Load(this);

			this.Closing += new EventHandler<CancelEventArgs>(form_OnClosing);

			#region Assigning Controls

			isoComboBox = FindChild<ComboBox>("isoComboBox");
			apertureComboBox = FindChild<ComboBox>("apertureComboBox");
			shutterSpeedComboBox = FindChild<ComboBox>("shutterSpeedComboBox");
			colorSpaceComboBox = FindChild<ComboBox>("colorSpaceComboBox");
			// capTargetComboBox = FindChild<ComboBox>("capTargetComboBox");
			imageFormatComboBox = FindChild<ComboBox>("imageFormatComboBox");

			bulbTimeTxt = FindChild<TextBox>("bulbTimeTxt");
			projectSubjectTxt = FindChild<TextBox>("projectSubjectTxt");
			projectDirectoryTxt = FindChild<TextBox>("projectDirectoryTxt");

			mainTableLayout = FindChild<TableLayout>("mainTableLayout");

            lblBusyMessage = FindChild<Label>("lblBusyMessage");
			lblConnectionStatus =  FindChild<Label>("lblConnectionStatus");

            captureBtn = FindChild<Button>("captureBtn");
            capturePreviewBtn = FindChild<Button>("capturePreviewBtn");

            cameraPreviewImg = FindChild<ImageView>("cameraPreviewImg");

			projectListBoxExp = FindChild<ListBox>("projectListBoxExp");
			projectListBoxShut = FindChild<ListBox>("projectListBoxShut");
			projectListBoxDur = FindChild<ListBox>("projectListBoxDur");
			projectListBoxIso = FindChild<ListBox>("projectListBoxIso");

			downloadAftChk = FindChild<CheckBox>("downloadAftChk");
			downloadImChk = FindChild<CheckBox>("downloadImChk");

			#endregion

			#region Events

			isoComboBox.SelectedValueChanged += new EventHandler<EventArgs>(isoComboBox_ValueChanged);
			apertureComboBox.SelectedValueChanged += new EventHandler<EventArgs>(apertureComboBox_ValueChanged);
			shutterSpeedComboBox.SelectedValueChanged += new EventHandler<EventArgs>(shutterSpeedComboBox_ValueChanged);
			colorSpaceComboBox.SelectedValueChanged += new EventHandler<EventArgs>(colorSpaceComboBox_ValueChanged);
			// capTargetComboBox.SelectedValueChanged += new EventHandler<EventArgs>(capTargetComboBox_ValueChanged);
			imageFormatComboBox.SelectedValueChanged += new EventHandler<EventArgs>(imageFormatComboBox_ValueChanged);

			shutterSpeedComboBox.ToolTip = $"The time the shutter is open to receiving light. (Longer exposure means a brighter image){Environment.NewLine}{Environment.NewLine}Without a tracking mount you can apply the 500 rule to prevent star-trailing.{Environment.NewLine}{Environment.NewLine}The 500 rule for a full frame camera requires you to set your camera to ISO 3200 or 6400, Aperture to f/2.8 (or as wide as possible) and your shutter speed to 500 divided by the focal length of your camera. For example, if you are shooting with a 50mm lens, your shutter speed would be 10 seconds (500 / 50 = 10).";
			bulbTimeTxt.ToolTip = $"Bulb time is the amount of seconds to keep the shutter open.{Environment.NewLine}{Environment.NewLine}To use this set the shutter speed to bulb and enter a valid integer.";

			projectListBoxExp.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxShut.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxDur.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxIso.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);

			projectDirectoryTxt.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectDirectoryTxt_DoubleClick);
			
			#endregion

			mainTableLayout.Enabled = false;

			program = new Program { entries = new List<ProgramEntry>() };
			program.entries.Add(new ProgramEntry(100, "light", "30", "1600"));
			program.entries.Add(new ProgramEntry(10, "bias", "1/4000", "1600"));
			program.entries.Add(new ProgramEntry(10, "dark", "30", "1600"));
			UpdateProgramEntries();
		}

        private void projectDirectoryTxt_DoubleClick(object sender, MouseEventArgs e)
        {
            SelectFolderDialog ofd = new SelectFolderDialog();

			ofd.Directory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			DialogResult dr = ofd.ShowDialog(this);

			if (dr == DialogResult.Ok)
			{
				projectDirectoryTxt.Text = ofd.Directory;
			}
        }

        private void UpdateProgramEntries()
		{
			projectListBoxExp.Items.Clear();
			projectListBoxShut.Items.Clear();
			projectListBoxDur.Items.Clear();
			projectListBoxIso.Items.Clear();

			projectListBoxExp.Items.Add("Exposures");
			projectListBoxShut.Items.Add("Shutter");
			projectListBoxDur.Items.Add("Duration");
			projectListBoxIso.Items.Add("ISO");

			foreach (ProgramEntry e in program.entries)
			{
				projectListBoxExp.Items.Add($"{e.exposures}");
				projectListBoxShut.Items.Add($"{e.shutter}");
				projectListBoxDur.Items.Add($"{e.duration}s");
				projectListBoxIso.Items.Add($"ISO {e.iso}");
			}
		}

        private void projectListBox_ValueChanged(object sender, EventArgs e)
        {
            ListBox s = (ListBox)sender;

			projectListBoxExp.SelectedIndex = s.SelectedIndex;
			projectListBoxShut.SelectedIndex = s.SelectedIndex;
			projectListBoxDur.SelectedIndex = s.SelectedIndex;
			projectListBoxIso.SelectedIndex = s.SelectedIndex;
        }

        private void imageFormatComboBox_ValueChanged(object sender, EventArgs e)
        {
			camera.imageFormat = imageFormatComboBox.SelectedIndex.ToString();
        }

        // private void capTargetComboBox_ValueChanged(object sender, EventArgs e)
        // {
		// 	if (capTargetComboBox.Text == "InternalRAM")
		// 		camera.captureTarget = CaptureTarget.InternalRAM;
		// 	else
		// 		camera.captureTarget = CaptureTarget.MemoryCard;
        // }

        private void colorSpaceComboBox_ValueChanged(object sender, EventArgs e)
        {
			camera.colorSpace = colorSpaceComboBox.Text;
        }

        private void apertureComboBox_ValueChanged(object sender, EventArgs e)
        {
            camera.aperture = double.Parse(apertureComboBox.Text);
        }

        private void isoComboBox_ValueChanged(object sender, EventArgs e)
        {
            camera.iso = isoComboBox.Text;
        }

        private void shutterSpeedComboBox_ValueChanged(object sender, EventArgs e)
        {
            if (shutterSpeedComboBox.Text == "bulb")
				bulbTimeTxt.Enabled = true;
			else
				bulbTimeTxt.Enabled = false;

			camera.shutterSpeed = shutterSpeedComboBox.Text;
        }

        private void capturePreviewBtn_Click(object sender, EventArgs e)
		{
			lblBusyMessage.Text = "Capturing preview...";

			new Thread(() => 
			{
				try
				{
					byte[] prev = camera.capturePreview();
					cameraPreviewImg.Image = new Bitmap(prev);
				}
				catch (Exception ex)
				{
					lblBusyMessage.Text = $"Error: {ex.Message}";
					return;
				}

				lblBusyMessage.Text = "Done";
			}).Start();
		}

		private void captureBtn_Click(object sender, EventArgs e)
		{
			if (program.entries.Count == 0)
				MessageBox.Show("Please add an entry to your project schedule before beginning capturing.", MessageBoxType.Warning);
			else
			{

			}
		}

        private void form_OnClosing(object sender, CancelEventArgs e)
        {
			if (cameraWatch != null && cameraWatch.Enabled == true)
            	cameraWatch.Stop();

			if (camera != null && camera.Connected)
			{
				MessageBox.Show("Please disconnect the camera before closing.");
				e.Cancel = true;
			}
        }

        protected void HandleQuit(object sender, EventArgs e)
		{
			this.Close();
		}

		protected void saveAsProjectBtn_Click(object sender, EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();

			sfd.Filters.Add(new FileFilter("AstroShutter Project File", new[] { "*.asproj" }));
			sfd.Filters.Add(new FileFilter("Json File", new[] { "*.json" }));

			sfd.FileName = "*.asproj";

			DialogResult res = sfd.ShowDialog(this);

			if (res == DialogResult.Ok)
			{
				projectJsonPath = sfd.FileName;
				saveProjectBtn_Click(sender, e);
			}
		}

		protected void saveProjectBtn_Click(object sender, EventArgs e)
		{
			if (projectJsonPath != null && projectJsonPath != "")
			{
				program.subject = projectSubjectTxt.Text;
				program.saveDirectory = projectDirectoryTxt.Text;
				program.downloadImmediatly = (bool)downloadImChk.Checked;
				program.downloadAfterwards = (bool)downloadAftChk.Checked;
				program.requestUserInput = (bool)requestUserInputChk.Checked;

				this.Title = $"AstroShutter - [{projectJsonPath}]";

				program.Save(projectJsonPath);
				lblBusyMessage.Text = $"Saved project to {projectJsonPath}";
			}
			else
				saveAsProjectBtn_Click(sender, e);
		}
		
		protected void newProjectBtn_Click(object sender, EventArgs e)
		{
			if (program.entries.Count > 0)
			{
				DialogResult res = MessageBox.Show("Are you sure you want to make a new project? This will discard any unsaved changes.", MessageBoxButtons.YesNoCancel, MessageBoxType.Warning);

				if (res == DialogResult.Yes)
				{
					program = new Program();

					UpdateProgramEntries();

					projectDirectoryTxt.Text = "";
					projectSubjectTxt.Text = "";
					downloadAftChk.Checked = false;
					downloadImChk.Checked = false;
					requestUserInputChk.Checked = true;

					this.Title = $"AstroShutter - [New Project]";
				}
			}
		}

		protected void openProjectBtn_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();

			ofd.Filters.Add(new FileFilter("AstroShutter Project File", new[] { "*.asproj" }));
			ofd.Filters.Add(new FileFilter("Json File", new[] { "*.json" }));

			ofd.FileName = "*.asproj";

			DialogResult res = ofd.ShowDialog(this);

			if (res == DialogResult.Ok)
			{
				projectJsonPath = ofd.FileName;
				program = Program.Load(projectJsonPath);

				UpdateProgramEntries();

				projectSubjectTxt.Text = program.subject;
				projectDirectoryTxt.Text = program.saveDirectory;
				downloadImChk.Checked = program.downloadImmediatly;
				downloadAftChk.Checked = program.downloadAfterwards;
				requestUserInputChk.Checked = program.requestUserInput;

				this.Title = $"AstroShutter - [{projectJsonPath}]";

				lblBusyMessage.Text = $"Loaded project from {projectJsonPath}";
			}
		}
		
		
		private void disconnectBtn_Click(object sender, EventArgs e)
		{
			lblConnectionStatus.Text = $"Camera disconnected!";

			mainTableLayout.Enabled = false;

			connectBtn.Enabled = true;
			disconnectBtn.Enabled = false;
			cameraWatch.Stop();

			camera = null;
		}

		private async void connectBtn_Click(object sender, EventArgs e)
		{
			ConnectForm cf = new ConnectForm();
			await cf.ShowModalAsync();
			
			try
			{
				camera = Cli.AutoDetect()[cf.selectedCamera];

				if (camera.Connected)
				{
					lblConnectionStatus.Text = $"Connection established to {camera.model}";

					cameraWatch = new System.Timers.Timer();

					cameraWatch.Interval = 2000;
					cameraWatch.Elapsed += new ElapsedEventHandler(cameraWatch_elapsed);

					connectBtn.Enabled = false;
					disconnectBtn.Enabled = true;

					PopulateControls();

					this.Focus();
				}
			}
			catch (Exception ex)
			{
				lblConnectionStatus.Text = $"Failed to connect to camera";
				Console.WriteLine(ex.StackTrace);
			}
		}

        private void PopulateControls()
        {
			lblBusyMessage.Text = "Loading camera parameters...";
			mainTableLayout.Enabled = false;

			new Thread(() => 
			{
				Application.Instance.Invoke(() => 
				{
					isoComboBox.Items.Clear();
					apertureComboBox.Items.Clear();
					shutterSpeedComboBox.Items.Clear();
					colorSpaceComboBox.Items.Clear();
					// capTargetComboBox.Items.Clear();
					imageFormatComboBox.Items.Clear();
				});

				foreach (string iso in camera.isoOptions)
					Application.Instance.Invoke(() => isoComboBox.Items.Add(iso));

				foreach (string aper in camera.apertureOptions)
					Application.Instance.Invoke(() => apertureComboBox.Items.Add(aper));
				
				foreach (string shutterSpeed in camera.shutterSpeedOptions)
					Application.Instance.Invoke(() => shutterSpeedComboBox.Items.Add(shutterSpeed));

				foreach (string cs in camera.colorSpaceOptions)
					Application.Instance.Invoke(() => colorSpaceComboBox.Items.Add(cs));

				foreach (string iFormat in camera.imageFormatOptions)
					Application.Instance.Invoke(() => imageFormatComboBox.Items.Add(iFormat));

				Application.Instance.Invoke(() => 
				{
					// capTargetComboBox.Items.Add(CaptureTarget.MemoryCard.ToString());
					// capTargetComboBox.Items.Add(CaptureTarget.InternalRAM.ToString());

					isoComboBox.Text = camera.iso.ToString();
					apertureComboBox.Text = camera.aperture.ToString();
					shutterSpeedComboBox.Text = camera.shutterSpeed;
					colorSpaceComboBox.Text = camera.colorSpace;
					// capTargetComboBox.Text = camera.captureTarget.ToString();
					imageFormatComboBox.Text = camera.imageFormat;

					mainTableLayout.Enabled = true;
					lblBusyMessage.Text = "Done!";
				});

				newProjectBtn.Enabled = true;
				openProjectBtn.Enabled = true;
				saveProjectBtn.Enabled = true;
				saveAsProjectBtn.Enabled = true;

				/// Wait a short while before starting the camera watcher
				Thread.Sleep(1000);
				Application.Instance.Invoke(() => cameraWatch.Start());
			}).Start();
        }

        private void cameraWatch_elapsed(object sender, ElapsedEventArgs e)
        {
            if (!camera.Connected && !camera.Busy)
			{
				lblConnectionStatus.Text = $"Camera disconnected? Double checking...";

				Thread.Sleep(500);
				if (!camera.Connected && !camera.Busy)
				{
					Application.Instance.Invoke(() => 
					{
						lblConnectionStatus.Text = $"Camera disconnected!";

						mainTableLayout.Enabled = false;
						connectBtn.Enabled = true;
						disconnectBtn.Enabled = false;
						cameraWatch.Stop();
					});
				}
				else
					lblConnectionStatus.Text = $"Connection established to {camera.model}";
			}
        }
    }
}
