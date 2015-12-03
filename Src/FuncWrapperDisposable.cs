using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace BtmI2p.ObjectStateLib
{
    public class FuncWrapperId
    {
        public string StateHelperId = "";
        public string MethodId = "";
        public override string ToString()
        {
            return StateHelperId + " " + MethodId;
        }
    }
    public class FuncWrapperDisposable : IDisposable
    {
        private readonly Action _decrementAction;
        public readonly FuncWrapperId FuncWrapperId;
        private static readonly ConcurrentDictionary<long, FuncWrapperDisposable> _funcWrapperDb
            = new ConcurrentDictionary<long, FuncWrapperDisposable>();

        public static List<FuncWrapperDisposable> FuncWrapperDb
        {
            get { return new List<FuncWrapperDisposable>(_funcWrapperDb.Values); }
        }
        private static long _nextFuncWrapperNum = 0;

        public FuncWrapperDisposable(
            Action incrementAction,
            Action decrementAction,
            FuncWrapperId funcWrapperId
        )
        {
            _decrementAction = decrementAction;
            FuncWrapperId = funcWrapperId;
            incrementAction();
            _wrapperNum = Interlocked.Increment(ref _nextFuncWrapperNum);
            EnterTime = DateTime.UtcNow;
            _funcWrapperDb.TryAdd(_wrapperNum, this);
        }

        public readonly DateTime EnterTime;
        private readonly long _wrapperNum;
        public void Dispose()
        {
            _decrementAction();
            FuncWrapperDisposable temp;
            _funcWrapperDb.TryRemove(_wrapperNum, out temp);
        }
    }
}
