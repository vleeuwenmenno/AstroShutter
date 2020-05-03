using System.IO;
using System.Threading;
using System;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using System.Timers;
using System.ComponentModel;
using AstroShutter.CliWrapper;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AstroShutter
{	
	public class MainForm : Form
	{	
		private Camera camera;
		private Program program;
		private DateTime? finishedAtTime;

		// Indicates if we should pause capturing
		private bool shouldBreak = false;

		// Indicates if we should abort capturing
		private bool shouldAbort = false;

		private int editingEntry = 0;
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
		private readonly TextBox exposuresTxt;
		private readonly TextBox imageTypeTxt;

		private readonly TextArea capturingStatusTextArea;
		
		private readonly TableLayout mainTableLayout;
		private readonly TableLayout projectSettingsTable;
		private readonly TableLayout cameraSettingsTable;
		private readonly TableLayout captureInfoTable;

		private readonly ButtonMenuItem connectBtn;
		private readonly ButtonMenuItem disconnectBtn;
		private readonly ButtonMenuItem capturePreviewMenuBtn;

		private readonly Label lblBusyMessage;
		private readonly Label lblConnectionStatus;

		private readonly Button captureSequenceBtn;
		private readonly Button capturePreviewBtn;
		private readonly Button projectAddEntryBtn;
		private readonly Button projectUpdateEntryBtn;
		private readonly Button projectDelEntryBtn;
		private readonly Button startAndPauseBtn;
		private readonly Button abortBtn;

		private readonly ButtonMenuItem newProjectBtn;
		private readonly ButtonMenuItem openProjectBtn;
		private readonly ButtonMenuItem saveProjectBtn;
		private readonly ButtonMenuItem saveAsProjectBtn;

		private readonly ImageView cameraPreviewImg;

		private readonly ListBox projectListBoxExp;
		private readonly ListBox projectListBoxShut;
		private readonly ListBox projectListBoxDur;
		private readonly ListBox projectListBoxIso;
		private readonly ListBox projectListBoxQuality;
		private readonly ListBox projectListBoxStatus;

		private readonly CheckBox downloadAftChk;
		private readonly CheckBox downloadImChk;
		private readonly CheckBox requestUserInputChk;
		private readonly CheckBox createSubDirChk;
		private readonly CheckBox createDirChk;

		#endregion

 		System.Timers.Timer cameraWatch;

		public MainForm()
		{
			XamlReader.Load(this);

			projectJsonPath = "";
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
			exposuresTxt = FindChild<TextBox>("exposuresTxt");
			imageTypeTxt = FindChild<TextBox>("imageTypeTxt");

			capturingStatusTextArea = FindChild<TextArea>("capturingStatusTextArea");

			mainTableLayout = FindChild<TableLayout>("mainTableLayout");
			cameraSettingsTable = FindChild<TableLayout>("cameraSettingsTable");
			projectSettingsTable = FindChild<TableLayout>("projectSettingsTable");
			captureInfoTable = FindChild<TableLayout>("captureInfoTable");

            lblBusyMessage = FindChild<Label>("lblBusyMessage");
			lblConnectionStatus =  FindChild<Label>("lblConnectionStatus");

            captureSequenceBtn = FindChild<Button>("captureSequenceBtn");
            capturePreviewBtn = FindChild<Button>("capturePreviewBtn");
            projectAddEntryBtn = FindChild<Button>("projectAddEntryBtn");
            projectUpdateEntryBtn = FindChild<Button>("projectUpdateEntryBtn");
            projectDelEntryBtn = FindChild<Button>("projectDelEntryBtn");
            startAndPauseBtn = FindChild<Button>("startAndPauseBtn");
            abortBtn = FindChild<Button>("abortBtn");

            cameraPreviewImg = FindChild<ImageView>("cameraPreviewImg");

			projectListBoxExp = FindChild<ListBox>("projectListBoxExp");
			projectListBoxShut = FindChild<ListBox>("projectListBoxShut");
			projectListBoxDur = FindChild<ListBox>("projectListBoxDur");
			projectListBoxIso = FindChild<ListBox>("projectListBoxIso");
			projectListBoxQuality = FindChild<ListBox>("projectListBoxQuality");
			projectListBoxStatus = FindChild<ListBox>("projectListBoxStatus");

			downloadAftChk = FindChild<CheckBox>("downloadAftChk");
			downloadImChk = FindChild<CheckBox>("downloadImChk");
			requestUserInputChk = FindChild<CheckBox>("requestUserInputChk");
			createSubDirChk = FindChild<CheckBox>("createSubDirChk");
			createDirChk = FindChild<CheckBox>("createDirChk");

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
			imageTypeTxt.ToolTip = imageTypeTxt.ToolTip + $"{Environment.NewLine}{Environment.NewLine}Light Frames{Environment.NewLine}The Light Frames are the images that contains the real information: images of galaxies, nebula...{Environment.NewLine}{Environment.NewLine}Dark Frames and Dark Flat Frames{Environment.NewLine}The Dark Frames are used to remove the dark signal from the light frames (or the flat frames for the Dark Flat frames).{Environment.NewLine}The best way to create the dark frames is to shoot pictures in the dark (hence the name) by covering the lens.{Environment.NewLine}{Environment.NewLine}The dark frames must be created with the exposure time, temperature and ISO speed of the light frames (resp. flat frames).{Environment.NewLine}{Environment.NewLine}Bias Frames (aka Offset Frames){Environment.NewLine}The Bias/Offset Frames are used to remove the CCD or CMOS chip readout signal from the light frames.{Environment.NewLine}{Environment.NewLine}It's very easy to create bias/offset frames: just take the shortest possible exposure (it may be 1/4000s or 1/8000s depending on your camera) in the dark by covering the lens.{Environment.NewLine}The bias frames must be create with the ISO speed of the light frames. The temperature is not important.{Environment.NewLine}{Environment.NewLine}Flat Frames{Environment.NewLine}The Flat Frames are used to correct the vignetting and uneven field illumination created by dust or smudges in your optical train.{Environment.NewLine}To create good flat frames it is very important to not remove your camera from your telescope before taking them (including not changing the focus).{Environment.NewLine}You can use a lot of different methods (including using a flatbox) but I found that the simplest way is to put a white T shirt in front of your telescope and  smooth out the folds. Then shoot something luminous (a flash, a bright white light, the sky at dawn...) and let the camera decide of the exposure time (Av mode),{Environment.NewLine}The flat frames should be created with the ISO speed of the light frames. The temperature is not important.{Environment.NewLine}{Environment.NewLine}Info source: http://deepskystacker.free.fr/english/faq.htm";

			projectListBoxExp.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxShut.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxDur.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxIso.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxQuality.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);
			projectListBoxStatus.SelectedValueChanged += new EventHandler<EventArgs>(projectListBox_ValueChanged);

			projectListBoxExp.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectListBox_DoubleClick);
			projectListBoxShut.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectListBox_DoubleClick);
			projectListBoxDur.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectListBox_DoubleClick);
			projectListBoxIso.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectListBox_DoubleClick);
			projectListBoxQuality.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectListBox_DoubleClick);
			projectListBoxStatus.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectListBox_DoubleClick);

			projectDirectoryTxt.MouseDoubleClick += new EventHandler<MouseEventArgs>(projectDirectoryTxt_DoubleClick);

			projectAddEntryBtn.MouseUp += new EventHandler<MouseEventArgs>(projectAddEntryBtn_Click);
			projectUpdateEntryBtn.MouseUp += new EventHandler<MouseEventArgs>(projectUpdateEntryBtn_Click);
			projectDelEntryBtn.MouseUp += new EventHandler<MouseEventArgs>(projectDelEntryBtn_Click);

			downloadAftChk.CheckedChanged += new EventHandler<EventArgs>(downloadAftChk_CheckedChanged);
			downloadImChk.CheckedChanged += new EventHandler<EventArgs>(downloadImChk_CheckedChanged);
			createSubDirChk.CheckedChanged += new EventHandler<EventArgs>(createSubDirChk_CheckedChanged);
			
			#endregion

			mainTableLayout.Enabled = false;

			program = new Program { entries = new List<ProgramEntry>() };
			UpdateProgramEntries();
		}

        private void form_OnClosing(object sender, CancelEventArgs e)
        {
			if (projectChanged())
			{
				if (cameraWatch != null && cameraWatch.Enabled == true)
					cameraWatch.Stop();

				if (camera != null && camera.Connected)
				{
					MessageBox.Show("Please disconnect the camera before closing.");
					e.Cancel = true;
					return;
				}
				
				DialogResult res = MessageBox.Show("You seem to have unsaved changes, do you want to quit and discard changes?", MessageBoxButtons.YesNo, MessageBoxType.Warning);
				
				if (res != DialogResult.Yes)
					e.Cancel = true;
			}
			else
			{
				if (cameraWatch != null && cameraWatch.Enabled == true)
					cameraWatch.Stop();

				if (camera != null && camera.Connected)
				{
					MessageBox.Show("Please disconnect the camera before closing.");
					e.Cancel = true;
				}
			}
        }

		#region Misc functions

        private bool projectChanged()
        {
			if (!File.Exists(projectJsonPath) && program.entries.Count > 0)
				return true;
			else
				return projectJsonPath != "" ? ComputeSha256Hash(program.JsonString()) != ComputeSha256Hash(File.ReadAllText(projectJsonPath)) : false;
        }

		string ComputeSha256Hash(string rawData)  
        {  
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())  
            {  
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
  
                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();  
                for (int i = 0; i < bytes.Length; i++)  
                {  
                    builder.Append(bytes[i].ToString("x2"));  
                }  
                return builder.ToString();  
            }  
        }  

		private void captureProgram()
		{
			finishedAtTime = null;
			program.sequenceStarted = true;
			program.sequenceStartedAt = DateTime.Now;

			foreach (ProgramEntry entry in program.entries)
			{					
				if (entry.exposuresDone == null)
					entry.exposuresDone = new List<List<string>>();

				entry.queueBegun = true;

				camera.iso = entry.iso;
				camera.shutterSpeed = entry.duration;
				camera.imageFormat = entry.imageQuality;
				camera.captureTarget = CaptureTarget.MemoryCard;

				while (entry.exposuresDone.Count < entry.exposures)
				{
					int.TryParse(entry.duration, out int bulb);
					string msg = "";
					if (entry.isBulb)
					{
						List<string> files = camera.captureImage(bulb);
						if (files.Count > 0)
						{
							entry.exposuresDone.Add(files);							
							msg += $"Captured photo {files[0]}";
						}
						else
						{
							// Stop what we are doing and let the camera watcher handle the rest
							startAndPauseBtn.Text = "Resume";
							shouldBreak = true;
							startAndPauseBtn.Enabled = false;
							program.sequenceStarted = false;
							break;
						}
					}
					else
					{
						List<string> files = camera.captureImage();
						if (files.Count > 0)
						{
							entry.exposuresDone.Add(files);							
							msg += $"Captured photo {files[0]}";
						}
						else
						{
							// Stop what we are doing and let the camera watcher handle the rest
							startAndPauseBtn.Text = "Resume";
							shouldBreak = true;
							startAndPauseBtn.Enabled = false;
							program.sequenceStarted = false;
							break;
						}
					}

					program.subject = projectSubjectTxt.Text;
					program.saveDirectory = projectDirectoryTxt.Text;
					program.downloadIm = (bool)downloadImChk.Checked;
					program.downloadAft = (bool)downloadAftChk.Checked;
					program.requestUserInput = (bool)requestUserInputChk.Checked;
					program.createDir = (bool)createDirChk.Checked;
					program.createSubDir = (bool)createSubDirChk.Checked;

					Application.Instance.Invoke(() => this.Title = $"AstroShutter - [{projectJsonPath}]");
					Application.Instance.Invoke(() => lblBusyMessage.Text = msg + " and saved state to project file");

					program.Save(projectJsonPath);
					
					Thread.Sleep(1000);
					if (program.downloadIm)
					{
						///TODO: Download listed files!
					}

					Application.Instance.Invoke(() => 
					{
						startAndPauseBtn.Enabled = false;
						startAndPauseBtn.Text = "Done";
						abortBtn.Text = "Reset";
					});

					if (shouldBreak)
						break;
				}

				if (shouldBreak)
					break;

				if (program.requestUserInput)
					Application.Instance.Invoke(() => MessageBox.Show($"{entry.exposuresDone.Count}/{entry.exposures} done for {entry.shutter} frames, press ok to coninue.", MessageBoxButtons.OK, MessageBoxType.Information));
			}

			if (shouldBreak)
			{
				startAndPauseBtn.Enabled = true;
				shouldBreak = false;

				if (shouldAbort)
				{					
					Application.Instance.Invoke(() => lblBusyMessage.Text = "Capturing aborted, all entries have been reset!");
					shouldAbort = false;
					
					ResetEntries();
				}
				else
					Application.Instance.Invoke(() => lblBusyMessage.Text = "Paused capturing, click resume to continue capturing");

				return;
			}

			if (program.downloadAft)
			{
				///TODO: Download all listed files!
			}

			program.sequenceFinished = true;
			program.sequenceStarted = false;
			finishedAtTime = DateTime.Now;

			program.subject = projectSubjectTxt.Text;
			program.saveDirectory = projectDirectoryTxt.Text;
			program.downloadIm = (bool)downloadImChk.Checked;
			program.downloadAft = (bool)downloadAftChk.Checked;
			program.requestUserInput = (bool)requestUserInputChk.Checked;
			program.createDir = (bool)createDirChk.Checked;
			program.createSubDir = (bool)createSubDirChk.Checked;

			Application.Instance.Invoke(() => 
			{
				this.Title = $"AstroShutter - [{projectJsonPath}]";

				lblBusyMessage.Text = "Program completed!";

				startAndPauseBtn.Text = "Start";
				abortBtn.Text = "Reset";
			});

			program.Save(projectJsonPath);
		}

		#endregion
		
		#region ControlChanged Events

        private void createSubDirChk_CheckedChanged(object sender, EventArgs e)
        {
            if (createSubDirChk.Checked == true)
			{
				createDirChk.Checked = true;
				createDirChk.Enabled = false;
			}
			else
				createDirChk.Enabled = true;
        }

        private void downloadImChk_CheckedChanged(object sender, EventArgs e)
        {
            if (downloadImChk.Checked == true)
				downloadAftChk.Checked = false;
        }

        private void downloadAftChk_CheckedChanged(object sender, EventArgs e)
        {
            if (downloadAftChk.Checked == true)
				downloadImChk.Checked = false;
        }

        private void projectListBox_ValueChanged(object sender, EventArgs e)
        {
            ListBox s = (ListBox)sender;

			projectListBoxExp.SelectedIndex = s.SelectedIndex;
			projectListBoxShut.SelectedIndex = s.SelectedIndex;
			projectListBoxDur.SelectedIndex = s.SelectedIndex;
			projectListBoxIso.SelectedIndex = s.SelectedIndex;
			projectListBoxQuality.SelectedIndex = s.SelectedIndex;
			projectListBoxStatus.SelectedIndex = s.SelectedIndex;

			if (projectListBoxExp.SelectedIndex != 0)
			{				
                projectUpdateEntryBtn.Enabled = true;
				projectDelEntryBtn.Enabled = true;
			}
            else
			{
				projectDelEntryBtn.Enabled = false;
                projectUpdateEntryBtn.Enabled = false;
			}
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

		#endregion

		#region UI Update functions

        private void UpdateCaptureStatusText()
        {
            int frames = 0;
			int totalTimeSeconds = 0;

			foreach (ProgramEntry pe in program.entries)
			{
				int.TryParse(pe.duration, out int dur);

				if (dur == 0)
					dur = 1;

				if (pe.exposuresDone == null)
					pe.exposuresDone = new List<List<string>>();

				totalTimeSeconds += (pe.exposures - pe.exposuresDone.Count) * dur; /// +1 to take into account the delay for waiting to release camera USB claim
				frames += (pe.exposures - pe.exposuresDone.Count);
				totalTimeSeconds += 2 * pe.exposures;
			}

			TimeSpan t = TimeSpan.FromSeconds( totalTimeSeconds );
			string remaining = string.Format("{0:D2} hours, {1:D2} min and {2:D2} sec", 
							t.Hours, 
							t.Minutes, 
							t.Seconds);

			DateTime startTime = program.sequenceStarted ? (DateTime)program.sequenceStartedAt : DateTime.Now;
			DateTime endTime = DateTime.Now;

			TimeSpan tt = endTime.Subtract ( startTime );
			string taken = string.Format("{0:D2} hours, {1:D2} min and {2:D2} sec", 
							tt.Hours, 
							tt.Minutes,
							tt.Seconds);
			
			DateTime finishTime;

			if (finishedAtTime == null)
				finishTime = DateTime.Now.AddSeconds(t.TotalSeconds);
			else
				finishTime = (DateTime)finishedAtTime;

			capturingStatusTextArea.Text = $"{Environment.NewLine}Capturing\t\t\t\t:\t{frames} Frames{Environment.NewLine}{Environment.NewLine}Time Taken\t\t\t\t:\t{taken}{Environment.NewLine}Est. Time Remaining\t:\t{remaining}{Environment.NewLine}Finish Time\t\t\t\t:\t{finishTime.ToString("HH:mm:ss")}";
        }

		private void ResetProgram()
		{
			program = new Program { entries = new List<ProgramEntry>() };

			UpdateProgramEntries();

			projectDirectoryTxt.Text = "";
			projectSubjectTxt.Text = "";
			downloadAftChk.Checked = false;
			downloadImChk.Checked = false;
			requestUserInputChk.Checked = true;

			projectJsonPath = "";

			projectSettingsTable.Enabled = true;
			cameraSettingsTable.Visible = true;
			captureInfoTable.Visible = false;

			this.Title = $"AstroShutter - [New Project]";
			lblBusyMessage.Text = "Created new project";

			ResetEntries();
		}

		private void ResetEntries()
		{
			Application.Instance.Invoke(() => 
			{
				startAndPauseBtn.Enabled = true;
				abortBtn.Enabled = false;

				program.sequenceStarted = false;
				program.sequenceFinished = false;
				program.sequenceStartedAt = null;
				
				foreach(ProgramEntry entry in program.entries)
				{
					entry.exposuresDone.Clear();
					entry.queueBegun = false;
				}
				
				lblBusyMessage.Text = "All entries have been reset!";

				abortBtn.Text = "Abort";
				UpdateProgramEntries();
			});
		}

        private void PopulateControls()
        {
			lblBusyMessage.Text = "Loading camera parameters...";
			mainTableLayout.Enabled = false;

			new Thread(() => 
			{
				isoComboBox.Items.Clear();
				apertureComboBox.Items.Clear();
				shutterSpeedComboBox.Items.Clear();
				colorSpaceComboBox.Items.Clear();
				// capTargetComboBox.Items.Clear();
				imageFormatComboBox.Items.Clear();

				foreach (string iso in camera.isoOptions)
					isoComboBox.Items.Add(iso);

				foreach (string aper in camera.apertureOptions)
					apertureComboBox.Items.Add(aper);
				
				foreach (string shutterSpeed in camera.shutterSpeedOptions)
					shutterSpeedComboBox.Items.Add(shutterSpeed);

				foreach (string cs in camera.colorSpaceOptions)
					colorSpaceComboBox.Items.Add(cs);

				foreach (string iFormat in camera.imageFormatOptions)
					imageFormatComboBox.Items.Add(iFormat);

				// capTargetComboBox.Items.Add(CaptureTarget.MemoryCard.ToString());
				// capTargetComboBox.Items.Add(CaptureTarget.InternalRAM.ToString());

				isoComboBox.Text = camera.iso.ToString();
				apertureComboBox.Text = camera.aperture.ToString();
				shutterSpeedComboBox.Text = camera.shutterSpeed;
				colorSpaceComboBox.Text = camera.colorSpace;
				// capTargetComboBox.Text = camera.captureTarget.ToString();
				imageFormatComboBox.Text = camera.imageFormat;

				Application.Instance.Invoke(() => 
				{
					mainTableLayout.Enabled = true;
					lblBusyMessage.Text = "Done!";

					newProjectBtn.Enabled = true;
					openProjectBtn.Enabled = true;
					saveProjectBtn.Enabled = true;
					saveAsProjectBtn.Enabled = true;
				});

				/// Wait a short while before starting the camera watcher
				Thread.Sleep(1000);
				Application.Instance.Invoke(() => cameraWatch.Start());
			}).Start();
        }
		
        private void UpdateProgramEntries()
		{
			projectListBoxExp.Items.Clear();
			projectListBoxShut.Items.Clear();
			projectListBoxDur.Items.Clear();
			projectListBoxIso.Items.Clear();
			projectListBoxQuality.Items.Clear();
			projectListBoxStatus.Items.Clear();

			projectListBoxExp.Items.Add("Exposures");
			projectListBoxShut.Items.Add("Shutter");
			projectListBoxDur.Items.Add("Duration");
			projectListBoxIso.Items.Add("ISO");
			projectListBoxQuality.Items.Add("Image Quality");
			projectListBoxStatus.Items.Add("Capture Status");

			foreach (ProgramEntry e in program.entries)
			{
				projectListBoxExp.Items.Add($"{e.exposures}");
				projectListBoxShut.Items.Add($"{e.shutter}");
				projectListBoxDur.Items.Add($"{e.duration}s");
				projectListBoxIso.Items.Add($"ISO {e.iso}");
				projectListBoxQuality.Items.Add($"{e.imageQuality}");
				projectListBoxStatus.Items.Add(e.queueBegun ? e.exposuresDone.Count == e.exposures ? $"Done" : $"Running {e.exposuresDone.Count}/{e.exposures}" : $"In queue");
			}
		}

		#endregion

		#region Click Events

        private void projectListBox_DoubleClick(object sender, MouseEventArgs e)
		{
			editingEntry = projectListBoxExp.SelectedIndex-1;
            if (editingEntry >= 0)
			{
				ProgramEntry entry = program.entries[editingEntry];

				exposuresTxt.Text = entry.exposures.ToString();
				imageTypeTxt.Text = entry.shutter;
				shutterSpeedComboBox.Text = entry.isBulb ? "bulb" : entry.duration;
				bulbTimeTxt.Text = entry.duration;
				isoComboBox.Text = entry.iso;
				imageFormatComboBox.Text = entry.imageQuality;
				
                projectUpdateEntryBtn.Enabled = true;
				projectDelEntryBtn.Enabled = true;
			}
            else
			{
				projectDelEntryBtn.Enabled = false;
                projectUpdateEntryBtn.Enabled = false;
			}
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

		private void captureBtn_Click(object sender, EventArgs e)
		{
			if (program.entries.Count == 0)
				MessageBox.Show("Please add an entry to your project schedule before beginning capturing.", MessageBoxType.Warning);
			else
			{
				projectSettingsTable.Enabled = false;
				cameraSettingsTable.Visible = false;
				captureInfoTable.Visible = true;

				UpdateCaptureStatusText();
			}
		}

        private void projectAddEntryBtn_Click(object sender, MouseEventArgs e)
		{
			ProgramEntry entry = new ProgramEntry();

			int.TryParse(exposuresTxt.Text, out int exposures);
			entry.exposures = exposures;
			entry.shutter = imageTypeTxt.Text;
			entry.isBulb = shutterSpeedComboBox.Text == "bulb";
			entry.duration = shutterSpeedComboBox.Text == "bulb" ? bulbTimeTxt.Text : shutterSpeedComboBox.Text;
			entry.iso = isoComboBox.Text;
			entry.imageQuality = imageFormatComboBox.Text;

			program.entries.Add(entry);
			UpdateProgramEntries();

			lblBusyMessage.Text = "Entry added";
			editingEntry = 0;
			projectDelEntryBtn.Enabled = false;
			projectUpdateEntryBtn.Enabled = false;
		}

		private void projectUpdateEntryBtn_Click(object sender, MouseEventArgs e)
		{
			ProgramEntry entry = program.entries[editingEntry];

			entry.exposures = int.Parse(exposuresTxt.Text);
			entry.shutter = imageTypeTxt.Text;
			entry.isBulb = shutterSpeedComboBox.Text == "bulb";
			entry.duration = shutterSpeedComboBox.Text == "bulb" ? bulbTimeTxt.Text : shutterSpeedComboBox.Text;
			entry.iso = isoComboBox.Text;
			entry.imageQuality = imageFormatComboBox.Text;

			program.entries[editingEntry] = entry;
			UpdateProgramEntries();

			lblBusyMessage.Text = "Entry updated";
			editingEntry = 0;
			projectDelEntryBtn.Enabled = false;
			projectUpdateEntryBtn.Enabled = false;
		}

		private void projectDelEntryBtn_Click(object sender, MouseEventArgs e)
		{
			DialogResult res = MessageBox.Show("Are you sure you want to delete this schedule entry?", MessageBoxButtons.YesNo, MessageBoxType.Warning);

			if (res == DialogResult.Yes)
			{
				program.entries.RemoveAt(editingEntry);
				UpdateProgramEntries();

				lblBusyMessage.Text = "Deleted an entry";
				editingEntry = 0;
				projectDelEntryBtn.Enabled = false;
                projectUpdateEntryBtn.Enabled = false;
			}
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

		private void startAndPauseBtn_Click(object sender, EventArgs e)
		{
			if (startAndPauseBtn.Text == "Pause")
			{
				abortBtn.Enabled = true;
				abortBtn.Text = "Reset";

				startAndPauseBtn.Text = "Resume";
				shouldBreak = true;
				startAndPauseBtn.Enabled = false;
				lblBusyMessage.Text = "Pausing, please wait while the last capture to completes...";
				return;
			}

			abortBtn.Enabled = true;
			abortBtn.Text = "Abort";

			capturePreviewBtn.Enabled = false;
			captureSequenceBtn.Enabled = false;
			startAndPauseBtn.Text = "Pause";

			new Thread(captureProgram).Start();
		}

        protected void quitBtn_Click(object sender, EventArgs e)
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
				program.downloadIm = (bool)downloadImChk.Checked;
				program.downloadAft = (bool)downloadAftChk.Checked;
				program.requestUserInput = (bool)requestUserInputChk.Checked;
				program.createDir = (bool)createDirChk.Checked;
				program.createSubDir = (bool)createSubDirChk.Checked;

				this.Title = $"AstroShutter - [{projectJsonPath}]";

				program.Save(projectJsonPath);
				lblBusyMessage.Text = $"Saved project to {projectJsonPath}";
			}
			else
				saveAsProjectBtn_Click(sender, e);
		}
		
		protected void newProjectBtn_Click(object sender, EventArgs e)
		{
			if (projectChanged())
			{
				DialogResult res = MessageBox.Show("Are you sure you want to make a new project? This will discard any unsaved changes.", MessageBoxButtons.YesNoCancel, MessageBoxType.Warning);

				if (res == DialogResult.Yes)
				{
					ResetProgram();
				}
			}
			else
				ResetProgram();
		}

		protected void openProjectBtn_Click(object sender, EventArgs e)
		{
			if (projectChanged())
			{
				DialogResult r = MessageBox.Show("Are you sure you want to open another project? This will discard any unsaved changes.", MessageBoxButtons.YesNoCancel, MessageBoxType.Warning);

				if (r != DialogResult.Yes)
					return;
			}

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
				downloadImChk.Checked = program.downloadIm;
				downloadAftChk.Checked = program.downloadAft;
				createDirChk.Checked = program.createDir;
				createSubDirChk.Checked = program.createSubDir;
				requestUserInputChk.Checked = program.requestUserInput;

				this.Title = $"AstroShutter - [{projectJsonPath}]";

				lblBusyMessage.Text = $"Loaded project from {projectJsonPath}";

				if (program.sequenceStarted && !program.sequenceFinished)
				{
					res = MessageBox.Show($"It's seems a running sequence was interrupted do you want to resume capturing?{Environment.NewLine}{Environment.NewLine}Yes - Will open the capture panel and allow you to click start to resume where capturing was left of.{Environment.NewLine}No - Will remove any status information and reset all program entries.", MessageBoxButtons.YesNo, MessageBoxType.Question);

					if (res == DialogResult.Yes)
					{
						projectSettingsTable.Enabled = false;
						cameraSettingsTable.Visible = false;
						captureInfoTable.Visible = true;

						abortBtn.Enabled = true;
						abortBtn.Text = "Reset";
						startAndPauseBtn.Text = "Resume";
					}
					else
					{
						ResetEntries();
					}
				}

				if (program.sequenceFinished)
				{
					abortBtn.Enabled = true;
					abortBtn.Text = "Reset";
					startAndPauseBtn.Enabled = false;
				}
			}
		}

		private void abortBtn_Click(object sender, EventArgs e)
		{
			if (abortBtn.Text == "Reset")
			{
				ResetEntries();
			}
			else
			{
				DialogResult res = MessageBox.Show($"Are you sure you want to abort? This will remove any status information and clear the project entries back to 0.", MessageBoxButtons.YesNo, MessageBoxType.Question);

				if (res ==  DialogResult.Yes)
				{
					shouldBreak = true;
					shouldAbort = true;

					lblBusyMessage.Text = "Aborting, waiting for camera to finish capturing...";
				}
			}
		}
	
		private void disconnectBtn_Click(object sender, EventArgs e)
		{
			lblConnectionStatus.Text = $"Camera disconnected!";

			mainTableLayout.Enabled = false;

			connectBtn.Enabled = true;
			disconnectBtn.Enabled = false;
			capturePreviewMenuBtn.Enabled = false;
			cameraWatch.Stop();

			camera = null;
		}

		private async void connectBtn_Click(object sender, EventArgs e)
		{
			ConnectForm cf = new ConnectForm();
			await cf.ShowModalAsync();
			
			try
			{
				camera = Cli.AutoDetect(true)[cf.selectedCamera];

				if (camera.Connected)
				{
					lblConnectionStatus.Text = $"Connection established to {camera.model}";

					cameraWatch = new System.Timers.Timer();

					cameraWatch.Interval = 2000;
					cameraWatch.Elapsed += new ElapsedEventHandler(cameraWatch_elapsed);

					connectBtn.Enabled = false;
					capturePreviewMenuBtn.Enabled = true;
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

        private void cameraWatch_elapsed(object sender, ElapsedEventArgs e)
        {
			if (projectChanged())
			{
				if (projectJsonPath == "")
					Application.Instance.Invoke(() => this.Title = $"AstroShutter - [New Project]*");
				else
					Application.Instance.Invoke(() => this.Title = $"AstroShutter - [{projectJsonPath}]*");
			}
			else
			{
				if (projectJsonPath == "")
					Application.Instance.Invoke(() => this.Title = $"AstroShutter - [New Project]");
				else
					Application.Instance.Invoke(() => this.Title = $"AstroShutter - [{projectJsonPath}]");
			}

			if (program.sequenceStarted)
				Application.Instance.Invoke(() => UpdateProgramEntries());

			Application.Instance.Invoke(() => UpdateCaptureStatusText());

            if (!camera.Connected && !camera.Busy && !program.sequenceStarted)
			{
				Application.Instance.Invoke(() => lblConnectionStatus.Text = $"Camera disconnected? Double checking...");

				Thread.Sleep(500);
				if (!camera.Connected && !camera.Busy)
				{
					Application.Instance.Invoke(() => 
					{
						lblConnectionStatus.Text = $"Camera disconnected!";

						mainTableLayout.Enabled = false;
						connectBtn.Enabled = true;
						disconnectBtn.Enabled = false;
						capturePreviewMenuBtn.Enabled = false;
						cameraWatch.Stop();
					});
				}
				else
					Application.Instance.Invoke(() => lblConnectionStatus.Text = $"Connection established to {camera.model}");
			}
        }
    
		#endregion
	}
}
