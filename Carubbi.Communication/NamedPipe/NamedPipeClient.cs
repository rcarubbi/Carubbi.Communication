using Carubbi.Utils.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Web;

namespace Carubbi.Communication.NamedPipe
{
    public class NamedPipeClient<TDtoInput, TDtoOutput, TDtoCredenciais> : IObservable<TDtoOutput>, IDisposable
        where TDtoInput : class
        where TDtoOutput : class
        where TDtoCredenciais : class
    {
        private readonly List<IObserver<TDtoOutput>> _observers;
        private BackgroundWorker _bgCallback;
        private readonly NamedPipeClientStream _clientPipe;
        private readonly NamedPipeServerStream _callbackPipe;
        private readonly StreamWriter _sw;
        private readonly StreamReader _sr;
        private readonly string _nomeIntegracao;
        private int _loteCount;

        public NamedPipeClient(string nomePipeInput, string nomePipeOutput, TDtoCredenciais credenciais, string nomeIntegracao)
        {
            _clientPipe = new NamedPipeClientStream(".", nomePipeInput, PipeDirection.Out);
            _callbackPipe = new NamedPipeServerStream(nomePipeOutput, PipeDirection.In, 1);
            _sw = new StreamWriter(_clientPipe);
            _sr = new StreamReader(_callbackPipe);
            _observers = new List<IObserver<TDtoOutput>>();
            _nomeIntegracao = nomeIntegracao;
            InicializarServidor(credenciais);
            InicializarCallbackListener();
        }

        ~NamedPipeClient()
        {
            Dispose();
        }

        private void InicializarServidor(TDtoCredenciais credenciais)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ConfigurationManager.AppSettings[$"CAMINHO_SERVIDOR_{_nomeIntegracao}"]
            };

            if (ServidorEmExecucao) return;

            var serializador = new Serializer<TDtoCredenciais>();
            startInfo.Arguments = string.Concat("\"", HttpUtility.HtmlEncode(serializador.XmlSerialize(credenciais).Replace(Environment.NewLine, string.Empty)), "\"");
            startInfo.CreateNoWindow = true;

            Process.Start(startInfo);
        }

        private Process RecuperarProcessoServidor()
        {
            return ServidorEmExecucao 
                ? Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ConfigurationManager.AppSettings[$"CAMINHO_SERVIDOR_{_nomeIntegracao}"]))[0] 
                : null;
        }

        public bool ServidorEmExecucao => Process.GetProcessesByName(Path.GetFileNameWithoutExtension(
                                              ConfigurationManager.AppSettings[$"CAMINHO_SERVIDOR_{_nomeIntegracao}"]
                                          )).Length > 0;

        private void InicializarCallbackListener()
        {
            _bgCallback = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _bgCallback.DoWork += _bgCallback_DoWork;
            _bgCallback.RunWorkerCompleted += _bgCallback_RunWorkerCompleted;
            _bgCallback.RunWorkerAsync();
        }

        protected void _bgCallback_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _bgCallback.RunWorkerAsync();
        }

        protected void _bgCallback_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_bgCallback.CancellationPending)
            {
                if (!_callbackPipe.IsConnected)
                    _callbackPipe.WaitForConnection();

                var msg = _sr.ReadLine();

                if (msg == null)
                {
                    _callbackPipe.Disconnect();
                    _bgCallback.CancelAsync();
                    continue;
                }

                var serializador = new Serializer<TDtoOutput>();
                var item = serializador.XmlDeserialize(msg);

                NotificarItem(item);

                _loteCount--;

                if (_loteCount == 0)
                    NotificarFinalProcessamentoLote();
            }

            if (_callbackPipe.IsConnected)
            {
                _callbackPipe.Disconnect();
            }
        }

        private void NotificarFinalProcessamentoLote()
        {
            foreach (var observer in _observers)
            {
                observer.OnCompleted();
            }

        }

        private void NotificarItem(TDtoOutput item)
        {
            foreach (var observer in _observers)
            {
                observer.OnNext(item);
            }
        }

        public IDisposable Subscribe(IObserver<TDtoOutput> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            _observers.Add(observer);

            return new Unsubscriber<TDtoOutput>(_observers, observer);
        }

        public void Enviar(List<TDtoInput> lote)
        {
            if (!_clientPipe.IsConnected)
            {
                _clientPipe.Connect();
                _sw.AutoFlush = true;
            }

            _loteCount = lote.Count;

            var serializador = new Serializer<List<TDtoInput>>();
            var msg = serializador.XmlSerialize(lote);
            msg = msg.Replace(Environment.NewLine, string.Empty);
            _sw.WriteLine(msg);

            _clientPipe.WaitForPipeDrain();
        }

        public void Dispose()
        {
            _observers.Clear();
            _bgCallback.CancelAsync();

            _clientPipe.Close();
            _clientPipe.Dispose();

            if (_callbackPipe.IsConnected)
                _callbackPipe.Disconnect();

            _callbackPipe.Close();
            _callbackPipe.Dispose();

            EncerrarServidor();
        }

        private void EncerrarServidor()
        {
            if (ServidorEmExecucao)
            {
                RecuperarProcessoServidor().Kill();
            }
        }

        public object Serializer { get; set; }
    }
}
