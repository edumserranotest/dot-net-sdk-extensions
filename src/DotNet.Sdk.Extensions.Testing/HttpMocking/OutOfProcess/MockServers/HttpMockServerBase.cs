﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DotNet.Sdk.Extensions.Testing.HttpMocking.OutOfProcess.MockServers
{
    public abstract class HttpMockServer : IAsyncDisposable
    {
        private readonly HttpMockServerArgs _mockServerArgs;
        private IHost? _host;

        internal HttpMockServer(HttpMockServerArgs mockServerArgs)
        {
            _mockServerArgs = mockServerArgs ?? throw new ArgumentNullException(nameof(mockServerArgs));
        }

        public async Task<List<HttpMockServerUrl>> StartAsync()
        {
            _host = CreateHostBuilder(_mockServerArgs.HostArgs).Build();
            await _host.StartAsync();
            return _host
                .GetServerAddresses()
                .Select(x => x.ToHttpMockServerUrl())
                .ToList();
        }

        protected abstract IHostBuilder CreateHostBuilder(string[] args);

        public async ValueTask DisposeAsync()
        {
            _host?.StopAsync();
            switch (_host)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }
}