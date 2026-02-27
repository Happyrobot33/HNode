using System;
using System.Net;
using ArtNet.Common;
using ArtNet.Enums;
using ArtNet.IO;
using ArtNet.Packets;
using UnityEngine;
using UnityEngine.Events;

namespace ArtNet
{
    [Serializable]
    internal class OnReceivedDmxEvent : UnityEvent<ReceivedData<DmxPacket>>
    {
    }

    [Serializable]
    internal class OnReceivedPollEvent : UnityEvent<ReceivedData<PollPacket>>
    {
    }

    [Serializable]
    internal class OnReceivedPollReplyEvent : UnityEvent<ReceivedData<PollReplyPacket>>
    {
    }

    public class ArtNetReceiver : MonoBehaviour
    {
        public const int DefaultArtNetPort = 6454;

        [SerializeField] private bool _autoStart = true;
        [SerializeField] private OnReceivedDmxEvent _onReceivedDmxEvent;
        [SerializeField] private OnReceivedPollEvent _onReceivedPollEvent;
        [SerializeField] private OnReceivedPollReplyEvent _onReceivedPollReplyEvent;

        private UdpReceiver UdpReceiver = new(DefaultArtNetPort);
        public DateTime LastReceivedAt { get; private set; }
        public bool IsConnected => LastReceivedAt.AddSeconds(1) > DateTime.Now;

        private void Awake()
        {
            //setup default artnet receiver
            //UdpReceiver = new(ArtNetPort);
            UdpReceiver.OnReceivedPacket = OnReceivedPacket;
            ChangePort(DefaultArtNetPort);
        }

        public void ChangePort(int port)
        {
            //check if port is different
            ArtNetLogger.LogInfo("ArtNetReceiver", $"Changing port from {UdpReceiver.Port} to {port}");
            UdpReceiver.ChangePort(port);
        }

        public ushort Port => (ushort)UdpReceiver.Port;
        public IPAddress Address => UdpReceiver.Address;

        public void ChangeIPAddress(IPAddress address)
        {
            ArtNetLogger.LogInfo("ArtNetReceiver", $"Changing IP Address from {UdpReceiver.Address} to {address}");
            UdpReceiver.ChangeIPAddress(address);
        }

        private void OnEnable()
        {
            if (_autoStart) UdpReceiver.StartReceive();
        }

        private void OnDisable()
        {
            UdpReceiver.StopReceive();
        }

        private void OnReceivedPacket(byte[] receiveBuffer, int length, EndPoint remoteEp)
        {
            var packet = ArtNetPacket.Create(receiveBuffer);
            if (packet == null) return;
            LastReceivedAt = DateTime.Now;

            switch (packet.OpCode)
            {
                case OpCode.Dmx:
                    _onReceivedDmxEvent?.Invoke(ReceivedData<DmxPacket>(packet, remoteEp));
                    break;
                case OpCode.Poll:
                    _onReceivedPollEvent.Invoke(ReceivedData<PollPacket>(packet, remoteEp));
                    break;
                case OpCode.PollReply:
                    _onReceivedPollReplyEvent.Invoke(ReceivedData<PollReplyPacket>(packet, remoteEp));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static ReceivedData<TPacket> ReceivedData<TPacket>(ArtNetPacket netPacket, EndPoint endPoint)
            where TPacket : ArtNetPacket
        {
            return new ReceivedData<TPacket>(netPacket as TPacket, endPoint);
        }
    }
}
