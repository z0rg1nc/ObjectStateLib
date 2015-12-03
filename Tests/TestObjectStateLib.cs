using System;
using System.Threading.Tasks;
using BtmI2p.ObjectStateLib;
using Xunit;

namespace BtmI2p.TestObjectStateLibNs
{
    public class TestObjectStateLibFixture : IDisposable
    {
        public TestObjectStateLibFixture()
        {
            _stateHelper.SetInitializedState();
        }

        public async Task Func1()
        {
            using ( _stateHelper.GetFuncWrapper())
            {
                await Task.Delay(2000).ConfigureAwait(false);
            }
        }
        public async Task Func2()
        {
            using (_stateHelper.GetFuncWrapper())
            {
                await Task.Delay(2000).ConfigureAwait(false);
            }
        }
        public async Task Func3()
        {
            using (_stateHelper.GetFuncWrapper())
            {
                await Task.Delay(2000).ConfigureAwait(false);
            }
        }
        private readonly DisposableObjectStateHelper _stateHelper
            = new DisposableObjectStateHelper("TestObjectStateLibFixture");
        public void Dispose()
        {
            _stateHelper.MyDisposeAsync().Wait();
        }
    }

    public class TestObjectStateLib : IClassFixture<TestObjectStateLibFixture>
    {
        [Fact]
        public async Task Test1()
        {
            await _fixtureData.Func1().ConfigureAwait(false);
            await _fixtureData.Func2().ConfigureAwait(false);
            await _fixtureData.Func3().ConfigureAwait(false);
        }

        private TestObjectStateLibFixture _fixtureData;
        public void SetFixture(TestObjectStateLibFixture data)
        {
            _fixtureData = data;
        }
    }
}
