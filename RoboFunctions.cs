using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoboDk.API;

namespace KTIRobot
{
    public partial class MainForm : Form
    {
        public double[] tester1A = { 707.38, 469.52, 200.00, -179.57, -0.87, 42.95 };
        public double[] tester1C = { 706.08, 342.82, 199.99, +179.09, 0.45, -136.54 };
        public double[] tester2A = { 712.40, 68.40, 199.99, -179.57, -0.87, 42.95 };
        public double[] tester2C = { 710.49, -58.00, 199.99, 179.09, 0.45, -136.88};
        public double[] tester3A = { 716.71, -328.41, 200.00, -180.00, -0.96, -135.40 };
        public double[] tester3C = { 715.71, -455.35, 200.00, 180.00, 0.96, 44.49 };
        public double[] inputArr1 = { 178.02, 536.38, 163.98, -179.31, -0.71, 133.68 };
        public double[] inputArr25 = { 417.98, 539.99, 164.00, -179.30, -0.71, 133.68 };
        public double[] failArr1 = { 179.00, +97.00, 167.00, -179.31, -0.71, 133.68 };
        public double[] failArr25 = { 419.00, +100.00, 167.04, -179.31, -0.71, 133.68 };
        public double[] passArr1 = { 180.04, -344.97, 166.05, -179.32, -0.71, 133.67 };
        public double[] passArr25 = { 419.99, -342.50, 164.66, -179.31, -0.71, 133.68 };

        // retrieve the reference frame and the tool frame (TCP) as poses
        Mat frame = null;
        Mat tool = null;
        void Initializer()
        {
            frame = _Robot.PoseFrame();
            tool = _Robot.PoseTool();
        }
        void GripOpen()
        {
            _Robot.setDO("15", "0");
            _Robot.Pause(30);
            _Robot.setDO("14", "1");
            _Robot.Pause(30);
            _Robot.setDO("10", "0");
            _Robot.Pause(30);
            _Robot.setDO("11", "0");
        }
        bool GripClose(int retry =1)
        {
            /* Double Pulse
            _Robot.setDO("14", "0");
            _Robot.Pause(30);
            _Robot.setDO("15", "1");
            _Robot.Pause(250);
            _Robot.setDO("15", "0");
            _Robot.Pause(30);
            _Robot.setDO("14", "1");
            _Robot.Pause(100);
            */
            _Robot.setDO("14", "0");
            _Robot.Pause(30);
            _Robot.setDO("15", "1");
            _Robot.Pause(250);
            string closeSensor = _Robot.SetParam("Driver", "GET $IN[12]");
            string openSensor = _Robot.SetParam("Driver", "GET $IN[13]");
            if (closeSensor.Equals("1"))
            {
                if (retry == 1) // First attempt failed, retry once
                    GripClose(0);
                else
                {
                    //Grip failed, set LED to red and return false
                    _Robot.setDO("11", "1");
                    _Robot.Pause(30);
                    _Robot.setDO("10", "1");
                    return false;
                }
            }
            else
            {
                if (openSensor.Equals("1"))
                {
                    //This case should not be possible, but if we end up here, set LED to yellow and return false
                    _Robot.setDO("10", "1");
                    _Robot.Pause(30);
                    _Robot.setDO("11", "0");
                    return false;
                }
                else
                {
                    _Robot.setDO("10", "0");
                    _Robot.Pause(30);
                    _Robot.setDO("11", "1");
                    return true;
                }
            }
            return false;
        }
        int PickInput(int input = 1)
        {
            if (input > 50)
                return -1; //input exceeds maximum tray size, return -1 so we know tray is empty
            GripOpen();
            //Full implementation of point arrays needed, using initially grabbed values for now.
            double[] inputPoint = { 0, 0, inputArr1[2], inputArr1[3], inputArr1[4], inputArr1[5] };
            double yskew = (inputArr25[1]-inputArr1[1])/25; //Compensation for slight skew in the axis
            if(input >25)
            {
              inputPoint[0] = inputArr1[0] + 10 * (input - 26);
              inputPoint[1] = inputArr1[1] - 166 + yskew * (input - 26);
            }
            else
            {
                inputPoint[0] = inputArr1[0] + 10 * (input - 1);
                inputPoint[1] = inputArr1[1] - 166 + yskew * (input - 1);
            }
            double[] initApproach = { inputPoint[0], inputPoint[1], inputPoint[2] + 200, inputPoint[3], inputPoint[4], inputPoint[5] };
            double[] finApproach = { inputPoint[0], inputPoint[1], inputPoint[2] + 30, inputPoint[3], inputPoint[4], inputPoint[5] }; ;
            Mat initPose = Mat.FromTxyzRxyz(initApproach);
            Mat finPose = Mat.FromTxyzRxyz(finApproach);
            Mat inputPose = Mat.FromTxyzRxyz(inputPoint);
            _Robot.MoveJ(initPose);
            _Robot.MoveL(finPose);
            _Robot.SetSpeed(30);
            _Robot.MoveL(inputPose);
            bool isGripped = GripClose();
            _Robot.MoveL(finPose);
            _Robot.SetSpeed(10000);
            _Robot.MoveL(initPose);
            if (isGripped)
                return input; //return where we found a module
            else
                return PickInput(input + 1); // no module in cuurent location, try next slot
        }
        void PlacePass(int input = 1)
        {
            //if PickInput is correct, do the same calculations with pass points, approach, then open gripper and leave
        }
        void PlaceFail(int input = 1)
        {
            //see PlacePass
        }
        void PlaceTester(int testerNumber, int siteNumber)
        {
            double[] testerPoint = { 0, 0, 0, 0, 0, 0 }; //Create a blank xyzprw array
            switch (testerNumber)
            {
                case 3:
                    if (siteNumber >2)
                    {
                        testerPoint = tester3C;
                    }
                    else
                    {
                        testerPoint = tester3A;
                    }
                    break;
                case 2:
                    if (siteNumber > 2)
                    {
                        testerPoint = tester2C;
                    }
                    else
                    {
                        testerPoint = tester2A;
                    }
                    break;
                default:
                    if (siteNumber > 2)
                    {
                        testerPoint = tester1C;
                    }
                    else
                    {
                        testerPoint = tester1A;
                    }
                    break;
            }
            if (siteNumber%2 == 0)
            {
                testerPoint[1] = testerPoint[1]-25;
            }
            double[] initApproach = {testerPoint[0], testerPoint[1], testerPoint[2]+200, testerPoint[3], testerPoint[4], testerPoint[5]};
            double[] finApproach = {testerPoint[0], testerPoint[1], testerPoint[2]+30, testerPoint[3], testerPoint[4], testerPoint[5]};
            Mat initPose = Mat.FromTxyzRxyz(initApproach);
            Mat finPose = Mat.FromTxyzRxyz(finApproach);
            Mat testerPose = Mat.FromTxyzRxyz(testerPoint);
            _Robot.MoveJ(initPose);
            _Robot.MoveL(finPose);
            _Robot.SetSpeed(30);
            _Robot.MoveL(testerPose);
            GripOpen();
            _Robot.SetSpeed(10000);
            _Robot.MoveL(initPose);
            
        }
        bool PickTester(int testerNumber, int siteNumber)
        {
            //see place tester for calculating points. Adjust to close gripper and ensure we picked up a module
            return false;
        }
    }
}
