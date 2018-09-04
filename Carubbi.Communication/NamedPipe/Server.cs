﻿using Carubbi.Utils.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Carubbi.Communication.NamedPipe
{
    public abstract class Server<TRequestMessage, TResponseMessage>
        where TRequestMessage : class
    {
        public int IdleSeconds { get; set; }

        private readonly BackgroundWorker _listeningPipeBackgroundWorker;
        private readonly BackgroundWorker _keepAliveBackgroundWorker;

        private readonly NamedPipeServerStream _serverPipe;
        private readonly NamedPipeClientStream _callbackPipe;

        private readonly StreamReader _streamReader;
        private readonly StreamWriter _streamWriter;

        private DateTime _lastRequestDateTime;
        private bool _isKeepAliveRunning;

        public void Start()
        {
            BeforeStart();

            _lastRequestDateTime = DateTime.Now;

            InitKeepAlive();
            StartListening();
        }

        protected Server(string serverPipeName, string callbackPipeName, string callbackClientPath = ".")
        {
            _listeningPipeBackgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _keepAliveBackgroundWorker = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

            _serverPipe = new NamedPipeServerStream(serverPipeName, PipeDirection.In, 1);
            _streamReader = new StreamReader(_serverPipe);

            _callbackPipe = new NamedPipeClientStream(callbackClientPath, callbackPipeName, PipeDirection.Out);
            _streamWriter = new StreamWriter(_callbackPipe);
        }

        protected abstract TResponseMessage RunService(TRequestMessage requestMessage);

        protected abstract void BeforeStart();

        protected abstract void KeepAlive();

        protected virtual void StartListening()
        {
            _listeningPipeBackgroundWorker.DoWork += _listeningPipeBackgroundWorker_DoWork;
            _listeningPipeBackgroundWorker.RunWorkerCompleted += _listeningPipeBackgroundWorker_RunWorkerCompleted;
            _listeningPipeBackgroundWorker.RunWorkerAsync();
        }

        private void _listeningPipeBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _listeningPipeBackgroundWorker.RunWorkerAsync();
        }

        private void _listeningPipeBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_listeningPipeBackgroundWorker.CancellationPending)
            {
                if (!_serverPipe.IsConnected)
                    _serverPipe.WaitForConnection();

                var serializedRequestMessages = _streamReader.ReadLine();

                if (serializedRequestMessages == null)
                {
                    _serverPipe.Disconnect();
                    _listeningPipeBackgroundWorker.CancelAsync();
                    continue;
                }

                var requestSerializer = new Serializer<List<TRequestMessage>>();
                var requestMessages = requestSerializer.XmlDeserialize(serializedRequestMessages);

                foreach (var requestMessage in requestMessages)
                {
                    while (_isKeepAliveRunning)
                    {
                        Thread.Sleep(200);
                    }
                    var responseMessage = RunService(requestMessage);
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
            var responseSerializer = new Serializer<TResponseMessage>();
            var serializedResponseMessage = responseSerializer.XmlSerialize(responseMessage)
                .Replace(Environment.NewLine, string.Empty);          

            if (!_callbackPipe.IsConnected)
            {
                _callbackPipe.Connect();
                _streamWriter.AutoFlush = true;
            } 

            _streamWriter.WriteLine(serializedResponseMessage);
            _callbackPipe.WaitForPipeDrain();
        }
         
        private void InitKeepAlive()
        {
            _keepAliveBackgroundWorker.DoWork += _keepAliveBackgroundWorker_DoWork; ;
            _keepAliveBackgroundWorker.RunWorkerAsync();
        }

        private void _keepAliveBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_keepAliveBackgroundWorker.CancellationPending)
            {
                if (_lastRequestDateTime.AddSeconds(IdleSeconds) >= DateTime.Now) continue;

                _isKeepAliveRunning = true;

                KeepAlive();

                _lastRequestDateTime = DateTime.Now;
                _isKeepAliveRunning = false;
            }
        }
    }
}
