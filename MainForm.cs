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
        List<IItem> _Station = null;  
        public MainForm()
        {
//          string[] arNames = new string[8];
            List<IItem> lIItem = new List<IItem>();   
            _Station = new List<IItem>();   

            InitializeComponent();
            _Rdk = new RoboDK();
            _Rdk.Connect();
            var status = _Rdk.AddFile("C:\\KTIProjects\\KTIRobot\\ShelfTable.rdk");
            // var status = _Rdk.AddStation("ShelfTable.rdk");

            _Robot = _Rdk.GetItemByName("Mitsubishi RV-8CRL", RoboDk.API.Model.ItemType.Robot);
            // _Station = _Rdk.GetOpenStation();
            //lIItem = _Station.GetItemList();
            _Robot.Connect();
            if(_Robot.Valid())
                _Robot.NewLink();

            _Rdk.SetRunMode(RoboDk.API.Model.RunMode.RunRobot);
            double[] joints = _Robot.JointsHome();
            double home0 = joints[0];
            _Robot.MoveJ(joints);
            for(int i = 0; i < 6; i++)
            {
                switch(i % 2)
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

//            CloseAllStations();
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
    }
}

