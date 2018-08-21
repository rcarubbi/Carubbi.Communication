using System;
using System.Collections.Generic;

namespace Carubbi.Communication.NamedPipe
{
    public  class Unsubscriber<TDtoOutput> : IDisposable
        where TDtoOutput : class
    {
        private readonly List<IObserver<TDtoOutput>> _observers;
        private readonly IObserver<TDtoOutput> _observer;

        internal Unsubscriber(List<IObserver<TDtoOutput>> observers, IObserver<TDtoOutput> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}
