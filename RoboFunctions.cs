﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RoboDk.API;

namespace KTIRobot
{
    public partial class MainForm : Form
    {
        //public double[] tester1A = { 707.38, 469.52, 200.00, -179.57, -0.87, 42.95 };
        //public double[] tester1C = { 706.08, 342.82, 199.99, +179.09, 0.45, -136.54 };
        //public double[] tester2A = { 712.40, 68.40, 199.99, -179.57, -0.87, 42.95 };
        //public double[] tester2C = { 710.49, -58.00, 199.99, 179.09, 0.45, -136.88};
        //public double[] tester3A = { 716.71, -328.41, 200.00, -180.00, -0.96, -135.40 };
        //public double[] tester3C = { 715.71, -455.35, 200.00, 180.00, 0.96, 44.49 };
        //public double[] inputArr1 = { 178.02, 536.38, 163.98, -179.31, -0.71, 133.68 };
        //public double[] inputArr25 = { 417.98, 539.99, 164.00, -179.30, -0.71, 133.68 };
        //public double[] failArr1 = { 179.00, +97.00, 167.00, -179.31, -0.71, 133.68 };
        //public double[] failArr25 = { 419.00, +100.00, 167.04, -179.31, -0.71, 133.68 };
        //public double[] passArr1 = { 180.04, -344.97, 166.05, -179.32, -0.71, 133.67 };
        //public double[] passArr25 = { 419.99, -342.50, 164.66, -179.31, -0.71, 133.68 };

        public double[] tester1A = { 707.38, 469.52, 250.00, -179.57, -0.87, 42.95 };
        public double[] tester1C = { 706.08, 342.82, 149.99, +179.09, 0.45, -136.54 };
        public double[] tester2A = { 712.40, 68.40, 149.99, -179.57, -0.87, 42.95 };
        public double[] tester2C = { 710.49, -58.00, 149.99, 179.09, 0.45, -136.88 };
        public double[] tester3A = { 716.71, -328.41, 250.00, -180.00, -0.96, -135.40 };
        public double[] tester3C = { 715.71, -455.35, 250.00, 180.00, 0.96, 44.49 };
//      public double[] inputArr1 = { 178.02,  536.38,  213.98,  -179.31,  -0.71,  133.68 };         // from RT TOOLBOX
        public double[] inputArr1 = { 175.412, 539.302, 164.018, -179.302, -0.699, 132.977 };        // from RoboDK def
        public double[] inputArr25 = { 417.98, 539.99, 214.00, -179.30, -0.71, 133.68 };
        public double[] failArr1 = { 179.00, +97.00, 217.00, -179.31, -0.71, 133.68 };
        public double[] failArr25 = { 419.00, +100.00, 217.04, -179.31, -0.71, 133.68 };
        public double[] passArr1 = { 180.04, -344.97, 216.05, -179.32, -0.71, 133.67 };
        public double[] passArr25 = { 419.99, -342.50, 214.66, -179.31, -0.71, 133.68 };

        // joint position in degrees
        // Tester 1
        public double[] j1A = {+33.34,+56.84,+58.56,+0.63,+63.82,+170.11};
        public double[] j1C = {+25.48,+47.83,+74.55,+1.19,+57.49,-18.03};

        // Tester 2
        public double[] j2A = {+5.10,+39.04,+89.49,+1.12,+51.05,+142.00};
        public double[] j2C = {-5.05,+39.21,+89.21,+1.20,+51.96,-48.12};

        // Fail tray
        public double[] jFail1 = {+28.03,-18.02,+163.07,+0.85,+35.81,+74.35};
        public double[] jFail25 = {+13.53,+12.74,+134.14,+0.47,+34.08,+60.14};

        // input tray
        public double[] jInput1 = {+71.44,+26.05,+115.41,+1.51,+38.83,+117.28};
        public double[] jInput25 = {+52.18,+38.77,+94.86,+1.10,+46.96,+98.44};

        // Pass tray
        public double[] jPass1 = {-61.06,+7.19,+141.05,-1.56,+32.29,-13.58};
        public double[] jPass25 = {-38.21,+23.44,+119.21,-0.93,+38.15,+9.55};

        public int outReg = 0;
        public void SetBit(int sigNum)
        {
            if ((sigNum >= 0) && (sigNum < 16))
            {
                outReg |= (1 << sigNum);
                // int outVal = outReg >> sigNum;
                //_robot.setDO(sigNum.ToString("X4"), outVal.ToString("X4"));
                //_robot.setDO("0000", outReg.ToString("X4"));
                _robot.SetParam("Driver", "SETDO 0000 " + outReg.ToString("X4"));
            }
        }
        public void ClrBit(int sigNum)
        {
            if ((sigNum >= 0) && (sigNum < 16))
            {
                outReg &= ~(1 << sigNum);
                // _robot.setDO("0000", outReg.ToString("X4"));
                _robot.SetParam("Driver", "SETDO 0000 " + outReg.ToString("X4"));
            }
        }
        public enum eLED_COLOR { eLED_RED, eLED_GREEN, eLED_YELLOW, eLED_NONE };

        public void SetLED(eLED_COLOR eColor)
        {
            switch (eColor)
            {
                case eLED_COLOR.eLED_RED:
                    SetBit(10);             // _robot.setDO("10", "1");
                    _robot.Pause(30);
                    SetBit(11);             // _robot.setDO("11", "1");
                    break;
                case eLED_COLOR.eLED_NONE:
                    ClrBit(10);
                    _robot.Pause(30);
                    ClrBit(11);
                    break;
                case eLED_COLOR.eLED_YELLOW:
                    SetBit(10);         // _robot.setDO("11", "0");
                    _robot.Pause(30);
                    ClrBit(11);         // _robot.setDO("10", "1");
                    break;
                case eLED_COLOR.eLED_GREEN:
                    ClrBit(10);         // _robot.setDO("10", "0");
                    _robot.Pause(30);
                    SetBit(11);         // _robot.setDO("11", "1");
                    break;
            }
            return;
        }

        void GripOpenRdk()
        {
            ClrBit(15);         // _robot.setDO("15", "0");
            _robot.Pause(150);

            ClrBit(14);         // _robot.setDO("14", "0");
            _robot.Pause(150);

            SetBit(14);         // _robot.setDO("14", "1");   // close
            _robot.Pause(150);

            SetLED(eLED_COLOR.eLED_NONE);
            return;
        }
        bool GripCloseRdk(int retry = 1)
        {
            double[] joints = _robot.Joints();

            _robot.setDO("15", "0");
            _robot.Pause(50);

            _robot.setDO("14", "0");
            _robot.Pause(50);

            _robot.setDO("15", "1");   // close
            _robot.Pause(50);

            ClrBit(15);             // _robot.setDO("15", "0");
            _robot.Pause(50);
            SetBit(14);             // _robot.setDO("14", "1");
            _robot.Pause(50);
            ClrBit(14);             // _robot.setDO("14", "0");
            _robot.Pause(50);
            SetBit(15);             // _robot.setDO("15", "1");
            _robot.WaitMove();
            
            // string closeSensor = _robot.SetParam("Driver", "GET $IN[12]");
            string closeSensor = _robot.SetParam("Driver", "GETDI 12");
            while (string.IsNullOrEmpty(closeSensor))
            {
                Thread.Sleep(100);
                closeSensor = _robot.SetParam("Driver", "GETDI 12");
            }

            _robot.WaitMove();
            while (_robot.Busy())
                _robot.Pause(100);

            // string openSensor = _robot.SetParam("Driver", "GET $IN[13]");
            string openSensor = _robot.SetParam("Driver", "GETDI 13");
            while (string.IsNullOrEmpty(openSensor))
            {
                Thread.Sleep(100);
                openSensor = _robot.SetParam("Driver", "GETDI 13");
            }


            int close = -1;
            int open = -1;
            if (!Int32.TryParse(closeSensor, out close))
            {
                _robot.Pause(500);
                closeSensor = _robot.SetParam("Driver", "GETDI 12");
                Int32.TryParse(closeSensor, out close);
            }
            if (!Int32.TryParse(openSensor, out open))
            {
                _robot.Pause(500);
                openSensor = _robot.SetParam("Driver", "GETDI 13");
                Int32.TryParse(openSensor, out open);
            }
            close &= 1;
            open &= 1;
            if (close == 1)
            {
                //Grip failed, set LED to red and return false
                SetLED(eLED_COLOR.eLED_RED);
                return false;
            }
            else
            {
                if (open == 1)
                {
                    //This case should not be possible, but if we end up here, set LED to yellow and return false
                    SetLED(eLED_COLOR.eLED_YELLOW);
                    return false;
                }
                else
                {
                    // not open and not closed -- so we have a module
                    SetLED(eLED_COLOR.eLED_GREEN);
                    return true;
                }
            }
        }
        int PickTray(int target, int input = 1)
        {
            if (input > 50)
                return -1; //input exceeds maximum tray size, return -1 so we know tray is empty

            // open gripped
            GripOpenRdk();

            Mat mPos1 = _robot.SolveFK(jInput1);
            Mat mPos2 = Mat.transl(0.0, 0.0, 100.0) * mPos1;
            Mat mPos3 = Mat.transl(0.0, 0.0, 200.0) * mPos1;
            Mat mPos4 = _robot.SolveFK(jInput25);

            double[] ik1 = _robot.SolveIK(mPos1);
            double[] ik2 = _robot.SolveIK(mPos2);
            double[] ik3 = _robot.SolveIK(mPos3);

            double[] xyz1 = mPos1.Pos();
            double[] xyz4 = mPos4.Pos();

            _robot.MoveJ(ik3);
            _robot.MoveJ(ik2);
            _robot.MoveJ(ik1);

            //Full implementation of point arrays needed, using initially grabbed values for now.
            double[] points = { 0, 0, inputArr1[2], inputArr1[3], inputArr1[4], inputArr1[5] };

            double yskew = (inputArr25[1] - inputArr1[1]) / 25; //Compensation for slight skew in the axis
            if (input > 25)
            {
                points[0] = inputArr1[0] + 10 * (input - 26);
                points[1] = inputArr1[1] - 166 + yskew * (input - 26);
            }
            else
            {
                points[0] = inputArr1[0] + 10 * (input - 1);
                points[1] = inputArr1[1] + yskew * (input - 1);
            }
            double[] initApproach = { points[0], points[1], points[2] + 200, points[3], points[4], points[5] };
            double[] finApproach = { points[0], points[1], points[2] + 30, points[3], points[4], points[5] };

            switch (target)
            {
                case 1:     // case input tray
                    {
//                        double[] tmp = { 0, 0, inputArr1[2], inputArr1[5], inputArr1[4], inputArr1[3] };
                        double[] tmp = { 0, 0, inputArr1[2], inputArr1[3], inputArr1[4], inputArr1[5] };
                        for (int i = 0; i < points.Length; i++)
                            points[i] = tmp[i]; // Changed from points[1] = tmp[i]; Assuming we want to assign each of points[] to match the corresponding tmp[]

                        yskew = (inputArr25[1] - inputArr1[1]) / 25; //Compensation for slight skew in the axis

                        if (input > 25)
                        {
                            points[0] = inputArr1[0] + 10 * (input - 26);
                            points[1] = inputArr1[1] - 166 + yskew * (input - 26);
                        }
                        else
                        {
                            points[0] = inputArr1[0] + 10 * (input - 1);
                            points[1] = inputArr1[1] + yskew * (input - 1);
                        }
                    }
                    break;

                case 2:     // fail tray
                    {
                        double[] tmp = { 0, 0, failArr1[2], failArr1[3], failArr1[4], failArr1[5] };
                        for (int i = 0; i < points.Length; i++)
                            points[i] = tmp[i];

                        yskew = (failArr25[1] - failArr1[1]) / 25; //Compensation for slight skew in the axis

                        if (input > 25)
                        {
                            points[0] = failArr1[0] + 10 * (input - 26);
                            points[1] = failArr1[1] - 166 + yskew * (input - 26);
                        }
                        else
                        {
                            points[0] = failArr1[0] + 10 * (input - 1);
                            points[1] = failArr1[1] + yskew * (input - 1);
                        }
                    }
                    break;
                case 3:     // pass tray
                    {
                        double[] tmp = { 0, 0, passArr1[2], passArr1[3], passArr1[4], passArr1[5] };
                        for (int i = 0; i < points.Length; i++)
                            points[i] = tmp[i];

                        yskew = (passArr25[1] - passArr1[1]) / 25; //Compensation for slight skew in the axis

                        if (input > 25)
                        {
                            points[0] = passArr1[0] + 10 * (input - 26);
                            points[1] = passArr1[1] - 166 + yskew * (input - 26);
                        }
                        else
                        {
                            points[0] = passArr1[0] + 10 * (input - 1);
                            points[1] = passArr1[1] + yskew * (input - 1);
                        }
                    }
                    break;
            }
            double[] tmp1 = { points[0], points[1], points[2] + 200, points[3], points[4], points[5] };
            for (int ix = 0; ix < initApproach.Length; ix++)
                initApproach[ix] = tmp1[ix];

            double[] tmp2 = { points[0], points[1], points[2] + 30, points[3], points[4], points[5] };
            for (int ij = 0; ij < finApproach.Length; ij++)
                finApproach[ij] = tmp2[ij];

            // convert to pose
            Mat initPose = Mat.FromTxyzRxyz(initApproach);
            Mat finPose = Mat.FromTxyzRxyz(finApproach);
            Mat inputPose = Mat.FromTxyzRxyz(points);
            //Possibly wrong translation using Txyz, try Fromxyzrpw instead
            //Mat initPose = Mat.FromXYZRPW(initApproach);
            //Mat finPose = Mat.FromXYZRPW(finApproach);
            //Mat inputPose = Mat.FromXYZRPW(points);

            // move to pose
            _robot.MoveJ(initPose);
            _robot.MoveL(finPose);

            // slow linear plunge
            _robot.SetSpeed(30);
            _robot.MoveL(inputPose);

            // Close gripper
            bool isGripped = GripCloseRdk();

            // slow line ar move up
            _robot.MoveL(finPose);

            // fast linear move
            _robot.SetSpeed(10000);
            _robot.MoveL(initPose);

            // check if we got something in the gripper
            if (isGripped)
                return input; //return where we found a module
            else
                return PickTray(target, input + 1); // no module in cuurent location, try next slot

        }
        /////////////////////////////////////////////////////////////////////////////////////
        /// place pass -- place into a module into a tray
        void PlaceTray(int target, int index = 1)
        {
            if (index > 50)
                return; //input exceeds maximum tray size, return -1 so we know tray is empty

            // module to alreadt gripped

            //Full implementation of point arrays needed, using initially grabbed values for now.
            double[] points = { 0, 0, inputArr1[2], inputArr1[3], inputArr1[4], inputArr1[5] };

            double yskew = (inputArr25[1] - inputArr1[1]) / 25; //Compensation for slight skew in the axis
            if (index > 25)
            {
                points[0] = inputArr1[0] + 10 * (index - 26);
                points[1] = inputArr1[1] - 166 + yskew * (index - 26);
            }
            else
            {
                points[0] = inputArr1[0] + 10 * (index - 1);
                points[1] = inputArr1[1] + yskew * (index - 1);
            }
            double[] initApproach = { points[0], points[1], points[2] + 200, points[3], points[4], points[5] };
            double[] finApproach = { points[0], points[1], points[2] + 30, points[3], points[4], points[5] };

            switch (target)
            {
                case 1:     // case input tray
                    {
                        double[] tmp = { 0, 0, inputArr1[2], inputArr1[3], inputArr1[4], inputArr1[5] };
                        for (int i = 0; i < points.Length; i++)
                            points[1] = tmp[i];

                        yskew = (inputArr25[1] - inputArr1[1]) / 25; //Compensation for slight skew in the axis

                        if (index > 25)
                        {
                            points[0] = inputArr1[0] + 10 * (index - 26);
                            points[1] = inputArr1[1] - 166 + yskew * (index - 26);
                        }
                        else
                        {
                            points[0] = inputArr1[0] + 10 * (index - 1);
                            points[1] = inputArr1[1] + yskew * (index - 1);
                        }
                    }
                    break;

                case 2:     // fail tray
                    {
                        double[] tmp = { 0, 0, failArr1[2], failArr1[3], failArr1[4], failArr1[5] };
                        for (int i = 0; i < points.Length; i++)
                            points[1] = tmp[i];

                        yskew = (failArr25[1] - failArr1[1]) / 25; //Compensation for slight skew in the axis

                        if (index > 25)
                        {
                            points[0] = failArr1[0] + 10 * (index - 26);
                            points[1] = failArr1[1] - 166 + yskew * (index - 26);
                        }
                        else
                        {
                            points[0] = failArr1[0] + 10 * (index - 1);
                            points[1] = failArr1[1] + yskew * (index - 1);
                        }
                    }
                    break;
                case 3:     // pass tray
                    {
                        double[] tmp = { 0, 0, passArr1[2], passArr1[3], passArr1[4], passArr1[5] };
                        for (int i = 0; i < points.Length; i++)
                            points[1] = tmp[i];

                        yskew = (passArr25[1] - passArr1[1]) / 25; //Compensation for slight skew in the axis

                        if (index > 25)
                        {
                            points[0] = passArr1[0] + 10 * (index - 26);
                            points[1] = passArr1[1] - 166 + yskew * (index - 26);
                        }
                        else
                        {
                            points[0] = passArr1[0] + 10 * (index - 1);
                            points[1] = passArr1[1] + yskew * (index - 1);
                        }
                    }
                    break;
            }
            double[] tmp1 = { points[0], points[1], points[2] + 200, points[3], points[4], points[5] };
            for (int ix = 0; ix < initApproach.Length; ix++)
                initApproach[ix] = tmp1[ix];

            double[] tmp2 = { points[0], points[1], points[2] + 30, points[3], points[4], points[5] };
            for (int ij = 0; ij < finApproach.Length; ij++)
                finApproach[ij] = tmp2[ij];

            // convert to pose
            Mat initPose = Mat.FromTxyzRxyz(initApproach);
            Mat finPose = Mat.FromTxyzRxyz(finApproach);
            Mat inputPose = Mat.FromTxyzRxyz(points);

            // move to pose
            _robot.MoveJ(initPose);
            _robot.MoveL(finPose);

            // slow linear plunge
            _robot.SetSpeed(30);
            _robot.MoveL(inputPose);

            // Open gripper
            GripOpenRdk();

            // slow line ar move up
            _robot.MoveL(finPose);

            // fast linear move
            _robot.SetSpeed(10000);
            _robot.MoveL(initPose);

            return;

        }

        void PlaceTester(int testerNumber, int siteNumber)
        {
            double[] testerPoint = { 0, 0, 0, 0, 0, 0 }; //Create a blank xyzprw array
            switch (testerNumber)
            {
                case 3:
                    if (siteNumber > 2)
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
            if (siteNumber % 2 == 0)
            {
                testerPoint[1] = testerPoint[1] - 25;
            }
            double[] initApproach = { testerPoint[0], testerPoint[1], testerPoint[2] + 200, testerPoint[3], testerPoint[4], testerPoint[5] };
            double[] finApproach = { testerPoint[0], testerPoint[1], testerPoint[2] + 30, testerPoint[3], testerPoint[4], testerPoint[5] };

            // convert to poses
            Mat initPose = Mat.FromTxyzRxyz(initApproach);
            Mat finPose = Mat.FromTxyzRxyz(finApproach);
            Mat testerPose = Mat.FromTxyzRxyz(testerPoint);

            // move to the tester
            _robot.MoveJ(initPose);
            _robot.MoveL(finPose);
            _robot.SetSpeed(30);
            _robot.MoveL(testerPose);

            // open gripp
            GripOpenRdk();
            _robot.SetSpeed(10000);
            _robot.MoveL(initPose);

        }
        bool PickTester(int testerNumber, int siteNumber)
        {
            // open gripp
            GripOpenRdk();

            //see place tester for calculating points. Adjust to close gripper and ensure we picked up a module
            double[] testerPoint = { 0, 0, 0, 0, 0, 0 }; //Create a blank xyzprw array
            switch (testerNumber)
            {
                case 3:
                    if (siteNumber > 2)
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
                default:   // case 1
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
            if (siteNumber % 2 == 0)
            {
                testerPoint[1] = testerPoint[1] - 25;
            }
            double[] initApproach = { testerPoint[0], testerPoint[1], testerPoint[2] + 200, testerPoint[3], testerPoint[4], testerPoint[5] };
            double[] finApproach = { testerPoint[0], testerPoint[1], testerPoint[2] + 30, testerPoint[3], testerPoint[4], testerPoint[5] };

            // convert to poses
            Mat initPose = Mat.FromTxyzRxyz(initApproach);
            Mat finPose = Mat.FromTxyzRxyz(finApproach);
            Mat testerPose = Mat.FromTxyzRxyz(testerPoint);

            // move to the tester
            _robot.MoveJ(initPose);
            _robot.MoveL(finPose);
            _robot.SetSpeed(30);
            _robot.MoveL(testerPose);

            // close gripper
            bool retVal = GripCloseRdk();
            _robot.MoveL(finPose);
            _robot.SetSpeed(10000);
            _robot.MoveL(initPose);

            return retVal;
        }
    }
}
