﻿using System.Net;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

using Sulakore.Habbo;
using Sulakore.Network;
using Sulakore.Habbo.Packages;
using Sulakore.Network.Protocol;
using Sulakore.Network.Protocol.Formats;

namespace Sulakore.Modules;

public class TService : IModule
{
    public static IPEndPoint DefaultModuleServer { get; } = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055);

    private readonly TService _parent;
    private readonly IModule _container;
    private readonly List<DataCaptureAttribute> _unknownDataAttributes;
    private readonly Dictionary<ushort, List<DataCaptureAttribute>> _outDataAttributes, _inDataAttributes;

    public virtual bool IsStandalone { get; }

    private IInstaller _installer;
    public virtual IInstaller Installer
    {
        get => _parent?.Installer ?? _installer;
        set => _installer = value;
    }

    public Incoming In => Installer.In;
    public Outgoing Out => Installer.Out;

    public IGame Game => Installer.Game;
    public IHConnection Connection => Installer.Connection;

    private readonly IDictionary<int, HEntity> _entities;
    public ReadOnlyDictionary<int, HEntity> Entities { get; }

    private readonly IDictionary<int, HWallItem> _wallItems;
    public ReadOnlyDictionary<int, HWallItem> WallItems { get; }

    private readonly IDictionary<int, HFloorObject> _floorObjects;
    public ReadOnlyDictionary<int, HFloorObject> FloorObjects { get; }

    public TService(IModule container)
        : this(container, null, null)
    { }
    public TService(IModule container, IPEndPoint moduleServer)
        : this(container, null, moduleServer)
    { }

    protected TService()
        : this(null, null, null)
    { }
    protected TService(IPEndPoint moduleServer)
        : this(null, null, moduleServer)
    { }

    protected TService(TService parent)
        : this(null, parent, null)
    { }
    protected TService(TService parent, IPEndPoint moduleServer)
        : this(null, parent, moduleServer)
    { }

    private TService(IModule container, TService parent, IPEndPoint moduleServer)
    {
        _parent = parent;
        _container = container ?? this;
        _unknownDataAttributes = parent?._unknownDataAttributes ?? new List<DataCaptureAttribute>();
        _inDataAttributes = parent?._inDataAttributes ?? new Dictionary<ushort, List<DataCaptureAttribute>>();
        _outDataAttributes = parent?._outDataAttributes ?? new Dictionary<ushort, List<DataCaptureAttribute>>();

        _entities = new ConcurrentDictionary<int, HEntity>();
        Entities = new ReadOnlyDictionary<int, HEntity>(_entities);

        _wallItems = new ConcurrentDictionary<int, HWallItem>();
        WallItems = new ReadOnlyDictionary<int, HWallItem>(_wallItems);

        _floorObjects = new ConcurrentDictionary<int, HFloorObject>();
        FloorObjects = new ReadOnlyDictionary<int, HFloorObject>(_floorObjects);

        Installer = _container.Installer;
        IsStandalone = parent != null ? false : _container.IsStandalone;
        if (LicenseManager.UsageMode != LicenseUsageMode.Runtime) return;

        foreach (MethodInfo method in _container.GetType().GetAllMethods())
        {
            foreach (var dataCaptureAtt in method.GetCustomAttributes<DataCaptureAttribute>())
            {
                if (dataCaptureAtt == null) continue;

                dataCaptureAtt.Method = method;
                if (_unknownDataAttributes.Any(dca => dca.Equals(dataCaptureAtt))) continue;

                dataCaptureAtt.Target = _container;
                if (dataCaptureAtt.Id != null)
                {
                    AddCallback(dataCaptureAtt, (ushort)dataCaptureAtt.Id);
                }
                else _unknownDataAttributes.Add(dataCaptureAtt);
            }
        }

        if (!IsStandalone || (Assembly.GetAssembly(_container.GetType()) != Assembly.GetEntryAssembly())) return;
        while (true)
        {
            HNode installerNode = HNode.ConnectAsync(moduleServer ?? DefaultModuleServer).Result;
            if (installerNode != null)
            {
                using HPacket infoPacket = CreateModuleInfoPacket();
                installerNode.SendAsync(infoPacket).GetAwaiter().GetResult();
                Installer = _container.Installer = new RemoteInstaller(_container, installerNode);
                break;
            }
            else throw new Exception($"Failed to establish connection with the module server: {moduleServer}");
        }
    }

    public virtual void OnConnected() => ResolveMessageCallbacks();

    public virtual void HandleIncoming(DataInterceptedEventArgs e) => HandleData(_inDataAttributes, e);
    public virtual void HandleOutgoing(DataInterceptedEventArgs e) => HandleData(_outDataAttributes, e);
    private void HandleData(IDictionary<ushort, List<DataCaptureAttribute>> callbacks, DataInterceptedEventArgs e)
    {
        HandleGameObjects(e.Packet, e.IsOutgoing);
        if (callbacks.TryGetValue(e.Packet.Id, out List<DataCaptureAttribute> attributes))
        {
            foreach (DataCaptureAttribute attribute in attributes)
            {
                e.Packet.Position = 0;
                attribute.Invoke(e);
            }
        }
    }

    public Identifiers GetMessages(bool isOutgoing) => isOutgoing ? Out : In;
    public HMessage GetMessage(short id, bool isOutgoing) => GetMessages(isOutgoing)[id];
    public HMessage GetMessage(string name, bool isOutgoing) => GetMessages(isOutgoing)[name];

    private void ResolveMessageCallbacks()
    {
        var unresolved = new Dictionary<string, IList<string>>();
        foreach (PropertyInfo property in _container.GetType().GetAllProperties())
        {
            var messageAtt = property.GetCustomAttribute<MessageAttribute>();
            if (string.IsNullOrWhiteSpace(messageAtt?.Identifier)) continue;

            HMessage message = GetMessage(messageAtt.Identifier, messageAtt.IsOutgoing);
            if (message == default)
            {
                if (!unresolved.TryGetValue(messageAtt.Identifier, out IList<string> users))
                {
                    users = new List<string>();
                    unresolved.Add(messageAtt.Identifier, users);
                }
                users.Add($"Property({property.Name})");
            }
            else property.SetValue(_container, message);
        }
        foreach (DataCaptureAttribute dataCaptureAtt in _unknownDataAttributes)
        {
            if (string.IsNullOrWhiteSpace(dataCaptureAtt.Identifier)) continue;
            HMessage message = GetMessage(dataCaptureAtt.Identifier, dataCaptureAtt.IsOutgoing);
            if (message == default)
            {
                if (!unresolved.TryGetValue(dataCaptureAtt.Identifier, out IList<string> users))
                {
                    users = new List<string>();
                    unresolved.Add(dataCaptureAtt.Identifier, users);
                }
                users.Add($"Method({dataCaptureAtt.Method})");
            }
            else AddCallback(dataCaptureAtt, (ushort)message.Id);
        }
        if (unresolved.Count > 0)
        {
            throw new MessageResolvingException(Game.Revision, unresolved);
        }
    }
    private HPacket CreateModuleInfoPacket()
    {
        var packet = EvaWireFormat.CreatePacket(0);
        var moduleAssembly = Assembly.GetAssembly(_container.GetType());

        var description = string.Empty;
        string name = moduleAssembly.GetName().Name;
        var moduleAtt = _container.GetType().GetCustomAttribute<ModuleAttribute>();
        if (moduleAtt != null)
        {
            name = moduleAtt.Name;
            description = moduleAtt.Description;
        }
        packet.Write(moduleAssembly.GetName().Version.ToString());

        packet.Write(name);
        packet.Write(description);

        var authors = new List<AuthorAttribute>();
        var authorsAtts = _container.GetType().GetCustomAttributes<AuthorAttribute>();
        if (authorsAtts != null)
        {
            authors.AddRange(authorsAtts);
        }

        packet.Write(authors.Count);
        foreach (AuthorAttribute author in authors)
        {
            packet.Write(author.Name);
        }
        return packet;
    }
    private void HandleGameObjects(ref HReadOnlyPacket packet, bool isOutgoing)
    {
        packet.Position = 0;
        if (!isOutgoing)
        {
            if (packet.Id == In.Users)
            {
                HEntity[] entities = HEntity.Parse(ref packet);
                foreach (HEntity entity in entities)
                {
                    _entities[entity.Index] = entity;
                }
                //_container.OnEntitiesLoaded(entities.Length);
            }
            else if (packet.Id == In.Items)
            {
                HWallItem[] wallItems = HWallItem.Parse(ref packet);
                foreach (HWallItem wallItem in wallItems)
                {
                    _wallItems[wallItem.Id] = wallItem;
                }
                //_container.OnWallItemsLoaded(wallItems.Length);
            }
            else if (packet.Id == In.Objects)
            {
                HFloorObject[] floorObjects = HFloorObject.Parse(ref packet);
                foreach (HFloorObject floorObject in floorObjects)
                {
                    _floorObjects[floorObject.Id] = floorObject;
                }
                //_container.OnFloorObjectsLoaded(floorObjects.Length);
            }
            else if (packet.Id == In.FloorHeightMap)
            {
                _entities.Clear();
                _wallItems.Clear();
                _floorObjects.Clear();
            }
        }
        packet.Position = 0;
    }
    private void AddCallback(DataCaptureAttribute attribute, ushort id)
    {
        Dictionary<ushort, List<DataCaptureAttribute>> callbacks = attribute.IsOutgoing ? _outDataAttributes : _inDataAttributes;
        if (!callbacks.TryGetValue(id, out List<DataCaptureAttribute> attributes))
        {
            attributes = new List<DataCaptureAttribute>();
            callbacks.Add(id, attributes);
        }
        attributes.Add(attribute);
    }

    public virtual void Dispose()
    {
        _inDataAttributes.Clear();
        _outDataAttributes.Clear();
        _unknownDataAttributes.Clear();
    }

    private sealed class RemoteInstaller : IInstaller, IHConnection
    {
        private delegate void DataEvent(ref HReadOnlyPacket packet);

        private readonly IModule _module;
        private readonly HNode _installerNode;
        private readonly Dictionary<ushort, DataEvent> _moduleEvents;

        HNode IHConnection.Local => throw new NotSupportedException();
        HNode IHConnection.Remote => throw new NotSupportedException();

        public Incoming In { get; private set; }
        public Outgoing Out { get; private set; }

        public IGame Game { get; set; }
        public IHConnection Connection => this;

        public RemoteInstaller(IModule module, HNode installerNode)
        {
            _module = module;
            _installerNode = installerNode;
            _moduleEvents = new Dictionary<ushort, DataEvent>
            {
                [1] = HandleData,
                [2] = HandleOnConnected
            };
            _ = HandleInstallerDataAsync();
        }

        private void HandleData(ref HReadOnlyPacket packet)
        {
            int step = packet.Read<int>();
            bool isOutgoing = packet.Read<bool>();
            //TODO: var format = HFormat.GetFormat(packet.Read<string>());
            bool canContinue = packet.Read<bool>();

            int ogDataLength = packet.Read<int>();
            byte[] ogData = packet.ReadBytes(ogDataLength);
            var args = new DataInterceptedEventArgs(format.CreatePacket(ogData), step, isOutgoing, ContinueAsync);

            bool isOriginal = packet.Read<bool>();
            if (!isOriginal)
            {
                int packetLength = packet.Read<int>();
                byte[] packetData = packet.ReadBytes(packetLength);
                args.Packet = format.CreatePacket(packetData);
            }

            try
            {
                if (isOutgoing)
                {
                    _module.HandleOutgoing(args);
                }
                else
                {
                    _module.HandleIncoming(args);
                }
            }
            catch
            {
                if (args.IsOriginal != isOriginal) // Was this packet modified before throwing an error?
                {
                    args.Restore();
                }
            }

            if (!args.WasRelayed)
            {
                _installerNode.SendAsync(CreateHandledDataPacket(args, false));
            }
        }
        private void HandleOnConnected(ref HReadOnlyPacket packet)
        {
            int messagesJsonDataLength = packet.Read<int>();
            byte[] messagesJsonData = packet.ReadBytes(messagesJsonDataLength);
            // TODO: Deserialize into the In, and Out properties

            _module.OnConnected();
        }

        private async Task HandleInstallerDataAsync()
        {
            try
            {
                HPacket packet = await _installerNode.ReceiveRentedPacketAsync().ConfigureAwait(false);
                if (packet == null) Environment.Exit(0);

                Task handleInstallerDataTask = HandleInstallerDataAsync();
                if (_moduleEvents.TryGetValue(packet.Id, out Action<HPacket> handler))
                {
                    handler(packet);
                }
            }
            catch { Environment.Exit(0); }
        }
        private async Task ContinueAsync(DataInterceptedEventArgs args)
        {
            HPacket handledDataPacket = CreateHandledDataPacket(args, true);
            await _installerNode.SendAsync(handledDataPacket).ConfigureAwait(false);
        }

        public ValueTask<int> SendToClientAsync(byte[] data)
        {
            return _installerNode.SendAsync(2, false, data.Length, data);
        }
        public ValueTask<int> SendToClientAsync(HPacket packet)
        {
            return SendToClientAsync(packet.ToBytes());
        }
        public ValueTask<int> SendToClientAsync(ushort id, params object[] values)
        {
            return SendToClientAsync(EvaWirePacket.Construct(id, values));
        }

        public ValueTask<int> SendToServerAsync(byte[] data)
        {
            return _installerNode.SendAsync(2, true, data.Length, data);
        }
        public ValueTask<int> SendToServerAsync(HPacket packet)
        {
            return SendToServerAsync(packet.ToBytes());
        }
        public ValueTask<int> SendToServerAsync(ushort id, params object[] values)
        {
            return SendToServerAsync(EvaWirePacket.Construct(id, values));
        }

        private HPacket CreateHandledDataPacket(DataInterceptedEventArgs args, bool isContinuing)
        {
            var handledDataPacket = EvaWireFormat.CreatePacket(1);
            handledDataPacket.Write(args.Step + args.IsOutgoing.ToString());

            handledDataPacket.Write(isContinuing);
            if (isContinuing)
            {
                handledDataPacket.Write(args.WasRelayed);
            }
            else
            {
                byte[] packetData = args.Packet.ToBytes();
                handledDataPacket.Write(packetData.Length);
                handledDataPacket.Write(packetData);
                handledDataPacket.Write(args.IsBlocked);
            }
            return handledDataPacket;
        }
    }
}