﻿using System;
using Eto.Forms;

namespace AstroShutter.Wpf
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Wpf).Run(new MainForm());
		}
	}
}
