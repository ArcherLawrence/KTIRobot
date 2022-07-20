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

namespace KTIRobot
{
    public partial class MainForm : Form
    {
        RoboDK _Rdk = null;
        IItem _Robot = null;
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
            var status = _Rdk.AddFile("C:\\KTIProjects\\KTIRobot\\ShelfTable.rdk");

            _Robot = _Rdk.GetItemByName("Mitsubishi RV-8CRL", RoboDk.API.Model.ItemType.Robot);
            _Robot.Connect();
            if (_Robot.Valid())
                _Robot.NewLink();

            _Rdk.SetRunMode(RoboDk.API.Model.RunMode.RunRobot);
            double[] joints = _Robot.JointsHome();
            double home0 = joints[0];
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
                _Robot.MoveJ(joints);
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseAllStations();
        }
    }
}

