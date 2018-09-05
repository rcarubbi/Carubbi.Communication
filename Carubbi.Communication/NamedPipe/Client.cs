using Carubbi.Utils.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;

namespace Carubbi.Communication.NamedPipe
{
    public abstract class Client<TRequestMessage, TResponseMessage> : IObservable<TResponseMessage>, IDisposable
        where TRequestMessage : class
        where TResponseMessage : class
    {
        private readonly List<IObserver<TResponseMessage>> _subscribers;

        private readonly NamedPipeClientStream _serverPipe;
        private readonly NamedPipeServerStream _callbackPipe;

        private readonly StreamWriter _streamWriter;
        private readonly StreamReader _streamReader;

        private BackgroundWorker _callbackBackgroundWorker;
        private int _messageCounter;
        
        protected Client(string processName, string serverPipePath = ".", string serverPipeName = null, string callbackPipeName = null)
        {
            var defaultServerPipeName = $"{processName}_SERVER_PIPE";
            var defaultCallbackPipeName = $"{Guid.NewGuid()}_CALLBACK_PIPE";

            _serverPipe = new NamedPipeClientStream(serverPipePath, serverPipeName ?? defaultServerPipeName, PipeDirection.Out);
            _streamWriter = new StreamWriter(_serverPipe);

            _callbackPipe = new NamedPipeServerStream(callbackPipeName ?? defaultCallbackPipeName, PipeDirection.In, 1);
            _streamReader = new StreamReader(_callbackPipe);

            _subscribers = new List<IObserver<TResponseMessage>>();

            BeforeStart();
   
            StartCallbackListener();
        }

        public IDisposable Subscribe(IObserver<TResponseMessage> subscriber)
        {
            if (!_subscribers.Contains(subscriber))
            {
                _subscribers.Add(subscriber);
            }

            _subscribers.Add(subscriber);

            return new Unsubscriber<TResponseMessage>(_subscribers, subscriber);
        }

        public void SendRequest(List<TRequestMessage> requestMessages)
        {
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
            _subscribers.Clear();
            _callbackBackgroundWorker.CancelAsync();

            _serverPipe.Close();
            _serverPipe.Dispose();

            if (_callbackPipe.IsConnected)
                _callbackPipe.Disconnect();

            _callbackPipe.Close();
            _callbackPipe.Dispose();

            AfterEnd();
        }

        protected abstract void BeforeStart();

        protected abstract void AfterEnd();

        private void StartCallbackListener()
        {
            _callbackBackgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _callbackBackgroundWorker.DoWork += _callbackBackgroundWorker_DoWork;
            _callbackBackgroundWorker.RunWorkerCompleted += _callbackBackgroundWorker_RunWorkerCompleted; ;
            _callbackBackgroundWorker.RunWorkerAsync();
        }

        private void _callbackBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _callbackBackgroundWorker.RunWorkerAsync();
        }

        private void _callbackBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_callbackBackgroundWorker.CancellationPending)
            {
                if (!_callbackPipe.IsConnected)
                    _callbackPipe.WaitForConnection();

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
                    NotifyResponseEnd();
            }

            if (_callbackPipe.IsConnected)
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

        ~Client()
        {
            Dispose();
        }
    }
}