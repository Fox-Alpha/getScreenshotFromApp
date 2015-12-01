/*
 * Erstellt mit SharpDevelop.
 * Benutzer: buck
 * Datum: 12/01/2015
 * Zeit: 12:16
 * 
 */
using System;
using System.Windows.Forms;

namespace getScreenShot
{
	/// <summary>
	/// Class with program entry point.
	/// </summary>
	internal sealed class Program
	{
		/// <summary>
		/// Program entry point.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
		
	}
}
