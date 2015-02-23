using Redis2Go;
using System;

namespace RedisBus.IntegrationTest
{
    public class RedisFixture : IDisposable
    {
        private readonly RedisRunner _runner;

        public RedisFixture()
        {
            this._runner = RedisRunner.StartForDebugging();
        }

        public void Dispose()
        {
            this._runner.Dispose();
        }
    }
}
