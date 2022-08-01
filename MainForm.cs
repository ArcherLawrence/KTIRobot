using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoboDk.API;
using RoboDk.API.Model;
namespace KTIRobot
{
    public partial class MainForm : Form
    {
        RoboDK _Rdk = null;
        IItem _Robot = null;
        IItem _Gripper = null;
        public MainForm()
        {
            InitializeComponent();

        }
        /// <summary>
        ///     Close all the stations available in RoboDK (top level items)
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void CloseAllStations()
        {
            // Get all the RoboDK stations available
            var allStations = _Rdk.GetItemList(RoboDk.API.Model.ItemType.Station);
            foreach (var station in allStations)
            {
                //notifybar.Text = $@"Closing {station.Name()}";
                // this will close a station without asking to save:
                station.Delete();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _Rdk = new RoboDK();
            _Rdk.Connect();
          

            var status = _Rdk.AddFile("C:\\KTIProjects\\KTIRobot\\ShelfTableA.rdk");
            _Rdk.SetRunMode(RoboDk.API.Model.RunMode.RunRobot);

            _Robot = _Rdk.GetItemByName("Mitsubishi RV-8CRL", RoboDk.API.Model.ItemType.Robot);
            _Robot.Connect();
            // _Robot.SetRunType(ProgramExecutionType.RunOnRobot);

            if (_Robot.Valid())
            {
                while (_Robot.Busy())
                    _Robot.Pause(50);
                _Robot.Pause(100);
                _Robot.NewLink();
            }
            else
                return;

            _Robot.SetSpeed(50);
            _Robot.Pause(100);
            //while (_Robot.Busy())
            //    _Robot.Pause(50);
            //_Robot = _Rdk.GetItemByName("SchunkGripper", RoboDk.API.Model.ItemType.Any);

            _Robot.Pause(100);
            //while (_Robot.Busy())
            //    _Robot.Pause(50);
            double[] joints = _Robot.JointsHome();
            double home0 = joints[0];
            _Robot.Pause(500);
            while (_Robot.Busy())
                _Robot.Pause(50);
            
            _Robot.MoveJ(joints);

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
                _Robot.Pause(100);
                while (_Robot.Busy())
                    _Robot.Pause(50);
                _Robot.MoveJ(joints);
            }
            joints[0] = 160.0;
            //while (_Robot.Busy())
            //    _Robot.Pause(50);
            joints[2] = 30.0;
            _Robot.MoveJ(joints);
            _Robot.Pause(100);
            Initializer();
            while (true)
            {
                _Robot.Pause(100);

                GripOpenRdk();

                _Robot.Pause(100);
                GripCloseRdk(100);
            }

        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            double[] joints = _Robot.JointsHome();
            while (_Robot.Busy())
                _Robot.Pause(50);
            _Robot.MoveJ(joints);
            while (_Robot.Busy())
                _Robot.Pause(50);

            CloseAllStations();
            Application.Exit();
        }

        private void GripClose_Click(object sender, EventArgs e)
        {
            //GripCloseRdk();
            _Robot.RunCodeCustom("C:\\KTIProjects\\KTIRobot\\GRIPCLOSE.prg", RoboDk.API.Model.ProgramRunType.CallProgram);
        }


        private void GripOpen_Click(object sender, EventArgs e)
        {
            // GripOpenRdk();
            _Robot.RunCodeCustom("GRIPOPEN.prg", RoboDk.API.Model.ProgramRunType.CallProgram);
        }
    }
}

