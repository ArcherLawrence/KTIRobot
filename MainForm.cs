using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using RoboDk.API;
using RoboDk.API.Exceptions;
using RoboDk.API.Model;
using KTIRobot.Properties;

namespace KTIRobot
{
    public partial class MainForm : Form
    {
        // Define if the robot movements will be blocking
        private const bool MoveBlocking = false;

        // RDK holds the main object to interact with RoboDK.
        // The RoboDK application starts when a RoboDK object is created.
        private IRoboDK _rdk;

        // Keep the ROBOT item as a global variable
        private IItem _robot;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // This will create a new icon in the windows toolbar that shows how we can lock/unlock the application
            // Setup_Notification_Icon();

            // Start RoboDK here if we want to start it before the Form is displayed
            if (!Check_RDK())
            {
                // RoboDK starts here. We can optionally pass arguments to start it hidden or start it remotely on another computer provided the computer IP.
                // If RoboDK was already running it will just connect to the API. We can force a new RoboDK instance and specify a communication port
                _rdk = new RoboDK();


                // Check if RoboDK started properly
                if (Check_RDK())
                {
                    notifybar.Text = @"RoboDK is Running...";

                    // attempt to auto select the robot:
                    SelectRobot();
                }

                // other ways to Start RoboDK
                //bool START_HIDDEN = false;
                //RDK = new RoboDK("", START_HIDDEN); // default connection, starts RoboDK visible if it has not been started
                //RDK = new RoboDK("localhost", false, 20599); //start visible, use specific communication port to not interfere with other applications
                //RDK = new RoboDK("localhost", true, 20599); //start hidden,  use specific communication port to not interfere with other applications
                InitMoveRobot();
            }
        }
        private void InitMoveRobot()
        {
            outReg = 0;
            _robot.setDO(0000.ToString("X4"), outReg.ToString("X4"));
            //_robot.SetSpeed(5000);
            //while (_robot.Busy())
            //    _robot.Pause(500);

            double[] joints = _robot.JointsHome();
            double home0 = joints[0];
            //while (_robot.Busy())
            //    _robot.Pause(50);

            //_robot.Disconnect();
            //_robot.ConnectSafe();
            //while (_robot.Busy())
            //    _robot.Pause(50);
            try { _robot.MoveJ(joints); }
            catch {
                if(!Check_RDK())
                {
                    _rdk.Disconnect();
                    _rdk.Connect();
                }
                if (Check_ROBOT())
                {
                    _robot.Disconnect();
                    _robot.ConnectSafe();
                }
                while (_robot.Busy())
                    _robot.Pause(50);
                _robot.MoveJ(joints);
            }
            //wiggle

            for (int i = 0; i < 6; i++)
            {
                switch (i % 3)
                {
                    case 0:
                        joints[0] = home0 - 10;
                        break;
                    case 1:
                        joints[0] = home0;
                        break;
                    case 2:
                        joints[0] = home0 + 10;
                        break;
                }
                while (_robot.Busy())
                    _robot.Pause(50);
                try { _robot.MoveJ(joints); }
                catch
                {
                    _robot.Disconnect();
                    _robot.ConnectSafe();
                    while (_robot.Busy())
                        _robot.Pause(50);
                    _robot.MoveJ(joints);
                }
            }
            joints[0] = 160.0;
            while (_robot.Busy())
                _robot.Pause(50);
            joints[2] = 30.0;
            try { _robot.MoveJ(joints); }
            catch
            {
                _robot.Disconnect();
                _robot.ConnectSafe();
                while (_robot.Busy())
                    _robot.Pause(50);
                _robot.MoveJ(joints);
            }
            while (_robot.Busy())
                _robot.Pause(50);

        }
        /// <summary>
        ///     Check if the RDK object is ready.
        ///     Returns True if the RoboDK API is available or False if the RoboDK API is not available.
        /// </summary>
        /// <returns></returns>
        public bool Check_RDK()
        {
            // check if the RDK object has been initialized:
            if (_rdk == null)
            {
                notifybar.Text = @"RoboDK has not been started";
                return false;
            }

            // Check if the RDK API is connected
            if (!_rdk.Connected())
            {
                notifybar.Text = @"Connecting to RoboDK...";
                // Attempt to connect to the RDK API
                if (!_rdk.Connect())
                {
                    notifybar.Text = @"Problems using the RoboDK API. The RoboDK API is not available...";
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        ///     Update the ROBOT variable by choosing the robot available in the currently open station
        ///     If more than one robot is available, a popup will be displayed
        /// </summary>
        public void SelectRobot()
        {
            notifybar.Text = @"Selecting robot...";
            if (!Check_RDK())
            {
                _robot = null;
                return;
            }
            _robot = _rdk.GetItemByName("Mitsubishi RV-8CRL", ItemType.Robot);
            if ((_robot != null) || (_robot.ConnectedState() != RobotConnectionType.Ready))
            {
                var status = _rdk.AddFile("C:\\KTIProjects\\KTIRobot\\ShelfTableA.rdk");
                _robot = _rdk.GetItemByName("Mitsubishi RV-8CRL", ItemType.Robot);
            }
            //_robot.SetSpeed(10000);
            _robot.ConnectSafe();
            if (_robot.Valid())
            {
                _robot.NewLink(); // This will create a new communication link (another instance of the RoboDK API), this is useful if we are moving 2 robots at the same time.                
                notifybar.Text = $@"Using robot: {_robot.Name()}";
            }
            else
            {
                notifybar.Text = @"Mit.RV8 Robot not available";
            }
            RunRobot();
        }

        /// <summary>
        ///     Check if the ROBOT object is available and valid. It will make sure that we can operate with the ROBOT object.
        /// </summary>
        /// <returns></returns>
        public bool Check_ROBOT(bool ignoreBusyStatus = false)
        {
            if (!Check_RDK()) return false;
            if (_robot == null || !_robot.Valid())
            {
                notifybar.Text = @"A robot has not been selected. Load a station or a robot file first.";
                return false;
            }

            try
            {
                notifybar.Text = $@"Using robot: {_robot.Name()}";
            }
            catch (RdkException rdkException)
            {
                notifybar.Text = $@"The robot has been deleted: {rdkException.Message}";
                return false;
            }

            // Safe check: If we are doing non blocking movements, we can check if the robot is doing other movements with the Busy command
            // ReSharper disable once RedundantLogicalConditionalExpressionOperand
            if (!MoveBlocking && !ignoreBusyStatus && _robot.Busy())
            {
                notifybar.Text = @"The robot is busy!! Try later...";
                return false;
            }

            return true;
        }
        /// <summary>
        ///     Close all the stations available in RoboDK (top level items)
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void CloseAllStations()
        {
            // Get all the RoboDK stations available
            var allStations = _rdk.GetItemList(ItemType.Station);
            foreach (var station in allStations)
            {
                notifybar.Text = $@"Closing {station.Name()}";
                // this will close a station without asking to save:
                station.Delete();
            }
        }


        private void RunRobot()             // void rad_RunMode_Online_CheckedChanged(object sender, EventArgs e)
        {
            // Check that there is a link with RoboDK:
            if (!Check_ROBOT())
                return;

            // Important: stop any previous program generation (if we selected offline programming mode)
            _rdk.Disconnect();

            Check_RDK();        // arl
            Check_ROBOT();

            // Connect to real robot
            if (_robot.Connect() || (_robot.ConnectedState() == RobotConnectionType.Ready ))
            {
                // Set to Run on Robot robot mode:
                _rdk.SetRunMode(RunMode.RunRobot);
            }
            else
            {
                notifybar.Text = @"Can't connect to the robot. Check connection and parameters.";
            }
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            CloseAllStations();
            Application.Exit();
        }

        private void GripClose_Click(object sender, EventArgs e)
        {
            GripCloseRdk(1);
        }


        private void GripOpen_Click(object sender, EventArgs e)
        {
            GripOpenRdk();
        }

        private void PosTestBtn_Click(object sender, EventArgs e)
        {
            PickTray(3, 1);
        }
    }
}

