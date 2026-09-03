using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Versioning;
using Carubbi.Utils.Persistence;

namespace Carubbi.Communication.NamedPipe;

[SupportedOSPlatform("windows")]
public class Client<TRequestMessage, TResponseMessage> : IObservable<TResponseMessage>, IDisposable
    where TRequestMessage : class
    where TResponseMessage : class
{
    private readonly string _serverPipeName;
    private readonly string _callbackPipeName;
    private readonly string _serverPipePath;

    private readonly List<IObserver<TResponseMessage>> _subscribers;

    private NamedPipeClientStream? _serverPipe;
    private NamedPipeServerStream? _callbackPipe;

    private StreamWriter? _streamWriter;
    private StreamReader? _streamReader;
    private BackgroundWorker? _callbackBackgroundWorker;
    private int _messageCounter;

    public event EventHandler? BeforeConnect;
    public event EventHandler? AfterEnd;

    public Client(string processName, string? serverPipeName = null, string? callbackPipeName = null, string serverPipePath = ".")
    {
        _serverPipeName = serverPipeName ?? $"{processName}_SERVER_PIPE";
        _callbackPipeName = callbackPipeName ?? $"{processName}_CALLBACK_PIPE";
        _serverPipePath = serverPipePath;

        _subscribers = [];
    }

    public void Connect()
    {
        BeforeConnect?.Invoke(this, EventArgs.Empty);

        _serverPipe = new NamedPipeClientStream(_serverPipePath, _serverPipeName, PipeDirection.Out);
        _streamWriter = new StreamWriter(_serverPipe);

        _callbackPipe = new NamedPipeServerStream(_callbackPipeName, PipeDirection.In, 1);
        _streamReader = new StreamReader(_callbackPipe);

        StartCallbackListener();
    }

    public IDisposable Subscribe(IObserver<TResponseMessage> observer)
    {
        if (!_subscribers.Contains(observer))
        {
            _subscribers.Add(observer);
        }

        return new Unsubscriber<TResponseMessage>(_subscribers, observer);
    }

    public void SendRequest(List<TRequestMessage> requestMessages)
    {
        if (_serverPipe is null || _streamWriter is null)
        {
            throw new InvalidOperationException("The client must be connected before sending a request.");
        }

        if (!_serverPipe.IsConnected)
        {
            _serverPipe.Connect();
            _streamWriter.AutoFlush = true;
        }

        _messageCounter = requestMessages.Count;

        var requestSerializer = new Serializer<List<TRequestMessage>>();

        var serializedRequestMessages = requestSerializer.XmlSerialize(requestMessages);
        serializedRequestMessages = serializedRequestMessages.Replace(Environment.NewLine, string.Empty);

        _streamWriter.WriteLine(serializedRequestMessages);

        _serverPipe.WaitForPipeDrain();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _subscribers.Clear();

        _callbackBackgroundWorker?.CancelAsync();

        _serverPipe?.Close();
        _serverPipe?.Dispose();
        _serverPipe = null;

        if (_callbackPipe is not null)
        {
            if (_callbackPipe.IsConnected)
            {
                _callbackPipe.Disconnect();
            }

            _callbackPipe.Close();
            _callbackPipe.Dispose();
            _callbackPipe = null;
        }

        AfterEnd?.Invoke(this, EventArgs.Empty);
    }

    private void StartCallbackListener()
    {
        _callbackBackgroundWorker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        _callbackBackgroundWorker.DoWork += _callbackBackgroundWorker_DoWork;
        _callbackBackgroundWorker.RunWorkerCompleted += _callbackBackgroundWorker_RunWorkerCompleted;
        _callbackBackgroundWorker.RunWorkerAsync();
    }

    private void _callbackBackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        _callbackBackgroundWorker?.RunWorkerAsync();
    }

    private void _callbackBackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        while (_callbackBackgroundWorker is not null && !_callbackBackgroundWorker.CancellationPending)
        {
            if (_callbackPipe is null || _streamReader is null)
            {
                break;
            }

            if (!_callbackPipe.IsConnected)
            {
                _callbackPipe.WaitForConnection();
            }

            var serializedResponseMessage = _streamReader.ReadLine();

            if (serializedResponseMessage == null)
            {
                _callbackPipe.Disconnect();
                _callbackBackgroundWorker.CancelAsync();
                continue;
            }

            var responseSerializer = new Serializer<TResponseMessage>();
            var responseMessage = responseSerializer.XmlDeserialize(serializedResponseMessage);

            NotifyResponseMessage(responseMessage);

            _messageCounter--;

            if (_messageCounter == 0)
            {
                NotifyResponseEnd();
            }
        }

        if (_callbackPipe is not null && _callbackPipe.IsConnected)
        {
            _callbackPipe.Disconnect();
        }
    }

    private void NotifyResponseEnd()
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnCompleted();
        }
    }

    private void NotifyResponseMessage(TResponseMessage item)
    {
        foreach (var subscriber in _subscribers)
        {
            subscriber.OnNext(item);
        }
    }
}
