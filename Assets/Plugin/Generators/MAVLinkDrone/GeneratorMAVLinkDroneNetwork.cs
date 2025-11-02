using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CrcSharp;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YamlDotNet.Serialization;
using static MAVLink;

namespace Generators.MAVLinkDrone
{
    public class MAVLinkDroneNetwork : IDMXGenerator
    {
        private Dictionary<byte, Drone> drones = new Dictionary<byte, Drone>();
        private UdpClient client;
        private IPEndPoint selfEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Loopback, 14550);
        private MavlinkParse parse;

        [YamlMember(Description = "How many drones to initialize on this network")]
        public EquationNumber droneCount = 254;
        [YamlMember(Description = "What UDP port to use for this network. These can NOT overlap with other networks")]
        public EquationNumber networkPort = 14550;
        public DMXChannel channelStart = 0;
        public IDroneLayoutProvider layoutProvider = new GridLayout();
        public bool pyroFeature = false;
        private List<UniTask> tasks = new List<UniTask>();
        private CancellationTokenSource cancellationTokenSource = new();
        public void Construct()
        {
            client = new UdpClient();
            remoteEndPoint.Port = networkPort;
            client.Client.Bind(selfEndPoint);
            parse = new MavlinkParse();

            Debug.Log($"MAVLink Drone Network started on port {networkPort} with {droneCount} drones.");

            //initialize a certain ammount of drones
            //drones.Add(5, new Drone(5, client, remoteEndPoint));
            for (byte i = 1; i <= droneCount; i++)
            {
                //Debug.Log(i);
                drones.Add(i, new Drone(i, client, remoteEndPoint));
            }

            //layout the drones
            layoutProvider.LayoutDrones(ref drones);

            //startup tasks
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            tasks.Add(UniTask.Create(SendData, cancellationToken));
            tasks.Add(UniTask.Create(SendHeartbeat, cancellationToken));
            tasks.Add(UniTask.Create(ReceiveData, cancellationToken));
        }

        public void Deconstruct()
        {
            //end all the tasks
            cancellationTokenSource.Cancel();
        }

        public void GenerateDMX(ref List<byte> dmxData)
        {
            //lists for all data
            List<byte> XCoarse = new List<byte>();
            List<byte> XFine = new List<byte>();
            List<byte> YCoarse = new List<byte>();
            List<byte> YFine = new List<byte>();
            List<byte> ZCoarse = new List<byte>();
            List<byte> ZFine = new List<byte>();
            List<byte> LEDR = new List<byte>();
            List<byte> LEDG = new List<byte>();
            List<byte> LEDB = new List<byte>();
            foreach (Drone d in drones.Values)
            {
                //make their X move cleanly based on the time
                //float offset = ((float)Math.Sin(DateTime.UtcNow.Ticks * 0.00000001d) - 0.5f) * 0.01f;
                //d.SetPosition(((float)Math.Sin(DateTime.UtcNow.Ticks * 0.00000001d) - 0.5f) * 0.001f, 0, 40);
                //Debug.Log((float)DateTime.UtcNow.Millisecond * 0.001f);
                //force color to 255
                //d.LEDColor = new Color32(255, 255, 255, 255);
                //d.SetPosition(d.uid, 0, 0);
                //d.SetPosition(0.000950f, 0.000950f, -50);

                //convert the XYZ to bytes
                //do this by converting them to -1 to 1 range from -800 to 800
                Vector3 pos = d.GetDronePosition();
                //pos = new Vector3(0, 0, 0);
                //Debug.Log($"Drone {d.uid} position: {pos.x}, {pos.y}, {pos.z}");
                float scaledX = Mathf.InverseLerp(-800, 800, pos.x);
                float scaledY = Mathf.InverseLerp(-800, 800, pos.y);
                float scaledZ = Mathf.InverseLerp(-800, 800, pos.z);

                //convert to 16 bit ushorts
                var x = new Util.CoarseFineChannelSet(scaledX);
                var y = new Util.CoarseFineChannelSet(scaledY);
                var z = new Util.CoarseFineChannelSet(scaledZ);

                //Debug.Log($"Drone {d.uid} X scaled: {scaledX}, ushort: {x}, scaled back up: {Mathf.Lerp(-800, 800, (float)x / ushort.MaxValue)}");

                //Debug.Log($"{x.coarse}, {x.fine}");

                //merge into a single list
                List<byte> droneValues = new List<byte>();

                //append to dmxData
                droneValues.AddRange(x.ToList());
                droneValues.AddRange(y.ToList());
                droneValues.AddRange(z.ToList());


                if (pyroFeature)
                {
                    //try to get a pyro event
                    var pyevent = d.GetPyroEvent();
                    //pitch is -90 to 90, convert to 0-byte max value
                    //yaw is -180 to 180, convert to 0-byte max value
                    //roll is -180 to 180, convert to 0-byte max value
                    droneValues.Add((byte)(Mathf.InverseLerp(-180, 180, pyevent.pitch) * byte.MaxValue));
                    droneValues.Add((byte)(Mathf.InverseLerp(-180, 180, pyevent.yaw) * byte.MaxValue));
                    droneValues.Add((byte)(Mathf.InverseLerp(-180, 180, pyevent.roll) * byte.MaxValue));
                    droneValues.Add((byte)(pyevent.pyroIndex));
                }
                else
                {
                    Color32 color = d.GetDroneColor();
                    //Debug.Log($"Drone {d.uid} color: {color.r}, {color.g}, {color.b}");

                    //append the LED color
                    droneValues.Add(color.r);
                    droneValues.Add(color.g);
                    droneValues.Add(color.b);
                }

                //add to the individual lists
                XCoarse.Add(x.coarse);
                XFine.Add(x.fine);
                YCoarse.Add(y.coarse);
                YFine.Add(y.fine);
                ZCoarse.Add(z.coarse);
                ZFine.Add(z.fine);
                LEDR.Add(d.GetDroneColor().r);
                LEDG.Add(d.GetDroneColor().g);
                LEDB.Add(d.GetDroneColor().b);

                //set the data
                //dmxData.SetRange(channelStart + ((d.uid - 1) * droneValues.Count), droneValues.Count, droneValues.ToArray());
            }
            //merge together
            List<byte> alldata = new List<byte>();
            alldata.AddRange(XCoarse);
            alldata.AddRange(XFine);
            alldata.AddRange(YCoarse);
            alldata.AddRange(YFine);
            alldata.AddRange(ZCoarse);
            alldata.AddRange(ZFine);
            alldata.AddRange(LEDR);
            alldata.AddRange(LEDG);
            alldata.AddRange(LEDB);

            //set the data
            dmxData.SetRange(channelStart, alldata.Count, alldata.ToArray());
            //Debug.Log(dmxData.Count);
            return;
        }


        public async UniTask SendData(CancellationToken cancellationToken)
        {
            while (Application.isPlaying)
            {
                //check cancellation token
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                //send data for all drones
                const int perUpdate = 10; //how many drones to process per update
                int currentCount = 0;
                foreach (Drone d in drones.Values)
                {
                    d.SendGPSRaw();
                    d.SendGPSFiltered();
                    d.SendSYSStatus();
                    currentCount++;
                    if (currentCount == perUpdate)
                    {
                        //wait for a bit before sending the next batch
                        await UniTask.Delay(20);
                        currentCount = 0;
                    }
                }
                if (perUpdate > drones.Count)
                {
                    //wait a bit before sending the next batch because we wont hit the call in the for loop
                    await UniTask.Delay(100);
                }
            }
        }

        //update coroutine
        public async UniTask SendHeartbeat(CancellationToken cancellationToken)
        {
            while (Application.isPlaying)
            {
                //check cancellation token
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                //process X ammount every time
                const int perUpdate = 10; //how many drones to process per update
                int currentCount = 0;
                foreach (Drone d in drones.Values)
                {
                    //send heartbeat
                    d.SendHeartbeat();
                    currentCount++;
                    if (currentCount == perUpdate)
                    {
                        //wait for a bit before sending the next batch
                        await UniTask.Delay(100);
                        currentCount = 0;
                    }
                }
                if (perUpdate > drones.Count)
                {
                    //wait a bit before sending the next batch because we wont hit the call in the for loop
                    await UniTask.Delay(100);
                }
            }
        }
        Stream stream = new MemoryStream();
        public async UniTask ReceiveData(CancellationToken cancellationToken)
        {
            while (Application.isPlaying)
            {
                //check cancellation token
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                //listen for messages on 14555
                while (client.Available > 0)
                {
                    byte[] buffer;
                    try
                    {
                        buffer = client.Receive(ref selfEndPoint);
                    }
                    //connection forcibly closed
                    catch (SocketException ex)
                    {
                        //Debug.LogWarning(ex.Message);
                        continue;
                    }
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.Write(buffer, 0, buffer.Length);
                    stream.Seek(0, SeekOrigin.Begin);

                    var message = parse.ReadPacket(stream);

                    MAVLINK_MSG_ID messageId = (MAVLINK_MSG_ID)message.msgid;

                    //Debug.Log(messageId);

                    switch (messageId)
                    {
                        case MAVLINK_MSG_ID.COMMAND_LONG:
                            //figure out the command
                            var commandLong = (mavlink_command_long_t)message.data;
                            //find the drone based on the target sysid
                            bool found = drones.TryGetValue(commandLong.target_system, out Drone d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {commandLong.target_system}");
                                continue;
                            }

                            //Debug.Log((MAV_CMD)obj.command);
                            bool handled = false;
                            switch ((MAV_CMD)commandLong.command)
                            {
                                case MAV_CMD.REQUEST_AUTOPILOT_CAPABILITIES:
                                    handled = true;
                                    d.SendAutopilotCapabilities();
                                    break;
                                case MAV_CMD.SET_MESSAGE_INTERVAL:
                                    //we dont actually CARE about message intervals, so just ACK it
                                    handled = true;
                                    break;
                                case MAV_CMD.USER_1:
                                    //show control information, such as reload show, remove show or test pyro
                                    //https://github.com/skybrush-io/skybrush-server/blob/4fd65199a0578c56928981a709f0cabb69b15bd8/src/flockwave/server/ext/mavlink/enums.py#L629
                                    //ack them automatically
                                    switch (commandLong.param1)
                                    {
                                        case 0:
                                            d.ReloadShow();
                                            break;
                                        case 1:
                                            //d.RemoveShow();
                                            break;
                                        case 2:
                                            //d.TestPyro();
                                            break;
                                    }
                                    handled = true;
                                    break;
                                default:
                                    //handle other commands
                                    Debug.Log($"Unhandled command: {(MAV_CMD)commandLong.command}");
                                    handled = false;
                                    break;
                            }
                            if (handled)
                            {
                                d.SendCommandACK((MAV_CMD)commandLong.command, MAV_RESULT.ACCEPTED, message.sysid, message.compid);
                            }
                            break;
                        case MAVLINK_MSG_ID.COMMAND_INT:
                            //figure out the command
                            var commandInt = (mavlink_command_int_t)message.data;
                            //find the drone based on the target sysid
                            found = drones.TryGetValue(commandInt.target_system, out d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {commandInt.target_system}");
                                continue;
                            }

                            //Debug.Log((MAV_CMD)obj.command);
                            handled = false;
                            switch ((MAV_CMD)commandInt.command)
                            {
                                //USER_2 is show origin info
                                //https://github.com/skybrush-io/skybrush-server/blob/4fd65199a0578c56928981a709f0cabb69b15bd8/src/flockwave/server/ext/mavlink/driver.py#L2260
                                case MAV_CMD.USER_2:
                                    handled = true;
                                    d.SetShowOrigin(commandInt.x * 0.0000001f, commandInt.y * 0.0000001f, commandInt.z * 0.001f);
                                    break;
                                default:
                                    //handle other commands
                                    Debug.Log($"Unhandled command: {(MAV_CMD)commandInt.command}");
                                    handled = false;
                                    break;
                            }
                            if (handled)
                            {
                                d.SendCommandACK((MAV_CMD)commandInt.command, MAV_RESULT.ACCEPTED, message.sysid, message.compid);
                            }
                            break;
                        case MAVLINK_MSG_ID.PARAM_REQUEST_READ:
                            var paramRequestRead = (mavlink_param_request_read_t)message.data;
                            //find the drone based on the target sysid
                            found = drones.TryGetValue(paramRequestRead.target_system, out d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {paramRequestRead.target_system}");
                                continue;
                            }

                            //convert the byte array to a string
                            string paramName = Encoding.UTF8.GetString(paramRequestRead.param_id).TrimEnd('\0');
                            //get the parameter type
                            found = d.parameters.TryGetValue(paramName, out float paramValue);
                            if (!found)
                            {
                                //Debug.LogError($"Parameter not found: {paramName}, sending 0");
                                paramValue = 0f;
                            }

                            //send it back
                            var paramValueMessage = new mavlink_param_value_t(
                                paramValue,
                                (ushort)d.parameters.Count,
                                (ushort)d.parameters.Keys.ToList().IndexOf(paramName),
                                paramRequestRead.param_id,
                                (byte)MAV_PARAM_TYPE.REAL32
                            );
                            d.Transmit(MAVLINK_MSG_ID.PARAM_VALUE, paramValueMessage);
                            break;
                        case MAVLINK_MSG_ID.PARAM_SET:
                            var paramSet = (mavlink_param_set_t)message.data;
                            //find the drone based on the target sysid
                            found = drones.TryGetValue(paramSet.target_system, out d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {paramSet.target_system}");
                                continue;
                            }

                            //convert the byte array to a string
                            paramName = Encoding.UTF8.GetString(paramSet.param_id).TrimEnd('\0');
                            d.SetParameter(paramName, paramSet.param_value);
                            //send it back as a confirmation
                            var paramSetMessage = new mavlink_param_value_t(
                                paramSet.param_value,
                                (ushort)d.parameters.Count,
                                (ushort)d.parameters.Keys.ToList().IndexOf(paramName),
                                paramSet.param_id,
                                (byte)MAV_PARAM_TYPE.REAL32
                            );
                            d.Transmit(MAVLINK_MSG_ID.PARAM_VALUE, paramSetMessage);
                            //Debug.Log($"Parameter set: {paramName} = {paramSet.param_value}");
                            break;
                        case MAVLINK_MSG_ID.MISSION_COUNT:
                            var missionCountMessage = (mavlink_mission_count_t)message.data;
                            //find the drone based on the target sysid
                            found = drones.TryGetValue(missionCountMessage.target_system, out d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {missionCountMessage.target_system}");
                                continue;
                            }

                            //switch on the mission type itself, we dont care about geofencing
                            switch ((MAV_MISSION_TYPE)missionCountMessage.mission_type)
                            {
                                case MAV_MISSION_TYPE.FENCE:
                                    break;
                                default:
                                    Debug.LogWarning($"Unhandled mission type: {(MAV_MISSION_TYPE)missionCountMessage.mission_type}");
                                    break;
                            }

                            //acknowledge the mission count
                            d.SendMissionACK(MAV_MISSION_RESULT.MAV_MISSION_ACCEPTED, (MAV_MISSION_TYPE)missionCountMessage.mission_type, message.sysid, message.compid);
                            break;
                        case MAVLINK_MSG_ID.FILE_TRANSFER_PROTOCOL:
                            var ftpobj = (mavlink_file_transfer_protocol_t)message.data;
                            //find the drone based on the target sysid
                            found = drones.TryGetValue(ftpobj.target_system, out d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {ftpobj.target_system}");
                                continue;
                            }
                            //convert to a ftpmessage packet
                            var ftpIncomingMessage = MavlinkUtil.ByteArrayToStructureGC<FTPMessage>(ftpobj.payload, 0);
                            //print all the data on it
                            //Debug.Log($"FTP Message: Seq: {ftpIncomingMessage.seq_number}, Session: {ftpIncomingMessage.session}, Opcode: {ftpIncomingMessage.opcode}, Size: {ftpIncomingMessage.size}, ReqOpcode: {ftpIncomingMessage.req_opcode}, BurstComplete: {ftpIncomingMessage.burst_complete}, Offset: {ftpIncomingMessage.offset}, Data Length: {ftpIncomingMessage.data.Length}");
                            FTPMessage? sendMessage = null;
                            switch (ftpIncomingMessage.opcode)
                            {
                                case FTPMessage.ftp_opcode.CreateFile:
                                    //clear out the existing file data
                                    d.showFileRaw.Clear();
                                    //reply with ack because we don't actually care about the file name
                                    sendMessage = new FTPMessage(
                                        (ushort)(ftpIncomingMessage.seq_number + 1),
                                        ftpIncomingMessage.session,
                                        FTPMessage.ftp_opcode.ACK,
                                        0,
                                        ftpIncomingMessage.opcode,
                                        1,
                                        0,
                                        null
                                    );
                                    break;
                                case FTPMessage.ftp_opcode.WriteFile:
                                    //ensure the list is large enough
                                    //d.showFileRaw.EnsureCapacity((int)(ftpIncomingMessage.size + ftpIncomingMessage.offset));
                                    //write the data to the list
                                    d.showFileRaw.SetRange((int)ftpIncomingMessage.offset, ftpIncomingMessage.size, ftpIncomingMessage.data);
                                    //ack the write
                                    sendMessage = new FTPMessage(
                                        (ushort)(ftpIncomingMessage.seq_number + 1),
                                        ftpIncomingMessage.session,
                                        FTPMessage.ftp_opcode.ACK,
                                        0,
                                        ftpIncomingMessage.opcode,
                                        1,
                                        0,
                                        null
                                    );
                                    break;
                                case FTPMessage.ftp_opcode.TerminateSession:
                                    //print the total size of the file
                                    //Debug.Log($"File received with size: {d.showFileRaw.Count} bytes");
                                    //ack the termination
                                    sendMessage = new FTPMessage(
                                        (ushort)(ftpIncomingMessage.seq_number + 1),
                                        ftpIncomingMessage.session,
                                        FTPMessage.ftp_opcode.ACK,
                                        0,
                                        ftpIncomingMessage.opcode,
                                        1,
                                        0,
                                        null
                                    );
                                    break;
                                case FTPMessage.ftp_opcode.CalcFileCRC32:
                                    //calculate the CRC32 of the received data
                                    var crcParams = new CrcParameters(
                                        32,
                                        Convert.ToUInt64("0x04C11DB7", 16),
                                        0,
                                        0,
                                        true,
                                        true
                                    );

                                    var crc32 = new Crc(crcParams);

                                    var crc = crc32.CalculateAsNumeric(d.showFileRaw.ToArray());

                                    //Debug.Log($"Calculated CRC32: {crc:X8} for file with size: {d.showFileRaw.Count} bytes");

                                    //conver the CRC to a byte array
                                    byte[] crcBytes = BitConverter.GetBytes(crc);

                                    //ack with the CRC32 value
                                    sendMessage = new FTPMessage(
                                        (ushort)(ftpIncomingMessage.seq_number + 1),
                                        ftpIncomingMessage.session,
                                        FTPMessage.ftp_opcode.ACK,
                                        (byte)crcBytes.Length,
                                        ftpIncomingMessage.opcode,
                                        1,
                                        0,
                                        crcBytes
                                    );
                                    break;
                                case FTPMessage.ftp_opcode.ResetSessions:
                                    //reply with ack because we don't actually care about the file name
                                    sendMessage = new FTPMessage(
                                        (ushort)(ftpIncomingMessage.seq_number + 1),
                                        ftpIncomingMessage.session,
                                        FTPMessage.ftp_opcode.ACK,
                                        0,
                                        ftpIncomingMessage.opcode,
                                        1,
                                        0,
                                        null
                                    );
                                    break;
                                default:
                                    Debug.Log($"Unhandled FTP message: {ftpIncomingMessage.opcode}");
                                    break;
                            }

                            if (sendMessage != null)
                            {
                                d.SendFTPMessage(sendMessage.Value, message.sysid, message.compid);
                            }
                            break;
                        case MAVLINK_MSG_ID.LED_CONTROL:
                            var ledControl = (mavlink_led_control_t)message.data;

                            found = drones.TryGetValue(ledControl.target_system, out d);

                            if (!found)
                            {
                                Debug.LogError($"Invalid system target: {ledControl.target_system}");
                                continue;
                            }

                            //add to the flashing list
                            d.SetFlashing();
                            break;
                        default:
                            Debug.Log($"Unhandled message: {messageId}");
                            break;
                    }
                }

                await UniTask.Delay(1);
            }
        }

        public void ConstructUserInterface(RectTransform rect)
        {
            //add a button to export all drone positions for the shader static mode
            //this involves constructing a string of `float3(X, Y, Z),` for each drone
            //and then copying it to the clipboard

            var button = Util.AddButton(rect, "Copy Drone Positions To Clipboard")
            .WithCallback(() =>
            {
                StringBuilder sb = new StringBuilder();
                foreach (Drone d in drones.Values)
                {
                    Vector3 pos = d.GetDronePosition();
                    sb.AppendLine($"float3({pos.x}, {pos.y}, {pos.z}),");
                }
                //remove the last comma
                if (sb.Length > 0)
                {
                    sb.Length -= 1;
                }
                GUIUtility.systemCopyBuffer = sb.ToString();
            });
        }

        public void DeconstructUserInterface()
        {
            //throw new NotImplementedException();
        }

        public void UpdateUserInterface()
        {

        }
    }
}
