using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using PixelMagic.GUI;

namespace FishBot
{
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public partial class MainWindow : Form
    {
        private static Hook wowHook;
        private static Lua lua;

        private Process _process;

        public Process process
        {
            get
            {
                return _process;
            }
            set
            {
                _process = value;
                Log.Write("Process Id = " + value.Id);
            }
        }
        
        private static List<ulong> lastBobberGuid;

        private readonly int LocalVersion = int.Parse(Application.ProductVersion.Split('.')[0]);
        private int Caught;
        private IntPtr FirstObj;
        private bool Fish;

        public MainWindow()
        {
            InitializeComponent();
        }

        private static bool IsFishing
        {
            get
            {
                lua.DoString("spellData = UnitChannelInfo('player')");
                string spell = lua.GetLocalizedText("spellData");

                if (spell.Length == 0)
                    return false;
                return true;
            }
        }

        private static string Exe_Version => File.GetLastWriteTime(Assembly.GetEntryAssembly().Location).ToString("yyyy.MM.dd");

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = string.Format(toolStripStatusLabel1.Text, Exe_Version);
            toolStripStatusLabel3.Text = string.Format(toolStripStatusLabel3.Text, LocalVersion);

            Log.Initialize(LogTextBox, this);

            Shown += MainWindow_Shown;
            FormClosing += MainWindow_FormClosing;
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            try
            {
                var frmSelect = new SelectWoWProcessToAttachTo(this);
                frmSelect.ShowDialog();

                if (process == null)
                {
                    Close();
                }

                Log.Write("Attempting to connect to running WoW.exe process...", Color.Black);
                
                wowHook = new Hook(process);
                wowHook.InstallHook();
                lua = new Lua(wowHook);

                Log.Write("Connected to process with ID = " + process?.Id, Color.Black);
                
                Log.Write("Click 'Fish' to begin fishing.", Color.Green);
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, Color.Red);
            }
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            wowHook?.DisposeHooking();
        }

        private void cmdFish_Click(object sender, EventArgs e)
        {
            textBox1.Text = wowHook.Memory.ReadString(Offsets.PlayerName, Encoding.UTF8, 512, true);

            Log.Write("Base Address = " + wowHook.Process.BaseOffset().ToString("X"));

            Log.Write("Target GUID = " + wowHook.Memory.Read<ulong>(Offsets.TargetGUID, true));

            var objMgr = wowHook.Memory.Read<IntPtr>(Offsets.CurMgrPointer, true);
            var curObj = wowHook.Memory.Read<IntPtr>(IntPtr.Add(objMgr, (int)Offsets.FirstObjectOffset));

            FirstObj = curObj;

            Log.Write("First object located @ memory location 0x" + FirstObj.ToString("X"), Color.Black);

            lastBobberGuid = new List<ulong>();

            cmdStop.Enabled = true;
            cmdFish.Enabled = false;

            SystemSounds.Asterisk.Play();

            Fish = !Fish;

            while (Fish)
            {
                try
                {
                    Application.DoEvents();

                    if (!IsFishing)
                    {
                        Log.Write("Fishing...", Color.Black);
                        lua.CastSpellByName("Fishing");
                        Thread.Sleep(200); // Give the lure a chance to be placed in the water before we start scanning for it
                                           // 200 ms is a good length, most people play with under that latency
                    }

                    curObj = FirstObj;

                    while (curObj.ToInt64() != 0 && (curObj.ToInt64() & 1) == 0)
                    {
                        var type = wowHook.Memory.Read<int>(curObj + Offsets.Type);
                        var cGUID = wowHook.Memory.Read<ulong>(curObj + Offsets.LocalGUID);

                        //if (cGUID == )

                        if (lastBobberGuid.Count == 5) // Only keep the last 5 bobber GUID's (explained below * )
                        {
                            lastBobberGuid.RemoveAt(0);
                            lastBobberGuid.TrimExcess();
                        }

                        if ((type == 5) && !lastBobberGuid.Contains(cGUID)) // 5 = Game Object, and ensure that we not finding a bobber we already clicked
                        {
                            // * wow likes leaving the old bobbers in the game world for a while
                            var objectName = wowHook.Memory.ReadString(wowHook.Memory.Read<IntPtr>(wowHook.Memory.Read<IntPtr>(curObj + Offsets.ObjectName1) + Offsets.ObjectName2),
                                Encoding.UTF8, 50);

                            if (objectName == "Fishing Bobber")
                            {
                                var bobberState = wowHook.Memory.Read<byte>(curObj + Offsets.BobberState);

                                if (bobberState == 1) // Fish has been caught
                                {
                                    Caught++;
                                    textBox2.Text = Caught.ToString();

                                    Log.Write("Caught something, hopefully a fish!", Color.Black);

                                    wowHook.Memory.Write(Offsets.MouseOverGUID, cGUID);
                                    Thread.Sleep(100);

                                    //lua.DoString(string.Format("InteractUnit('mouseover')"));
                                    lua.OnRightClickObject((uint) curObj, 1);

                                    lastBobberGuid.Add(cGUID);
                                    Thread.Sleep(200);

                                    break;
                                }
                            }
                        }

                        var nextObj = wowHook.Memory.Read<IntPtr>(IntPtr.Add(curObj, (int) Offsets.NextObjectOffset));
                        if (nextObj == curObj)
                            break;
                        curObj = nextObj;
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(ex.Message, Color.Red);
                }
            }
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            cmdStop.Enabled = false;
            cmdFish.Enabled = true;

            SystemSounds.Asterisk.Play();
            Fish = false;
        }

        private void cmdLogin_Click(object sender, EventArgs e)
        {
            lua.DoString("DefaultServerLogin('winifix', '0b10ne')");
        }

        private void cmdDance_Click(object sender, EventArgs e)
        {
            lua.DoString("DoEmote('Dance')");
        }

        private static IEnumerable<Object> Objects
        {
            get
            {
                var Manager = wowHook.Memory.Read<IntPtr>(Offsets.CurMgrPointer, true);

                for (var baseAddress = wowHook.Memory.Read<IntPtr>(Manager + Offsets.FirstObjectOffset);
                    !Equals(baseAddress, IntPtr.Zero) && (baseAddress.ToInt64() & 1) == 0;
                    baseAddress = wowHook.Memory.Read<IntPtr>(baseAddress + Offsets.NextObjectOffset))
                {
                    yield return new Object(wowHook, baseAddress);
                }
            }
        }

        private static IntPtr PlayerPtr => wowHook.Memory.Read<IntPtr>(wowHook.Memory.Read<IntPtr>(wowHook.Memory.Read<IntPtr>(new IntPtr(0xC7BCD4)) + 136) + 40);
            
        private void cmdGetObjects_Click(object sender, EventArgs e)
        {
            Log.Clear();

            textBox1.Text = wowHook.Memory.ReadString(Offsets.PlayerName, Encoding.UTF8, 512, true);
            Log.Write("Player Ptr: " + PlayerPtr.ToString("X"));

            //Log.Write(wowHook.Memory.Read<IntPtr>(Offsets.PlayerBase).ToString("X"));
            var Player = new Object(wowHook, PlayerPtr);
            Log.Write("Player GUID: " + Player.GUID.ToString("X"));
            Log.Write("Type: " + Player.Type);
            Log.Write("Player X: " + Player.x);
            Log.Write("Player Y: " + Player.y);
            Log.Write("Player Z: " + Player.z);
            
            DataTable dt = new DataTable();
            dt.Columns.Add("BaseAddress");
            dt.Columns.Add("Type");
            dt.Columns.Add("GUID");
            dt.Columns.Add("X");
            dt.Columns.Add("Y");
            dt.Columns.Add("Z");
            dgv.DataSource = dt;
            
            foreach (var o in Objects.Where(x => x.Type != ConstantEnums.WoWObjectType.Corpse && x.Type != ConstantEnums.WoWObjectType.Item))
            {
                var drNew = dt.NewRow();
                drNew["BaseAddress"] = o.BaseAddress.ToString("X");
                drNew["Type"] = o.Type.ToString();
                drNew["GUID"] = o.GUID.ToString("X");
                drNew["X"] = o.x;
                drNew["Y"] = o.y;
                drNew["Z"] = o.z;
                dt.Rows.Add(drNew);
            }
        }

        private void cmdZone_Click(object sender, EventArgs e)
        {
            lua.DoString("zoneData = GetZoneText()");
            Log.Write("Zone: " + lua.GetLocalizedText("zoneData"), Color.Black);
        }
    }
}