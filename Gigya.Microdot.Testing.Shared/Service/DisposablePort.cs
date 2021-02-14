﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Gigya.Microdot.SharedLogic;

namespace Gigya.Microdot.Testing.Shared.Service
{
    public class DisposablePort : IDisposable
    {
        public readonly int Port;
        private readonly List<Semaphore> _semaphores = new List<Semaphore>(4);
        private static readonly ConcurrentDictionary<Semaphore, DateTime> PortMaintainer = new ConcurrentDictionary<Semaphore, DateTime>();

        private DisposablePort(int port)
        {
            Port = port;
        }

        public void Dispose()
        {
            foreach (var x in _semaphores)
            {
                try
                {
                    PortMaintainer.TryRemove(x, out _);
                    x.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(value: $"Failed to dispose the port sequence {Port}: {ex.Message}");
                }
            }

            Console.WriteLine($"Disposed port sequence: {Port}");
        }

        private static HashSet<int> Occupied()
        {
            var ipGlobal = IPGlobalProperties.GetIPGlobalProperties();
            var occupied = new List<int>();
            occupied.AddRange(ipGlobal.GetActiveTcpConnections().Select(x => x.LocalEndPoint.Port));
            occupied.AddRange(ipGlobal.GetActiveTcpListeners().Select(x => x.Port));
            occupied.AddRange(ipGlobal.GetActiveUdpListeners().Select(x => x.Port));
            return new HashSet<int>(occupied.Distinct());
        }

        public static DisposablePort GetPort()
        {
            return GetPort(retries: 10000, rangeFrom: 49152, rangeTo: 65535, sequence: Enum.GetValues(typeof(PortOffsets)).Length);
        }

        /// <summary>
        /// Find a non-occupied sequence of ports in range [from, to].
        /// </summary>
        /// <param name="retries">How many time to look into </param>
        /// <param name="rangeFrom">Min value of port</param>
        /// <param name="rangeTo">Max value of port</param>
        /// <param name="sequence">How many ports sequentially we need to allocate</param>
        private static DisposablePort GetPort(int retries, int rangeFrom, int rangeTo, int sequence)
        {
            uint totalNewSemExceptions = 0u;
            var sw = Stopwatch.StartNew();
            var random = new Random(Guid.NewGuid().GetHashCode());

            for (int retry = 0; retry < retries; retry++)
            {
                var occupiedPorts = Occupied(); // work on up-to-date list of ports in every retry

                var randomPort = random.Next(rangeFrom, rangeTo);

                // Check the every port in the sequence isn't occupied
                bool freeRange = true;
                for (int port = randomPort; port < randomPort + sequence; port++)
                {
                    freeRange = !occupiedPorts.Contains(port);
                    if (!freeRange)
                        break;
                }

                bool someOneElseWantThisPort = false;

                if (freeRange)
                {
                    // We need to avoid race condition between different App Domains and processes running in 
                    // parallel and allocating the same port, especially the tests running in parallel.
                    // The semaphore is machine / OS wide, so the hope it is good enough.

                    var result = new DisposablePort(randomPort);

                    for (int port = randomPort; port < randomPort + sequence; port++)
                    {
                        var name = $"ServiceTester-{port}";

                        //if (Semaphore.TryOpenExisting(name, out var _))
                        //{
                        //    someOneElseWantThisPort = true;
                        //}
                        //else
                        //{
                        //    try
                        //    {
                        //        var item = new Semaphore(1, 1, name);
                        //        result._semaphores.Add(item);
                        //        PortMaintainer.TryAdd(item, DateTime.UtcNow);
                        //        if (port == randomPort)
                        //        {
                        //            IsHttpSysLocked(port);
                        //        }
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        Console.WriteLine($"Failed to create semaphore for port: {port}, Exception: " + e.Message);
                        //        someOneElseWantThisPort = true;
                        //        totalNewSemExceptions++;
                        //        result.Dispose(); // also freeing already created semaphores
                        //    }
                        //}
                        try
                        {
                            var item = new Semaphore(1, 1, name);
                            result._semaphores.Add(item);
                            PortMaintainer.TryAdd(item, DateTime.UtcNow);
                            if (port == randomPort)
                            {
                                IsHttpSysLocked(port);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to create semaphore for port: {port}, Exception: " + e.Message);
                            someOneElseWantThisPort = true;
                            totalNewSemExceptions++;
                            result.Dispose(); // also freeing already created semaphores
                        }
                    }



                    if (someOneElseWantThisPort == false)
                    {
                        Console.WriteLine($"Service Tester found a free port: {randomPort}. " +
                                          $"After retries: {retry}. " +
                                          $"Initially occupied ports: {occupiedPorts.Count}. " +
                                          $"Port maintainer contains: {PortMaintainer.Count}. " +
                                          $"New semaphore exceptions: {totalNewSemExceptions}. " +
                                          $"Total elapsed, ms: {sw.ElapsedMilliseconds}");
                        return result;
                    }
                }
            }

            throw new Exception($"Can't find free port in range: [{rangeFrom}-{rangeTo}]." +
                                $"Retries: {retries}. " +
                                $"Currently occupied ports: {Occupied().Count}. " +
                                $"Port maintainer contains: {PortMaintainer.Count}. " +
                                $"New semaphore exceptions: {totalNewSemExceptions}. " +
                                $"Total elapsed, ms: {sw.ElapsedMilliseconds}." +
                                $"Process id: {Environment.ProcessId}");
        }

        private static void IsHttpSysLocked(int port, bool https = false)
        {
            var urlPrefixTemplate = https ? "https://+:{0}/" : "http://+:{0}/";
            var prefix = string.Format(urlPrefixTemplate, port);

            var listener = new System.Net.HttpListener
            {
                IgnoreWriteExceptions = true,
                Prefixes = { prefix }
            };

            listener.Start();

            Thread.SpinWait(1);
            listener.Stop();

        }
    }
}