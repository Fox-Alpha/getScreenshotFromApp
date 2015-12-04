/*
 * Erstellt mit SharpDevelop.
 * Benutzer: buck
 * Datum: 12/01/2015
 * Zeit: 12:16
 * 
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Management;
using System.IO;


namespace getScreenShot
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		private const int ALT = 0xA4;
		private const int EXTENDEDKEY = 0x1;
		private const int KEYUP = 0x2;
		private const uint Restore = 9;
		
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);
		
		[DllImport("user32.dll")]
		private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
		
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool IsIconic(IntPtr hWnd);
		
		[DllImport("user32.dll")]
		private static extern int ShowWindow(IntPtr hWnd, uint Msg);
		
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();		

		[DllImport("user32.dll", SetLastError=true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", SetLastError=true)]
		static extern bool SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
		
		[DllImport("user32.dll", SetLastError=true)]
		static extern IntPtr SetActiveWindow(IntPtr hWnd);

		#region Properties
        //	Rückgabewerte für Nagios
        enum nagiosStatus
		{
			Ok=0,
			Warning=1,
			Critical=2,
			Unknown=3
		}
        
        /// <summary>
        /// Optionen für den Vergleich der angegebenen Version und der aus
        /// dem Prozess ermittelten Versions Angabe
        /// </summary>
		[Flags()]
		public enum cmdActionArgsModeType : int
		{
			NONE = 0,
			APP,
			FULL,
			GUI,
			NoGUI,
			logOnly
		}

		int status = (int) nagiosStatus.Ok;

        Dictionary<string, string> dicCmdArgs;
		cmdActionArgsModeType _modeType;
		
		public cmdActionArgsModeType modeType
        {
			get { return _modeType; }
			set { _modeType = value; }
		}
		
		string captureSavePath = @"c:\temp\appScreenshots\";
		string captureFileNamePrefix = @"AppShot";
		string captureFileLogExt = "log";
		string captureImageExt = "png";
		
		
		string _cmdProcess;
		public string cmdProcess
        {
			get { return _cmdProcess; }
			set { _cmdProcess = value; }
		}
		
		
		#endregion
		
		#region FormFunktions
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			dicCmdArgs = new Dictionary<string, string>();
			modeType = cmdActionArgsModeType.NONE;			
			
			string strMode;
			string strProcess;
			
			//
			//	Übergebene Kommandozeilen Parameter ermitteln
			//
			
			if(check_cmdLineArgs())
			{			
				dicCmdArgs.TryGetValue("mode", out strMode);
				dicCmdArgs.TryGetValue("process", out strProcess);
				
				if(check_modeParameters(strMode) && !string.IsNullOrWhiteSpace(strProcess))
				{
					cmdProcess = strProcess;
					
//					if((modeType & cmdActionArgsModeType.NoGUI) != 0)
//					{
//						this.WindowState = FormWindowState.Minimized;
//						timerCapture.Enabled = true;
//					}
				}
			}
			//	#####
		}

		void MainForm_Shown(object sender, EventArgs e)
		{
			if((modeType & cmdActionArgsModeType.NoGUI) != 0)
			{
//				this.WindowState = FormWindowState.Minimized;
				timerCapture.Enabled = true;
			}
		}
		#endregion
		
		#region HelperFunktions
		
        /// <summary>
        /// Prüfen und ermitteln welche Parameter der Anwendung übergeben wurden
        /// Parameter werden in einem Dictionary gespeichert
        /// </summary>
		bool check_cmdLineArgs()
		{
			
			string cmdMode;
			
            if (Environment.GetCommandLineArgs().Length > 0)
            {
                cmdProcess = ParseCmdLineParam("process", Environment.CommandLine);
                cmdMode = ParseCmdLineParam("mode", Environment.CommandLine);

                if (!string.IsNullOrWhiteSpace(cmdProcess))
                {
                    dicCmdArgs.Add("process", cmdProcess);
                }
                else
                    dicCmdArgs.Add("process", string.Empty);

                if (!string.IsNullOrWhiteSpace(cmdMode))
                {
                    dicCmdArgs.Add("mode", cmdMode);
                }
                else
                    dicCmdArgs.Add("mode", string.Empty);
                
                return true;
            }
            return false;
		}
		
        /// <summary>
        /// Auslesen der Parameter aus dem Kommandozeilen Aufruf
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cmdline"></param>
        /// <returns></returns>
        string ParseCmdLineParam(string key, string cmdline)
		{
			string res = "";
			try
			{
				int end = 0;
				int start = 0;
				int pos = 0;
				
				//	Ersetzem von Anführungszeichen in der Parameterliste
				cmdline = Regex.Replace(cmdline, "\"", "");

                //  Start auf ersten Parameter beginnend mit ' -' setzen
                if ((pos = cmdline.IndexOf(" -", start)) > -1)
                {
                    start = cmdline.IndexOf(" -", start);
                    cmdline = cmdline.Substring(start, cmdline.Length - start);
                }
                else
                    return string.Empty;

                //Wenn Key nicht gefunden wurde, dann beenden.
                if ((start = cmdline.ToLower().IndexOf(key)) <= 0)
					return string.Empty;
				
				if (cmdline.Length == start+key.Length)
					return cmdline.Substring(start, cmdline.Length-start);
				
				//prüfen ob dem Parameter ein Wert mit '=' angehängt ist
				if (cmdline[start+key.Length] == '=') {
					//Start hinter das '=' setzten
					start += key.Length+1;
				}
				else				
					start += key.Length;
				
				//Position des nächsten Parameters ermitteln
				if(cmdline.Length > start)
					end = cmdline.IndexOf(" -", start);

				int length = 0;
				
				if (end > 0)
				{
					length = end-start;
				} 
				else 
				{
					length = cmdline.Length-start;
				}
				if(length > 0)
					res = cmdline.Substring(start, length);
				
			} 
			catch (System.Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return res;
		}
        
    	/// <summary>
        /// Übergebenen Kommandozeilenparameter prüfen ob dieser im enum enthalten ist
        /// </summary>
        /// <param name="value">Der Wert im Enum als String der geprüft werden soll</param>
    	bool check_modeParameters(string value)
    	{
        //	Übergebenen Action Parameter prüfen ob dieser im enum enthalten ist
	        cmdActionArgsModeType result;	
	        
        	foreach (string enumarg in Enum.GetNames(typeof(cmdActionArgsModeType)))
	        {
        		if (!string.IsNullOrWhiteSpace(value) && value.ToLower() == enumarg.ToLower())
                {
        			Debug.WriteLine("{0} = {1}",enumarg, value);
        			Enum.TryParse(enumarg, out result);
        			if ((modeType & cmdActionArgsModeType.NONE) != 0)  
                    {
        				modeType = result;
        			}
        			else
        				modeType = modeType | result;
        		} 
	        }
        	Debug.WriteLine("Mode = {0}", modeType);

            return (modeType & cmdActionArgsModeType.NONE) != 0 ? false : true;
        //	####
    	}
		
		/// <summary>
		/// Hängt dem Dateinamen einen Count an um Doppelungen bei
		/// Mehrfach vorhandenen Prozessen (JM4) zu vermeiden.
		/// </summary>
		/// <param name="count"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		string checkFileName(int count, string suffix = "", int pid = 0, int filetype = 0)
		{
			string strDate = DateTime.Now.ToString("yyyy-MM-dd");
			//	#### Verzeichnis zum speichern bei bedarf anlegen
			if (!Directory.Exists(captureSavePath)) {
				Directory.CreateDirectory(captureSavePath);
			}
			//	#### Unterverzeichnis mit Datum anlegen
			if (!Directory.Exists(captureSavePath + strDate)) {
				Directory.CreateDirectory(captureSavePath + strDate);
			}
			//	####
			
			//	#### Dateinamen und vollen Pfad erzeugen
			string FullFileName = string.Format("{0}\\{6}\\{1}_{2}{7}_{3}_{4}.{5}",
			                                    captureSavePath, 
			                                    captureFileNamePrefix, 
			                                    suffix == string.Empty ? "" : suffix,
			                                    DateTime.Now.ToString("yyyyMMdd-HHmmss"), 
			                                    count,
			                                    filetype == 0 ? captureImageExt : captureFileLogExt,
			                                    strDate,
			                                    pid > 0 ? "_"+pid : ""
			                                   );
			//	####
			
			if (!string.IsNullOrWhiteSpace("")) {
				
			}
			
				
			
			return FullFileName;
		}

		/// <summary>
		/// Anzeige von Dateigrößen in Lesbare Formate
		/// </summary>
		/// <param name="bytes">Wert in Byte</param>
		/// <returns>Formartierter String</returns>
		public string ToFuzzyByteString(long bytes)
        {                    
            double s = bytes; 
            string[] format = { "{0} bytes", "{0} KB", "{0} MB", "{0} GB", "{0} TB" };

            int i = 0;

            while (i < format.Length && s >= 1024)              
            {                                     
                s = (long) (100*s/1024)/100.0;  
                i++;            
            }                     
            return string.Format(format[i], s);  
        }
		
		void getScreenshotFromProcess()
		{
			if (string.IsNullOrWhiteSpace(cmdProcess) && ((modeType & cmdActionArgsModeType.NoGUI) != 0)) {
				//	Abruch wenn kein Process Name angegeben wurde und NoGui als Modus angegeben wurde
				return;
			}
			
			Process[] prz = Process.GetProcessesByName(cmdProcess);
			IntPtr przHandle;
			
			if (prz.Length > 0) {
				foreach (Process ps in prz) 
				{
					if (ps.MainWindowHandle != null) 
					{	
						if(ActivateWindow(ps.MainWindowHandle, ps.Id, out przHandle))
						{
							Debug.WriteLine(string.Format("{0} / {1}", ps.MainWindowHandle, przHandle), "getScreenshotFromProcess()");
							Thread.Sleep(1000);
							
							//	Mindestens ein Prozess wurde gefunden
							if (przHandle != IntPtr.Zero) 
							{
								if((modeType & cmdActionArgsModeType.logOnly) == 0)
								{
									ScreenCapturer.CaptureAndSave(checkFileName(Array.IndexOf(prz, ps)+1, ps.ProcessName, ps.Id)); //, przHandle, ImageFormat.Png);
								}
								saveProcessInfo2File(ps, checkFileName(Array.IndexOf(prz, ps)+1, ps.ProcessName, ps.Id,1));
							}
							else
							{
								saveProcessInfo2File(ps, checkFileName(Array.IndexOf(prz, ps)+1, ps.ProcessName, ps.Id, 1));
								File.AppendAllText(checkFileName(Array.IndexOf(prz, ps)+1, ps.ProcessName,1), "Screenshot der Anwendung konnte nicht erstellt werden");
							}
						}
						else
						{
							saveProcessInfo2File(ps, checkFileName(Array.IndexOf(prz, ps)+1, ps.ProcessName, ps.Id, 1));
							File.AppendAllText(checkFileName(Array.IndexOf(prz, ps)+1, ps.ProcessName,1), "Screenshot der Anwendung konnte nicht erstellt werden");
						}
					}
				}
			}
		}
		#endregion
		
		#region Events
		void button1_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(textBox1.Text)) {
				cmdProcess = textBox1.Text;
				getScreenshotFromProcess();
				
				modeType = cmdActionArgsModeType.GUI;
			}
		}
		
		void saveProcessInfo2File(Process ps, string fileName)
		{
			File.WriteAllText(fileName,string.Format("Prozess ID: {0}\r\n" +
				              "Fenstertitel: {1}\r\n" +
				              "Prozess Startzeit: {2}\r\n" +
				              "Prozess CPU Zeit: {3}\r\n" +
				              "aktueller Speicherverbrauch: {4}\r\n" +
				              "maximaler Speicherverbrauch: {5}\r\n" +
				              "Kommandozeilen Aufruf: {6}\r\n" +
				              "Prozess Dateiname/Pfad: {7}\r\n",
				              ps.Id.ToString(),
				              ps.MainWindowTitle,
				              ps.StartTime.ToString(),
				              ps.TotalProcessorTime.ToString(),
				              ToFuzzyByteString(ps.WorkingSet64),
				              ToFuzzyByteString(ps.PeakWorkingSet64),
				              GetCommandLine(ps),
				              ps.MainModule.FileName
				             ));
		}
		
		/// <summary>
		/// Ermitteln der Kommandozeilenparameter eines Prozesses per WMI
		/// </summary>
		/// <param name="process">Der Prozess von dem die Kommandozeile ermittelt werden soll</param>
		/// <returns>Ermittelte Kommandozeile</returns>
		string GetCommandLine(Process process)
		{
		    var commandLine = new StringBuilder(process.MainModule.ModuleName);
		
		    commandLine.Append(" ");
		    using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
		    {
		        foreach (var @object in searcher.Get())
		        {
		            commandLine.Append(@object["CommandLine"]);
		            commandLine.Append(" ");
		        }
		    }
		
		    return commandLine.ToString();
		}
		void timerCapture_Tick(object sender, EventArgs e)
		{
			timerCapture.Enabled = false;
			getScreenshotFromProcess();
			if((modeType & cmdActionArgsModeType.NoGUI) != 0)
				Application.Exit();
			
		}
		
		public bool ActivateWindow(IntPtr mainWindowHandle, int procID, out IntPtr newHandle)
		{
			uint iProcessID = 0;
			IntPtr handle = IntPtr.Zero;
			newHandle = IntPtr.Zero;
			IntPtr fgHandle = IntPtr.Zero;
			
		    //check if already has focus
		    if (mainWindowHandle == GetForegroundWindow()) 
		    	return true;
		
		    //check if window is minimized
		    if(SwitchToThisWindow(mainWindowHandle, true))
		    	Debug.WriteLine("SwitchToThisWindow erfolgreich", "ActivateWindow()");
		    else
		    	Debug.WriteLine("SwitchToThisWindow nicht erfolgreich", "ActivateWindow()");
//		    if (IsIconic(mainWindowHandle))
//		    {
//		        ShowWindow(mainWindowHandle, Restore);
//		        ShowWindow(mainWindowHandle, 9);	//SW_SHOWNORMAL, Aktivieren und anzeigen. Fenster wiederherstellen wenn minimiert
//		    }
		    
		    
		
		    // Simulate a key press
		    keybd_event((byte)ALT, 0x45, EXTENDEDKEY | 0, 0);
		
		    //SetForegroundWindow(mainWindowHandle);
		
		    // Simulate a key release
		    keybd_event((byte)ALT, 0x45, EXTENDEDKEY | KEYUP, 0);
		
		    fgHandle = SetActiveWindow(mainWindowHandle);
		    SetForegroundWindow(mainWindowHandle);
		    
		    Thread.Sleep(500);
		    
		    handle = GetForegroundWindow();
		    
		    GetWindowThreadProcessId(handle, out iProcessID);
		    
		    Debug.WriteLine(string.Format("{0} / {1} / {2}", mainWindowHandle, handle, fgHandle), "ActivateWindow()");
		    
		    if (iProcessID == procID)
		    {
		    	newHandle = handle;
//		    	 ShowWindow(handle, 9);
		    	return true;
		    }
		    
		    
		    return false;
		}
		
		void textBox1_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter) {
				button1.PerformClick();
			}
		}
		void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			
			modeType = checkBox1.Checked ? modeType | cmdActionArgsModeType.logOnly : modeType & cmdActionArgsModeType.logOnly;
		}

        #endregion
	}
}
