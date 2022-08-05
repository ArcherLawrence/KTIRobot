# This is a Python module that allows driving a Mitsubishi robot.
# This Python module can be run directly in console mode to test its functionality.
# This module allows communicating with a robot through the command line.
# The same commands we can input manually are used by RoboDK to drive the robot from the PC.
# RoboDK Drivers are located in /RoboDK/api/Robot/ by default. Drivers can be PY files or EXE files.
#
# Drivers are modular. They are not part of the RoboDK executable but they must be placed in C:/RoboDK/api/robot/, then, linked in the Connection parameters menu:
#   1. right click a robot in RoboDK, then, select "Connect to robot".
#   2. In the "More options" menu it is possible to update the location and name of the driver.
# Driver linking is automatic for currently available drivers.
#
# Alternatively to the standard programming methods (where a program is generated, then, transferred to the robot and executed) it is possible to run a program simulation directly on the robot
# The robot movement in the simulator is then synchronized with the real robot.
# Programs generated from RoboDK can be run on the robot by right clicking the program, then selecting "Run on robot".
#   Example:
#   https://www.youtube.com/watch?v=pCD--kokh4s
#
# Example of an online programming project:
#   https://robodk.com/blog/online-programming/
#
# It is possible to control the movement of a robot from the RoboDK API (for example, from a Python or C# program using the RoboDK API).
# The same code is used to simulate and optionally move the real robot.
#   Example:
#   https://robodk.com/offline-programming
#
#   To establish connection from RoboDK API:
#   https://robodk.com/doc/en/PythonAPI/robolink.html#robolinCONk.Item.ConnectSafe
#
# Example of a quick manual test in console mode:
#  User entry: CONNECT 192.168.123.1
#  Response:   SMS:Response from the robot or failure to connect
#  Response:   SMS:Ready 
#  User entry: MOVJ 10 20 30 40 50 60
#  Response:   SMS:Working...
#  Response:   SMS:Ready
#  User entry: CJNT
#  Response:   SMS:Working...
#  Response:   JNTS: 10 20 30 40 50 60
#
#---------------------------------------------------------------------------------

import sys
import time
import socket
import threading

try:
   import queue
except ImportError:
    # Python2
   import Queue as queue

global MOV_LAST_J
MOV_LAST_J = None
   
def joints_error(j1, j2):
    if j1 is None or j2 is None:
        return 1e6
        
    if type(j2) is list and type(j2[0]) is str:
        j2 = [float(x) for x in j2]

    error = -1
    nj = min(len(j1), len(j2))
    for i in range(nj):
        error = max(error, abs(j1[i]-j2[i]))

    return error
   
#----------- communication class for the Mitsubishi robot -------------
# This class handles communication between this driver (PC) and the Mitsubishi robot
class RobotCom:
    """Robot class for programming Mitsubishi robots"""
    LAST_MSG = ""       # Keep a copy of the last message received
    CONNECTED = False   # Connection status is known at all times
    rounding = -1
    speed_mms = 50
    accel_percent_joints = 100
    accel_percent_linear = 100
    
    # This is executed when the object is created
    def __init__(self):
        self.BUFFER_SIZE = 512 # bytes
        self.TIMEOUT = 60 # seconds # No robot movement should take more than 60 seconds
        #self.TIMEOUT = 10 # seconds
        self.sock = None
        self.sockjnts = None       
        
    # Disconnect from robot
    def disconnect(self):
        self.CONNECTED = False
        try:
            self.sockjnts.close()
        except:
            pass
        try:
            if self.sock is not None:
                self.Run('1;1;CLOSE')
                self.sock.close()
        except:
            pass    
        UpdateStatus(ROBOTCOM_DISCONNECTED)
        return True
    
    # Connect to robot
    def connect(self, ip, port=10001):
        global ROBOT_MOVING
        self.disconnect()
        print_message('Connecting to robot %s:%i' % (ip, port))
        # Create new socket connection
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.settimeout(4)
        self.sockjnts = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sockjnts.settimeout(4)        
        UpdateStatus(ROBOTCOM_WORKING)
        try:
            self.sock.connect((ip, port))
            
        except ConnectionRefusedError as e:
            print(str(e))
            print_message("Connection refused")
            return False
        
        self.CONNECTED = True
        ROBOT_MOVING = False
        print_message('Waiting for welcome message...')
        UpdateStatus(ROBOTCOM_WORKING)
        #time.sleep(2)
        
        # receive welcome message and output to the log
        self.Run('1;1;OPEN=ROBODK',False)
        #print(self.recv_str())
        # notify status that the robot is still working
        UpdateStatus(ROBOTCOM_WORKING)
        
        # send activate robot and read confirmation
        self.Run('1;1;RSTALRM',False)
        self.Run('1;1;CNTLON',False)
        self.Run('1;1;SRVON')
        # RoboDK provides xyzwpr data for the TCP with respect to the robot reference frame for linear movements
        #self.Run('SetWRF', [0, 0, 0, 0, 0, 0])
        #self.sock.settimeout(self.TIMEOUT)        
        #self.sockjnts.connect((ip, 10002))
        
        q = queue.Queue()
        t = threading.Thread(target=robot_monitor, args=(q, self.sock))
        t.daemon = True
        t.start()
        UpdateStatus(ROBOTCOM_READY)
        return True

    # Send a line to the robot through the communication port (TCP/IP)
    def send_str(self, msg):
        try:
            sent = self.sock.send(bytes(msg+'\0','ascii'))
            if sent == 0:
                return False
            return True
        except ConnectionAbortedError as e:
            self.CONNECTED = False
            print(str(e))
            return False
    
    # Receive a line from the robot through the communication port (TCP/IP)
    def recv_str(self):
        bdata = b''
        try:
            bdata = self.sock.recv(self.BUFFER_SIZE)
            
        except ConnectionAbortedError as e:
            self.CONNECTED = False
            print(str(e))
            return
            
        if bdata == b'':
            return None
        
        try:
            self.LAST_MSG = bdata.decode('ascii')
        except Exception as e:
            pass
            
        return self.LAST_MSG
    
    # Run a specific command and provide required parameters   
    def Run(self, cmd, send_ready=True):
        # Skip the command if the robot is not connected
        global STATUS
        if (not self.CONNECTED) or (ok2SendCmd() == False):
            UpdateStatus(ROBOTCOM_NOT_CONNECTED)
            return False
            
        #---------- Send robot command -------
        # notify RoboDK
        #print('sending: %s' % cmd)
        #if not ("JPOSF" in cmd):
        #    UpdateStatus(ROBOTCOM_WORKING)

        # Try to send the command
        if self.send_str(cmd) is False:
            print_message("Robot connection broken")
            UpdateStatus(ROBOTCOM_NOT_CONNECTED_BRS)
            return False

        # Try to receive a response
        robot_msg = self.recv_str()
        if self.LAST_MSG is None:
            print_message("Robot connection broken")
            UpdateStatus(ROBOTCOM_NOT_CONNECTED_BRR)
            return False
                    
        try: #This throws an exception on emergency stop
            if 'Qer' in robot_msg:
                print("SMS:" + robot_msg)
                self.send_str("1;1;ERROR")
                errno = self.recv_str()[3:7]
                self.send_str("1;1;ERRORMES" + str(errno))
                print_error(self.recv_str()[3:])
                return False
                
        except Exception as e:
            pass
        
        if "JPOSF" in cmd:
            # robot response after a GetJoints request
            jointList = robot_msg.split(";")[1:12:2]
            for joint in jointList:
                if "*" in str(joint):
                    jointList.remove(joint)
            #For handling scara
            print_joints(jointList)
            return True
        
        # Any other acknowledge message (assumed to be successful)
        # By default, we will send the command Ready at every instruction (one Run per instruction in general)
        #print(robot_msg)
        #if send_ready:
            #UpdateStatus(ROBOTCOM_READY)
        else:
            # Save the Ready status to send later and notify RoboDK that the instruction was completed
            global STATUS
            STATUS = ROBOTCOM_READY

        return True

# Receives a string through TCP/IP. It reads until if finds NULL character
def read_line(socket):
    data = socket.recv(512)
    s = data.decode("ascii")
    return s


def send_line(socket, msg):
    data = msg.encode("ascii")
    socket.sendall(data)
    
def calc_turns_flag(jnts):
    arrRotationBits = [0] * 14
    for i in range(0, len(jnts)):
      arrRotationBits[i] = int(round((jnts[i]+180) // 360))

    finalNumber = 0
    for i in range(0, len(jnts)):
      finalNumber += arrRotationBits[i] << (4*i)
    #print("Final number" + str(finalNumber))
    return finalNumber

# Specific thread to monitor robot communication
# This thread establishes a permanent link between the robot and the PC to retrieve the robot position at all times
# The robot position is displayed only when the robot is executing a motion command
# When the communication link is broken it will notify the user
def robot_monitor(q, socket):
    global ROBOT_MOVING
    global ROBOT_AXIS_COUNT
    try:
        while True:
            if (ROBOT_MOVING == True):
                send_line(socket, "1;1;JPOSF")
                response = read_line(socket)
                robot_msg = response.split(';')
                #Check for empty message
                if (len(robot_msg) < 4):
                    continue
                
                #bdata = socket.recv(512)
                #if bdata == b'':
                #    print_message("Invalid monitoring response")
                #    return                
                #robot_msg = bdata.decode('ascii')
                joints_str = robot_msg[1:(ROBOT_AXIS_COUNT*2):2]
                for joint in joints_str:
                    if "*" in str(joint):
                        joints_str.remove(joint)

                print_joints(joints_str, True)
                time.sleep(0.05) # Don't overload the poor robot
                #Check if robot is done moving
                send_line(socket, "1;1;STATE")
                reply = read_line(socket)
                
                response = reply.split(";") # reply is the return string for the command
                value = response[4]
                 
                #if int(value[0], 16) & 0b0001 == 0b0001:  # 1. bit is the teach mode bit
                    #status.teaching_mode = True
                #if int(value[0], 16) & 0b0010 == 0b0010:  # 2. bit is the SRVON bit
                    #    status.servo_on = True
                #print("SMS:"+bin(int(value,16)))
                #for i in range(0,len(bin(int(value,16)))):
                #    print("SMS:" + str(i) + ":" +bin(int(value,16))[i])


                if int(value[0], 16) & 0b0100 == 0b0100:  # 3. bit is the RUN/STOP bit #This only works if you want the robot to come to a full stop
                #if (int(value[0], 16) & 0b0100 == 0b0100) or (bin(int(value,16))[8+7] == 0):  #Use Operation Disable/Enable instead 
                    pass
                else:
                    global MOV_LAST_J
                    if joints_error(MOV_LAST_J, joints_str) < 4.0:
                        str_jnts = [str(x) for x in MOV_LAST_J]
                        print_joints(str_jnts, True)
                    else:
                        print_message("SMS:Warning: Error moving robot: " + str(joints_str))   
                        
                    ROBOT_MOVING = False
                    if (ok2SendCmd() == True):
                        # Make sure we flush all monitoring            
                        UpdateStatus(ROBOTCOM_READY)
                    #print("Done")
                    
                #if int(value[0], 16) & 0b1000 == 0b1000:  # 4. bit is operation enabled bit
                #    status.control_on = True
                #else:
                #    status.off = True
                
                
                #msg_id = int(robot_msg[1:5])
                #if msg_id == 3007:
                    # monitoring stream of data: [3007][j1, j2, j3, j4, j5, j6]
                    #print_joints(robot_msg[7:-2].replace(',',''), True)
                #elif msg_id == 3010:
                    # position data is also part of the stream. Ignore
                    # [3010][212.985, -93.965, 34.273, 180.000, -1.967, 63.936]
                #    pass
                #elif msg_id == 3000:
                    # Welcome message is:
                    # [3000][Connected to Mecademic Meca500 Robot.]
                #    pass
                #else:
                #    print(robot_msg)
                #    print_message("Unknown monitoring response")
                #    return
                
    except Exception as e:
            e_str = str(e)
            print_error(e_str)
        
def print_error(e_str):
    print_message(e_str)    
    if 'RUN or WAI' in e_str:
        #Cannot execute (RUN or WAI)
        print_message("Do you have the program open?")
    
    UpdateStatus(ROBOTCOM_UNKNOWN)
        
#-----------------------------------------------------------------------------
#-----------------------------------------------------------------------------
# Generic RoboDK driver for a specific Robot class
global ROBOT
global ROBOT_IP
global ROBOT_PORT
global ROBOT_MOVING
global ROBOT_TOOL_STR


ROBOT = RobotCom()
ROBOT_IP = "127.0.0.1"      # IP of the robot
ROBOT_PORT = 10000          # Communication port of the robot
ROBOT_AXIS_COUNT = 6
ROBOT_MOVING = False
ROBOT_TOOL_STR = '0.000,0.000,0.000,0.000,0.000,0.000'


#------------ robot connection -----------------
# Establish connection with the robot
def RobotConnect():
    global ROBOT
    global ROBOT_IP
    global ROBOT_PORT
    ROBOT.connect(ROBOT_IP, ROBOT_PORT)
    
# Disconnect from the robot
def RobotDisconnect():
    global ROBOT
    ROBOT.disconnect()
    
        
#-----------------------------------------------------------------------------
# Generic RoboDK driver tools

# Note, a simple print() will flush information to the log window of the robot connection in RoboDK
# Sending a print() might not flush the standard output unless the buffer reaches a certain size

def print_message(message):
    """print_message will display a message in the log window (and the connexion status bar)"""
    print("SMS:" + message)
    sys.stdout.flush() # very useful to update RoboDK as fast as possible

def set_driver_status(message):
    """print_message will display a message in the log window (and the connexion status bar)"""
    print("RE:" + message)
    sys.stdout.flush()  # very useful to update RoboDK as fast as possible

def show_message(message):
    """show_message will display a message in the status bar of the main window"""
    print("SMS2:" + message)
    sys.stdout.flush() # very useful to update RoboDK as fast as possible

def print_joints(joints, ismoving = False):
    if ismoving:
        # Display the feedback of the joints when the robot is moving
        if ROBOT_MOVING:
            #print("CJNT_MOVING " + " ".join(format(x, ".5f") for x in joints)) # if joints is a list of float
            print("JNTS_MOVING " + " ".join(joints))
            
    else:
        #print("CJNT " + " ".join(format(x, ".5f") for x in joints)) # if joints is a list of float
        print("JNTS " + " ".join(joints))
    sys.stdout.flush() # very useful to update RoboDK as fast as possible
    #print("Test")
            
# ---------------------------------------------------------------------------------
# Constant values to display status using UpdateStatus()
ROBOTCOM_UNKNOWN                = -1000
ROBOTCOM_CONNECTION_PROBLEMS    = -3
ROBOTCOM_DISCONNECTED           = -2
ROBOTCOM_NOT_CONNECTED          = -1
ROBOTCOM_NOT_CONNECTED_BRS      = -4
ROBOTCOM_NOT_CONNECTED_BRR      = -5
ROBOTCOM_READY                  =  0
ROBOTCOM_WORKING                =  1
ROBOTCOM_WAITING                =  2

# Last robot status is saved
global STATUS
STATUS = ROBOTCOM_DISCONNECTED

def ok2SendCmd():
    global STATUS
    if (STATUS == ROBOTCOM_UNKNOWN):
        return False
    return True


# UpdateStatus will send an appropriate message to RoboDK which will result in a specific coloring
# for example, Ready will be displayed in green, Waiting... will be displayed in Yellow and other messages will be displayed in red
def UpdateStatus(set_status=None):
    global STATUS
    if set_status is not None:
        STATUS = set_status
        
    if STATUS == ROBOTCOM_CONNECTION_PROBLEMS:
        print_message("Connection problems")
    elif STATUS == ROBOTCOM_DISCONNECTED:
        print_message("Disconnected")
    elif STATUS == ROBOTCOM_NOT_CONNECTED:
        print_message("Not connected")
    elif STATUS == ROBOTCOM_NOT_CONNECTED_BRS:
        print_message("Not connected -- send")
    elif STATUS == ROBOTCOM_NOT_CONNECTED_BRR:
        print_message("Not connected -- recv")
    elif STATUS == ROBOTCOM_READY:
        print_message("Ready")
    elif STATUS == ROBOTCOM_WORKING:
        print_message("Working...")
    elif STATUS == ROBOTCOM_WAITING:
        print_message("Waiting...")
    else:
        print_message("Unknown status")
        print_message("Connection problems")

# Sample set of commands that can be provided by RoboDK of through the command line
def TestDriver():    
    RunCommand("CONNECT 127.0.0.1 10001")
    while True:
        RunCommand("MOVJ -41.331827 37.725242 77.778252 -0.074857 64.571346 -41.303277 679.589111 -597.855225 499.146271 -179.998655 -0.100830 -179.996364")
        RunCommand("MOVJ -21.219167 56.087544 102.869740 -0.104701 21.136253 -21.125115 678.851196 -263.638880 91.766626 -179.998655 -0.100830 -179.996364")
    #RunCommand("SETTOOL -0.025 -41.046 50.920 60.000 -0.000 90.000")
    #RunCommand("MOVJ -5.362010 46.323420 20.746290 74.878840 -50.101680 61.958500")
    #RunCommand("SPEED 250")
    #RunCommand("MOVL 0 0 0 0 0 0 -5.362010 50.323420 20.746290 74.878840 -50.101680 61.958500")
    #RunCommand("PAUSE 2000") # Pause 2 seconds

#-------------------------- Main driver loop -----------------------------
# Read STDIN and process each command (infinite loop)
# IMPORTANT: This must be run from RoboDK so that RoboDK can properly feed commands through STDIN
# This driver can also be run in console mode providing the commands through the console input
def RunDriver():
    for line in sys.stdin:
        RunCommand(line)
        
# Each line provided through command line or STDIN will be processed by RunCommand    
def RunCommand(linecmd):
    global ROBOT_IP
    global ROBOT_PORT
    global ROBOT_AXIS_COUNT
    global ROBOT
    global ROBOT_MOVING
    global ROBOT_TOOL_STR
    global MOV_LAST_J
    global MOV_LAST_J_NOMINAL

    # must contain 1 or more hex character and no '.'
    def isHex(msg):
        retVal = 0
        for ch in msg:
            if (ch == '.'):
                return 0
            if((ch >='A') and (ch<='F')):
                retVal = 1
            elif ((ch >='a') and (ch<='f')):
                retVal = 1
        return retVal

    # strip a line of words into a list of numbers
    def line_2_values(words):
        values = []        
        for word in words[1:]:
            try:
                number = float(word)
                if(isHex(word)!=0):
                    tmp = int(word, 16)
                    values.append(float(tmp))
                else:
                    values.append(number)
            except:
                pass
        return values
    
    linecmd = linecmd
    words = linecmd.split(' ')
    values = line_2_values(words)
    nvalues = len(values)
    nwords = len(words)
    
    if (ROBOT_MOVING == True) and (not linecmd.startswith("STOP")):
        # Don't take commands while busy, unless it's to kill the robot
        return    
    
    if linecmd == "":
        # Skip if no command is provided
        return
    
    elif nwords >= 2 and linecmd.startswith("CONNECT"):
        # Connect to robot provided the IP and the port
        ROBOT_IP = words[1]
        if nwords >= 3 and nvalues >= 1:# and int(values[0]) != 10000:
            ROBOT_PORT = int(values[0])
            #print("Using default port 10000, not %i" % ROBOT_PORT)

        if nwords >= 4 and nvalues >= 1:# and int(values[0]) != 10000:
            ROBOT_AXIS_COUNT = int(values[1])
            #print("Using default port 10000, not %i" % ROBOT_PORT)

        RobotConnect()
    
    elif nwords >= ROBOT_AXIS_COUNT and linecmd.startswith("MOVJ"):
        # Activate the monitor feedback
        MOV_LAST_J = values[:ROBOT_AXIS_COUNT]
        MOV_LAST_J_NOMINAL = values[:ROBOT_AXIS_COUNT]
        UpdateStatus(ROBOTCOM_WORKING)        
        
        # Execute a joint move. RoboDK provides j1,j2,...,j6,x,y,z,w,p,r
        err_flag = False
        #err_flag |= ROBOT.Run("1;1;1;1;SLOTINIT", False)
        err_flag |= ROBOT.Run("1;1;FDELMRL", False)
        err_flag |= ROBOT.Run("1;1;NEW", False)
        err_flag |= ROBOT.Run("1;1;LOAD=MRL", False)
        err_flag |= ROBOT.Run("1;1;EDATA 1 J1=(" + (','.join(format(vi, ".6f") for vi in values[:6])) + ")", False)
        #err_flag |= ROBOT.Run("1;1;EDATA 2 Cnt 1", False)
        err_flag |= ROBOT.Run('1;1;EDATA 2 SPD %.3f' % ROBOT.speed_mms , False)
        err_flag |= ROBOT.Run('1;1;EDATA 3 ACCEL %.3f' % ROBOT.accel_percent_joints , False)
        err_flag |= ROBOT.Run('1;1;EDATA 4 MOV J1', False)
        #err_flag |= ROBOT.Run('1;1;EDATA 5 END', False)
        err_flag |= ROBOT.Run("1;1;SAVE", False)
        err_flag |= ROBOT.Run("1;1;RSTPRG", False)
        err_flag |= ROBOT.Run("1;1;PRGLOAD=MRL", False)
        err_flag |= ROBOT.Run("1;1;RSTPRG", False)
        err_flag |= ROBOT.Run("1;1;RUNMRL;1")
        #This will turn on the monitoring thread as such this needs to be after the command to make the robot moveROBOT_MOVING = True
        ROBOT_MOVING = True
        
    elif nvalues >= (ROBOT_AXIS_COUNT+6) and linecmd.startswith("MOVL"):       
        MOV_LAST_J = values[:ROBOT_AXIS_COUNT]
        MOV_LAST_J_NOMINAL = values[:ROBOT_AXIS_COUNT]
        
        UpdateStatus(ROBOTCOM_WORKING)        
        
        # Execute a linear move. RoboDK provides j1,j2,...,j6,x,y,z,w,p,r
        #ROBOT.Run('1;1;EXECMVS ' + '(' + (','.join(format(vi, ".6f") for vi in values[6:])) + ")(7,0)")
        #Linear format for mitsubishi is xyz J4 J5 J6 and not xyzwpr
        LinearValues = values[ROBOT_AXIS_COUNT:12]
        if ROBOT_AXIS_COUNT == None:
            ROBOT_AXIS_COUNT = 6
        
        #turns_flag = calc_turns_flag(values[0:ROBOT_AXIS_COUNT])
        #config_flag = int(6) | (0 if values[4] >= 0 else 1)
        if (ROBOT_AXIS_COUNT == 4):
            # For SCARA robots
            turns_flag = calc_turns_flag(values[0:ROBOT_AXIS_COUNT])
            config_flag = 0 if values[1] < 0 else 4
        else:            
            turns_flag = calc_turns_flag(values[0:ROBOT_AXIS_COUNT])
            config_flag = int(6) | (0 if values[4] > 0 else 1)
        
        #config_flag = 7 #0b110, not 0b111
        #print("Mov Cartesian: " + str(LinearValues) + " Config flag: " + str(config_flag) + "," + str(turns_flag))
        #The old code here didn't seem to work for sending linear moves so I just quickly implemented that
        # Execute a linear move. RoboDK provides j1,j2,...,j6,x,y,z,w,p,r
        err_flag = False
        #err_flag |= ROBOT.Run("1;1;1;1;SLOTINIT", False)
        err_flag |= ROBOT.Run("1;1;FDELMRL", False)
        err_flag |= ROBOT.Run("1;1;NEW", False)
        err_flag |= ROBOT.Run("1;1;LOAD=MRL", False)
        err_flag |= ROBOT.Run("1;1;EDATA 1 Base (0.000,0.000,0.000,0.000,0.000,0.000)")
        err_flag |= ROBOT.Run("1;1;EDATA 2 Tool (" + ROBOT_TOOL_STR + ")")
        err_flag |= ROBOT.Run('1;1;EDATA 3 ACCEL %.3f' % ROBOT.accel_percent_joints , False)
        err_flag |= ROBOT.Run('1;1;EDATA 4 SPD %.3f' % ROBOT.speed_mms , False)
        #err_flag |= ROBOT.Run("1;1;EDATA 5 P1=(" + (','.join(format(vi, ".3f") for vi in LinearValues[0:6])) + ")" + "(" + str(config_flag) + "," + str(turns_flag) + ")") 
        err_flag |= ROBOT.Run("1;1;EDATA 5 J1=(" + (','.join(format(vi, ".6f") for vi in values[:6])) + ")", False)
        err_flag |= ROBOT.Run('1;1;EDATA 6 Mvs J1', False)
        err_flag |= ROBOT.Run("1;1;SAVE", False)
        err_flag |= ROBOT.Run("1;1;RSTPRG", False)
        err_flag |= ROBOT.Run("1;1;PRGLOAD=MRL", False)
        err_flag |= ROBOT.Run("1;1;RSTPRG", False)
        err_flag |= ROBOT.Run("1;1;RUNMRL;1")

        # Activate the monitor feedback
        ROBOT_MOVING = True

    elif nvalues >= (ROBOT_AXIS_COUNT+12) and linecmd.startswith("MOVC"):       
        UpdateStatus(ROBOTCOM_WORKING)        
        
        # Execute a linear move. RoboDK provides j1,j2,...,j6,x,y,z,w,p,r
        #ROBOT.Run('1;1;EXECMVS ' + '(' + (','.join(format(vi, ".6f") for vi in values[6:])) + ")(7,0)")
        #Linear format for mitsubishi is xyz J4 J5 J6 and not xyzwpr
        wayPoint0 = MOV_LAST_J_NOMINAL
        wayPoint1 = values[:ROBOT_AXIS_COUNT]
        wayPoint2 = values[ROBOT_AXIS_COUNT:ROBOT_AXIS_COUNT*2]

        if ROBOT_AXIS_COUNT == None:
            ROBOT_AXIS_COUNT = 6


        MOV_LAST_J = values[ROBOT_AXIS_COUNT:ROBOT_AXIS_COUNT*2]
        MOV_LAST_J_NOMINAL = values[ROBOT_AXIS_COUNT:ROBOT_AXIS_COUNT*2]

        #config_flag = 7 #0b110, not 0b111
        #print("Mov Cartesian: " + str(LinearValues) + " Config flag: " + str(config_flag) + "," + str(turns_flag))
        #The old code here didn't seem to work for sending linear moves so I just quickly implemented that
        # Execute a linear move. RoboDK provides j1,j2,...,j6,x,y,z,w,p,r
        err_flag = False
        #err_flag |= ROBOT.Run("1;1;1;1;SLOTINIT", False)
        err_flag |= ROBOT.Run("1;1;FDELMRL", False)
        err_flag |= ROBOT.Run("1;1;NEW", False)
        err_flag |= ROBOT.Run("1;1;LOAD=MRL", False)
        err_flag |= ROBOT.Run("1;1;EDATA 1 Base (0.000,0.000,0.000,0.000,0.000,0.000)")
        err_flag |= ROBOT.Run("1;1;EDATA 2 Tool (" + ROBOT_TOOL_STR + ")")
        err_flag |= ROBOT.Run('1;1;EDATA 3 ACCEL %.3f' % ROBOT.accel_percent_joints , False)
        err_flag |= ROBOT.Run('1;1;EDATA 4 SPD %.3f' % ROBOT.speed_mms , False)
        #err_flag |= ROBOT.Run("1;1;EDATA 5 P1=(" + (','.join(format(vi, ".3f") for vi in LinearValues[0:6])) + ")" + "(" + str(config_flag) + "," + str(turns_flag) + ")") 
        err_flag |= ROBOT.Run("1;1;EDATA 5 J1=(" + (','.join(format(vi, ".6f") for vi in wayPoint0)) + ")", False)
        err_flag |= ROBOT.Run("1;1;EDATA 6 J2=(" + (','.join(format(vi, ".6f") for vi in wayPoint1)) + ")", False)
        err_flag |= ROBOT.Run("1;1;EDATA 7 J3=(" + (','.join(format(vi, ".6f") for vi in wayPoint2)) + ")", False)
        
        err_flag |= ROBOT.Run('1;1;EDATA 8 Mvr J1, J2, J3', False)
        err_flag |= ROBOT.Run("1;1;SAVE", False)
        err_flag |= ROBOT.Run("1;1;RSTPRG", False)
        err_flag |= ROBOT.Run("1;1;PRGLOAD=MRL", False)
        err_flag |= ROBOT.Run("1;1;RSTPRG", False)
        err_flag |= ROBOT.Run("1;1;RUNMRL;1")

        # Activate the monitor feedback
        ROBOT_MOVING = True
        
    elif linecmd.startswith("CJNT"):
        # Retrieve the current position of the robot
        ROBOT.Run('1;1;JPOSF')    

    elif nvalues >= 1 and linecmd.startswith("SPEED"):
        UpdateStatus(ROBOTCOM_WORKING)     
        # First value is linear speed in mm/s\
        # IMPORTANT! We should only send one "Ready" per instruction
        #Initial code, blindly uses override
        #if values[0] > 0:            
            # make sure we do not exceed maximum speed (robot turns into error mode)
            #ROBOT.Run('1;1;EXECSPD (%.3f)' % min(10000.0, values[0]), False)
            #speed_percent = min(values[0]*100/5000, 100)
            #ROBOT.Run('1;1;OVRD=%.3f' % speed_percent, False)

        if values[0] > 0:            
            #Linear speed 
            #mm/s
            #Needs to be inside move program
            #ROBOT.Run('1;1;SPD=%.3f' % values[0], False)
            ROBOT.speed_mms = values[0]
        #    pass
        if values[1] > 0:            
            #Joint speed
            #mm/s
            speed_percent = min(values[1]*100/5000, 100)
            speed_percent = max(speed_percent,1)
            ROBOT.Run('1;1;JOVRD %.3f' % speed_percent, False)
        if values[2] > 0:    
            #Linear Acceleration        
            speed_percent = min(values[2]*100/5000, 100)
            speed_percent = max(speed_percent,1)
            #Needs to be inside move program
            #ROBOT.Run('1;1;ACCEL=%.3f' % speed_percent, False)
            ROBOT.accel_percent_linear = speed_percent
        if values[3] > 0:            
            #Joint Acceleration        
            speed_percent = min(values[3]*100/5000, 100)
            speed_percent = max(speed_percent,1)
            #ROBOT.Run('1;1;ACCEL=%.3f' % speed_percent, False)
            ROBOT.accel_percent_joints = speed_percent
            pass

        # Provokes sending Ready:
        UpdateStatus()
    elif nvalues >= 1 and linecmd.startswith("SETROUNDING"):
        # Set the rounding/smoothing value. Also known as ZoneData in ABB or CNT for Fanuc

        #This is pointless as where feeding the moves one by one
        #ROBOT.Run('SetCornering', [1] if values[0] > 0 else [0])
        ROBOT.rounding = values[0]
        #if values[0] > 0:
        #    ROBOT.Run('1;1;CNT 1,%.0f,%.0f' % (values[0],values[0]), False)
        #else:
        #    ROBOT.Run('1;1;CNT 0', False)
        UpdateStatus(ROBOTCOM_READY)
        pass
    
    
    
    elif nvalues >= 1 and linecmd.startswith("PAUSE"):
        UpdateStatus(ROBOTCOM_WAITING)
        # Run a pause
        if values[0] > 0:
            import time
            time.sleep(values[0] * 0.001)
        UpdateStatus(ROBOTCOM_READY)
        
    elif nwords == 3 and linecmd.startswith("SETDO"):
        #16 bit version
        UpdateStatus(ROBOTCOM_WORKING)
        dIO_id = words[1]
        dIO_value = words[2]
        codeStr = ""
        codeStr = str("OUT={0};{1}").format((dIO_id),(dIO_value))
        ROBOT.Run('1;1;' + codeStr)
        print_message("16 + " + codeStr)
        UpdateStatus(ROBOTCOM_READY)

    elif nvalues >= 2 and linecmd.startswith("SETDO"):
        UpdateStatus(ROBOTCOM_WORKING)
        dIO_id = values[0]
        dIO_value = values[1]
        codeStr = ""
        #print_message("Warning: Setting DO[%i] = %.1f not implemented" % (dIO_id, dIO_value))
        if not str(dIO_id).startswith('OUT'):
            codeStr = str("OUT={0:d};{1:d}").format(int(dIO_id),int(dIO_value))
        else:
            codeStr = str("{0:d} = {1:d}").format(dIO_id,dIO_value)
        print_message("1 + " + codeStr)
        ROBOT.Run('1;1;' + codeStr)
        UpdateStatus(ROBOTCOM_READY)

    elif nvalues >= 2 and linecmd.startswith("WAITDI"):
        UpdateStatus(ROBOTCOM_WORKING)
        dIO_id = values[0]
        dIO_value = values[1]
        print_message("Warning: Waiting DI[%i] = %.1f not implemented" % (dIO_id, dIO_value))
        UpdateStatus(ROBOTCOM_READY)
        
    elif nvalues >= 6 and linecmd.startswith("SETTOOL"):
        # Set the Tool reference frame provided the 6 XYZWPR values by RoboDK
        #Z axis is reversed for scara robots
        #if ROBOT_AXIS_COUNT == 4:
        #    values[2] = -values[2]
        ROBOT_TOOL_STR = ','.join(format(vi, ".6f") for vi in values)
        ROBOT.Run('1;1;EXECTOOL (' + ROBOT_TOOL_STR + ')')
        UpdateStatus(ROBOTCOM_READY)
        
    elif nvalues >= 1 and nwords >= 2 and linecmd.startswith("RUNPROG"):
        UpdateStatus(ROBOTCOM_WORKING)
        prog_id = int(values[0])
        prog_name = "Program %i" % prog_id
        if nwords >= 3:
            prog_name = words[1]
            
        print_message("Warning: Running program %s not implemented" % (prog_name))
        UpdateStatus(ROBOTCOM_READY)
        
    elif nwords >= 2 and linecmd.startswith("POPUP "):
        UpdateStatus(ROBOTCOM_WORKING)
        message = linecmd[6:]            
        print_message("Warning: Display message %s not implemented" % (message))
        UpdateStatus(ROBOTCOM_READY)
        
    elif linecmd.startswith("DISCONNECT"):
        # Disconnect from robot
        ROBOT.disconnect()
        UpdateStatus(ROBOTCOM_DISCONNECTED)
        
    elif linecmd.startswith("TEST"):
        # Call custom procedure for quick testing
        TestDriver()

        
    elif linecmd.startswith("QUIT"):
        # Stop the driver
        ROBOT.disconnect()
        UpdateStatus(ROBOTCOM_DISCONNECTED)
        quit(0) # Stop the driver
    elif linecmd.startswith("STOP"):
        ROBOT_MOVING = False
        # Stop the robot right away
        ROBOT.Run('1;1;SRVOFF')

        #Stop program running
        ROBOT.Run("1;1;RSTPRG", False)

        ROBOT.disconnect()
        UpdateStatus(ROBOTCOM_DISCONNECTED)
   
    elif linecmd.startswith("c "):
        ROBOT.Run(linecmd[2:].strip())
        UpdateStatus(ROBOTCOM_READY)
    elif linecmd.startswith("r "):
        #UpdateStatus(ROBOTCOM_WORKING)
        codeStr = linecmd[2:].strip()
        ROBOT.Run('1;1;' + codeStr)
        retStr = ROBOT.LAST_MSG
        retStr = retStr.lstrip("QoK")
        set_driver_status(retStr)
        UpdateStatus(ROBOTCOM_READY)
    elif linecmd.startswith("GETDI"):
        #UpdateStatus(ROBOTCOM_WORKING)
        dIO_id = values[0]
        codeStr = ""
        if not str(dIO_id).startswith('IN'):
            codeStr = str("IN{0:d}").format(int(dIO_id))
        else:
            codeStr = str("IN{0:d}").format(dIO_id)
        ROBOT.Run('1;1;' + codeStr)
        retStr = ROBOT.LAST_MSG
        retStr = retStr.lstrip("QoK")
        set_driver_status(retStr)
        UpdateStatus(ROBOTCOM_READY)
    
    else:
        print("Unknown command: " + linecmd)
    
    # Stop monitoring feedback
    #ROBOT_MOVING = False

def RunMain():
    """Call Main procedure"""
    
    # Flush version
    print_message("RoboDK Driver v2.3.1 for Mitsubishi robot controllers")
    
    cmdlist = ''
    cmdlist +='c 1;1;SRVOFF|Servo OFF|'
    cmdlist +='c 1;1;SRVON|Servo ON|'    
    cmdlist +='c 1;1;CNTLON|Control On|'    
    cmdlist +='c 1;1;CNTLOFF|Control Off|'    

    #1;1;CNTLOFF
    
    print("CMDLIST:" + cmdlist)
    sys.stdout.flush()
    
    # Flush Disconnected message
    UpdateStatus()
    
    # Run the driver from STDIN
    RunDriver()
    
    # Test the driver with a sample set of commands
    #TestDriver()

if __name__ == "__main__":
    """Call Main procedure"""
    # Important, leave the Main procedure as RunMain
    RunMain()
    