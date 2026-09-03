using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Threading;
using Carubbi.Communication.Serialization;

namespace Carubbi.Communication.NamedPipe;

[SupportedOSPlatform("windows")]
public abstract class Server<TRequestMessage, TResponseMessage>
    where TRequestMessage : class
    where TResponseMessage : class
{
    private readonly BackgroundWorker _listeningPipeBackgroundWorker;
    private readonly BackgroundWorker _keepAliveBackgroundWorker;

    private readonly IMessageSerializer<List<TRequestMessage>> _requestSerializer;
    private readonly IMessageSerializer<TResponseMessage> _responseSerializer;

    private readonly NamedPipeServerStream _serverPipe;
    private readonly NamedPipeClientStream _callbackPipe;

    private DateTime _lastRequestDateTime;
    private bool _isKeepAliveRunning;

    public int IdleSeconds { get; set; }

    protected Server(
        string processName,
        string? serverPipeName = null,
        string? callbackPipeName = null,
        string callbackClientPath = ".",
        MessageFormat format = MessageFormat.Xml,
        PipeSecurity? serverPipeSecurity = null)
        : this(
            processName,
            MessageSerializerFactory.Create<List<TRequestMessage>>(format),
            MessageSerializerFactory.Create<TResponseMessage>(format),
            serverPipeName,
            callbackPipeName,
            callbackClientPath,
            serverPipeSecurity)
    {
    }

    protected Server(
        string processName,
        IMessageSerializer<List<TRequestMessage>> requestSerializer,
        IMessageSerializer<TResponseMessage> responseSerializer,
        string? serverPipeName = null,
        string? callbackPipeName = null,
        string callbackClientPath = ".",
        PipeSecurity? serverPipeSecurity = null)
    {
        var serverPipeNameValue = serverPipeName ?? $"{processName}_SERVER_PIPE";
        var callbackPipeNameValue = callbackPipeName ?? $"{processName}_CALLBACK_PIPE";

        _requestSerializer = requestSerializer;
        _responseSerializer = responseSerializer;

        var security = serverPipeSecurity ?? NamedPipeSecurity.AllowEveryone();

        _listeningPipeBackgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
        _keepAliveBackgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

        _serverPipe = NamedPipeServerStreamAcl.Create(
            serverPipeNameValue,
            PipeDirection.In,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.None,
            0,
            0,
            security,
            HandleInheritability.None,
            PipeAccessRights.ReadWrite);

        _callbackPipe = new NamedPipeClientStream(callbackClientPath, callbackPipeNameValue, PipeDirection.Out);
    }

    public void Start()
    {
        BeforeStart();

        _lastRequestDateTime = DateTime.Now;

        InitKeepAlive();
        StartListening();
    }

    protected abstract TResponseMessage ProcessRequest(TRequestMessage requestMessage);

    protected abstract void BeforeStart();

    protected virtual void KeepAlive()
    {
    }

    private void StartListening()
    {
        _listeningPipeBackgroundWorker.DoWork += _listeningPipeBackgroundWorker_DoWork;
        _listeningPipeBackgroundWorker.RunWorkerCompleted += _listeningPipeBackgroundWorker_RunWorkerCompleted;
        _listeningPipeBackgroundWorker.RunWorkerAsync();
    }

    private void _listeningPipeBackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        _listeningPipeBackgroundWorker.RunWorkerAsync();
    }

    private void _listeningPipeBackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        while (!_listeningPipeBackgroundWorker.CancellationPending)
        {
            if (!_serverPipe.IsConnected)
            {
                _serverPipe.WaitForConnection();
            }

            byte[] payload;
            try
            {
                payload = PipeFraming.ReadFrame(_serverPipe);
            }
            catch (IOException)
            {
                break;
            }

            var requestMessages = _requestSerializer.Deserialize(payload);

            foreach (var requestMessage in requestMessages)
            {
                while (_isKeepAliveRunning)
                {
                    Thread.Sleep(200);
                }

                var responseMessage = ProcessRequest(requestMessage);
                _lastRequestDateTime = DateTime.Now;
                CallBack(responseMessage);
            }
        }

        if (_serverPipe.IsConnected)
        {
            _serverPipe.Disconnect();
        }
    }

    private void CallBack(TResponseMessage responseMessage)
    {
        if (!_callbackPipe.IsConnected)
        {
            _callbackPipe.Connect();
        }

        var payload = _responseSerializer.Serialize(responseMessage);
        PipeFraming.WriteFrame(_callbackPipe, payload);
        _callbackPipe.WaitForPipeDrain();
    }

    private void InitKeepAlive()
    {
        if (!MethodOverridden(nameof(KeepAlive)))
        {
            return;
        }

        _keepAliveBackgroundWorker.DoWork += _keepAliveBackgroundWorker_DoWork;
        _keepAliveBackgroundWorker.RunWorkerAsync();
    }

    private bool MethodOverridden(string methodName)
    {
        var type = GetType();
        var method = type.GetMethod(methodName, BindingFlags.Instance);
        if (method == null)
        {
            return false;
        }

        var declaringTypeName = method.DeclaringType?.FullName;

        return declaringTypeName != null && declaringTypeName.Equals(type.FullName, StringComparison.OrdinalIgnoreCase);
    }

    private void _keepAliveBackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        while (!_keepAliveBackgroundWorker.CancellationPending)
        {
            if (_lastRequestDateTime.AddSeconds(IdleSeconds) >= DateTime.Now)
            {
                continue;
            }

            _isKeepAliveRunning = true;

            KeepAlive();

            _lastRequestDateTime = DateTime.Now;
            _isKeepAliveRunning = false;
        }
    }
}
