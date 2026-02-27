using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using ArtNet.Devices;
using ArtNet.IO;
using ArtNet.Packets;
using UnityEngine;

namespace ArtNet
{
    public class DmxManager : MonoBehaviour
    {
        private readonly Queue<ushort> _updatedUniverses = new();
        private Dictionary<ushort, byte[]> DmxDictionary { get; } = new();
        public Dictionary<ushort, IEnumerable<IDmxDevice>> DmxDevices { get; private set; }

        public ArtNetReceiver ArtNetReceiver;
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public void Update()
        {
            lock (_updatedUniverses)
            {
                while (0 < _updatedUniverses.Count)
                {
                    var universe = _updatedUniverses.Dequeue();
                    var dmx = DmxDictionary[universe];
                    DmxDevices.TryGetValue(universe, out var devices);
                    if (devices == null) continue;
                    foreach (var device in devices)
                    {
                        var deviceDmx = new byte[device.ChannelNumber];
                        Buffer.BlockCopy(dmx, device.StartAddress, deviceDmx, 0, device.ChannelNumber);
                        device.DmxUpdate(deviceDmx);
                    }
                }
            }
        }

        public void OnEnable()
        {
            DmxDevices = FindDmxDevices();
        }

        private static Dictionary<ushort, IEnumerable<IDmxDevice>> FindDmxDevices()
        {
            return FindObjectsOfType<GameObject>().SelectMany(o => o.GetComponents<IDmxDevice>())
                .GroupBy(device => device.Universe).ToDictionary(g => g.Key, g => g as IEnumerable<IDmxDevice>);
        }

        public ushort[] Universes()
        {
            return DmxDictionary.Keys.ToArray();
        }

        public byte[] DmxValues(ushort universe)
        {
            return DmxDictionary.TryGetValue(universe, out var data) ? data : new byte[512];
        }

        public void ReceivedDmxPacket(ReceivedData<DmxPacket> receivedData)
        {
            var packet = receivedData.Packet;
            var universe = packet.Universe;
            if (!DmxDictionary.ContainsKey(universe)) DmxDictionary.Add(universe, packet.Dmx);
            Buffer.BlockCopy(packet.Dmx, 0, DmxDictionary[universe], 0, 512);
            lock (_updatedUniverses)
            {
                if (_updatedUniverses.Contains(universe)) return;
                _updatedUniverses.Enqueue(universe);
            }
        }

        public void ReceivedPollPacket(ReceivedData<PollPacket> receivedData)
        {
            var packet = receivedData.Packet;
            //unclear what flags and priority are for
            //Debug.Log(packet.Flags);
            //Debug.Log(packet.Priority);

            byte status = 0b11110000;

            //resolve the IP. if its 0.0.0.0, then just use localhost
            byte[] ipBytes = ArtNetReceiver.Address.GetAddressBytes();
            if (ArtNetReceiver.Address.Equals(IPAddress.Any))
            {
                ipBytes = IPAddress.Loopback.GetAddressBytes();
            }

            //construct a response packet
            var responsePacket = new PollReplyPacket
            {
                IpAddress = ipBytes,
                Port = ArtNetReceiver.Port,
                //OUR version number. Just report 69 because its funny
                VersionInfo = 0x0000,
                NetSwitch = 0,
                SubSwitch = 0,
                Oem = 0,
                UbeaVersion = 0,
                Status1 = status,
                EstaCode = 0,
                ShortName = "HNode",
                LongName = "HNode by Happyrobot33",
                NodeReport = "All systems functional",
                NumPorts = ushort.MaxValue,
                PortTypes = new byte[] { 0x45, 0x45, 0x45, 0x45 },
                InputStatus = new byte[] { 0x0,0x0,0x0,0x0},
                OutputStatus = new byte[] { 0x0,0x0,0x0,0x0},
                InputSubSwitch = new byte[] { 0, 0, 0, 0 },
                OutputSubSwitch = new byte[] { 0, 0, 0,0 },
                SwVideo = 0,
                SwMacro = 0,
                SwRemote = 0,
                Spares = new byte[3],
                Style = 0,
                MacAddress = new byte[6],
                BindIpAddress = new byte[4],
                BindIndex = 0,
            };

            //send this over the network to the current port and broadcast address
            var stream = new MemoryStream();
            var writer = new ArtNetWriter(stream);
            writer.Write(responsePacket.ToByteArray());

            writer.Flush();

            //send over udp directly back to the requesting endpoint
            //the port SHOULD change because polls actually do move with the port changing
            Debug.Log($"Responding to poll from: {receivedData.RemoteEp}");
            _socket.SendTo(stream.ToArray(), receivedData.RemoteEp);
        }
    }
}
