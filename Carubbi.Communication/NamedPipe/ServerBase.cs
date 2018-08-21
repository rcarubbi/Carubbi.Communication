using Carubbi.Utils.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows.Forms;

namespace Carubbi.Communication.NamedPipe
{
    public abstract class ServerBase<TEntradaServico, TSaidaServico> : Form
        where TEntradaServico : class
    {
        public int SegundosOcioso { get; set; }

        private readonly BackgroundWorker _bgEscutarPipe;
        private readonly BackgroundWorker _bgStillAlive;
        private readonly NamedPipeServerStream _serverPipe;
        private readonly StreamReader _sr;

        private readonly NamedPipeClientStream _callbackPipe;
        private readonly StreamWriter _sw;

        private static DateTime _dataUltimaRequisicao;
        private bool _stillAliveEmExecucao;

        protected abstract TSaidaServico ExecutarServico(TEntradaServico entradaServico);

        protected abstract void Login();

        protected abstract void StillAlive();

        protected ServerBase(string nomePipeInput, string nomePipeOutput)
        {
            _bgEscutarPipe = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _bgStillAlive = new BackgroundWorker { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
            _serverPipe = new NamedPipeServerStream(nomePipeInput, PipeDirection.In, 1);
            _sr = new StreamReader(_serverPipe);
            _callbackPipe = new NamedPipeClientStream(".", nomePipeOutput, PipeDirection.Out);
            _sw = new StreamWriter(_callbackPipe);
            
            Shown += ServerBase_Shown;
        }

        private void ServerBase_Shown(object sender, EventArgs e)
        {
            Login();
            _dataUltimaRequisicao = DateTime.Now;
            InicializarStillAlive();
            EscutarNamedPipe();
        }

        protected virtual void EscutarNamedPipe()
        {
            _bgEscutarPipe.DoWork += bgEscutarPipe_DoWork;
            _bgEscutarPipe.RunWorkerCompleted += _bgEscutarPipe_RunWorkerCompleted;
            _bgEscutarPipe.RunWorkerAsync();
        }

        protected void _bgEscutarPipe_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _bgEscutarPipe.RunWorkerAsync();
        }

        protected void bgEscutarPipe_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_bgEscutarPipe.CancellationPending)
            {
                if (!_serverPipe.IsConnected)
                    _serverPipe.WaitForConnection();

                var msg = _sr.ReadLine();

                if (msg == null )
                {
                    _serverPipe.Disconnect();
                    _bgEscutarPipe.CancelAsync();
                    continue;
                }

                var serializadorInput = new Serializer<List<TEntradaServico>>();
                var lote = serializadorInput.XmlDeserialize(msg);
                
                foreach (var item in lote)
                {
                    while (_stillAliveEmExecucao)
                    {
                        Thread.Sleep(200);
                    }
                    var output = ExecutarServico(item);
                    _dataUltimaRequisicao = DateTime.Now;
                    CallBack(output);
                }
               
            }

            if (_serverPipe.IsConnected)
            {
                _serverPipe.Disconnect();
            }
        }

        private void CallBack(TSaidaServico retorno)
        {
            var serializadorOutput = new Serializer<TSaidaServico>();
            var mensagemCallBack = serializadorOutput.XmlSerialize(retorno).Replace(Environment.NewLine, string.Empty);          

            if (!_callbackPipe.IsConnected)
            {
                _callbackPipe.Connect();
                _sw.AutoFlush = true;
            } 

            _sw.WriteLine(mensagemCallBack);
            _callbackPipe.WaitForPipeDrain();
        }
         
        private void InicializarStillAlive()
        {
            _bgStillAlive.DoWork += _bgStillAlive_DoWork;
            _bgStillAlive.RunWorkerAsync();
        }

        protected void _bgStillAlive_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_bgStillAlive.CancellationPending)
            {
                if (_dataUltimaRequisicao.AddSeconds(SegundosOcioso) >= DateTime.Now) continue;

                _stillAliveEmExecucao = true;

                StillAlive();

                _dataUltimaRequisicao = DateTime.Now;
                _stillAliveEmExecucao = false;
            }
        }
    }
}
