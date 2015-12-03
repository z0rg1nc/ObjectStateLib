using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BtmI2p.MiscUtils;
using NLog;

namespace BtmI2p.ObjectStateLib
{
    public class WrongDisposableObjectStateException : Exception
    {
        public EDisposableObjectState ActualState { get; set; }
    }
    public enum EDisposableObjectState
    {
        Created,
        Initialized,
        Disposing,
        Disposed
    }
    public class DisposableObjectStateHelper : IMyAsyncDisposable
    {
        public DisposableObjectStateHelper(
            string stateHelperId = ""
            )
        {
            _stateHelperId = stateHelperId;
        }
        
        private EDisposableObjectState _state = EDisposableObjectState.Created;

        public void SetInitializedState()
        {
            if (_state != EDisposableObjectState.Created)
                throw new WrongDisposableObjectStateException()
                {
                    ActualState = _state
                };
            _state = EDisposableObjectState.Initialized;
        }

        private int _runningFuncCounter = 0;
        private readonly ConcurrentDictionary<string,MutableTuple<int>> _methodIdCounters
            = new ConcurrentDictionary<string, MutableTuple<int>>();
        private void IncrementRunningFuncCounter(string methodId = "")
        {
            Interlocked.Increment(ref _runningFuncCounter);
            Interlocked.Increment(
                ref _methodIdCounters.GetOrAdd(
                    methodId,
                    x => MutableTuple.Create(0)
                ).Item1
            );
            if (_state != EDisposableObjectState.Initialized)
            {
                Interlocked.Decrement(ref _runningFuncCounter);
                Interlocked.Decrement(
                    ref _methodIdCounters.GetOrAdd(
                        methodId,
                        x => MutableTuple.Create(0)
                    ).Item1
                );
                throw new WrongDisposableObjectStateException()
                {
                    ActualState = _state
                };
            }
        }

        private void DecrementRunningFuncCounter(string methodId = "")
        {
            Interlocked.Decrement(
                ref _methodIdCounters.GetOrAdd(
                    methodId,
                    x => MutableTuple.Create(0)
                ).Item1
            );
            if (Interlocked.Decrement(ref _runningFuncCounter) <= 0)
                _zeroFuncTrigger.OnNext(null);
        }
        private readonly Subject<object> _zeroFuncTrigger 
            = new Subject<object>();
        private readonly string _stateHelperId;
        public IDisposable GetFuncWrapper(
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0,
#if DEBUG
            [CallerFilePath] 
#endif
            string fp = ""
        )
        {
            int fc = MiscFuncs.CheckStackFrameCount();
            string methodId = string.Format(
                "{0} {1} line {2} file {3}",
                fc,
                memberName,
                sourceLineNumber,
                fp
            );
            return new FuncWrapperDisposable(
                () => IncrementRunningFuncCounter(methodId),
                () => DecrementRunningFuncCounter(methodId),
                new FuncWrapperId()
                {
                    MethodId = methodId,
                    StateHelperId = _stateHelperId
                }
            );
        }
        
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public async Task MyDisposeAsync()
        {
            if (
                _state != EDisposableObjectState.Initialized
                && _state != EDisposableObjectState.Created
            )
                throw new WrongDisposableObjectStateException()
                {
                    ActualState = _state
                };
            _state = EDisposableObjectState.Disposing;
            Task zeroCounterTask =
                _zeroFuncTrigger.FirstAsync().ToTask();
            if (_runningFuncCounter == 0)
                return;
            /**/
            bool measureDisposingTime = _stateHelperId != "";
            var sw = new Stopwatch();
            if (measureDisposingTime)
            {
                sw.Start();
                _logger.Trace(
                    "StateHelper dispose enter {0}, counters - '{1}'", 
                    _stateHelperId,
                    _methodIdCounters.Where(x => x.Value.Item1 > 0).ToArray().WriteObjectToJson()
                );
            }
            await zeroCounterTask.ConfigureAwait(false);
            /**/
            _state = EDisposableObjectState.Disposed;
            if (measureDisposingTime)
            {
                _logger.Trace("StateHelper dispose leave {0}", _stateHelperId);
                sw.Stop();
                if (sw.ElapsedMilliseconds > 5000)
                {
                    _logger.Trace(
                        "StateHelper Dispose exceeds time limit id:{0}",
                        _stateHelperId
                    );
                }
            }
        }
    }
}