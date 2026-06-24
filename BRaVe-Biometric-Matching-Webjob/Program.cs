using BRaVe_Biometric_Matching_Webjob.Exceptions;
using BRaVe_Biometric_Matching_Webjob.Helpers;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using BRaVe_Biometric_Matching_Webjob.Models;
using BRaVe_Biometric_Matching_Webjob.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob
{
    internal class Program
    {
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);

        static int Main(string[] args)
        {
            Console.Title = "Biometric Matching Worker (.NET 4.5)";
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Log("Shutdown requested…");
                cts.Cancel();
            };

            try
            {
                Log("Biometric Matching Worker | Build date: June 4th, 2026");

                // Choose the queue type you use (Storage or Service Bus)
                IJobQueue queue = CreateQueueFromConfig(); // pick one below

                IMatchingServerService server = new MatchingServerService();

                var processor = new JobProcessor();

                RunAsync(server, queue, processor, cts.Token).Wait();
                return 0;
            }
            catch (MatchingErrors.PingError e)
            {
                LogError(e.Message);
                return 1;
            }
            catch (MatchingErrors.TcpConnectionError e)
            {
                LogError(e.Message);
                return 1;
            }
            catch (Exception ex)
            {
                LogError("Fatal error: " + ex);
                return 1;
            }
        }

        private static IJobQueue CreateQueueFromConfig()
        {
            return new AzureServiceBusQueue();
        }

        private static async Task RunAsync(
            IMatchingServerService server,
            IJobQueue queue,
            JobProcessor processor,
            CancellationToken ct)
        {
            Log("Starting worker…");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 1) Check matching server status
                    var isUp = await server.IsHealthyAsync(ct);
                    if (!isUp)
                    {
                        Log("[WARN] Matching server is DOWN. Retrying in a few seconds…");
                        await Task.Delay(IdleDelay, ct);
                        continue;
                    }

                    // 2) Try to get a jobId from the queue
                    var jobId = await queue.TryGetNextJobIdAsync(ct);

                    if (jobId == null)
                    {
                        // Nothing to do; small idle delay to avoid tight loop
                        //Console.WriteLine("[INFO] No Job Found");
                        await Task.Delay(IdleDelay, ct);
                        continue;
                    }

                    Log("[INFO] Received jobId: " + jobId);

                    // 3) Process the job
                    var success = await processor.ProcessAsync(server, jobId, ct);

                    // 4) Complete or abandon message
                    if (success)
                    {
                        await queue.CompleteAsync(ct);
                        Log("[INFO] Job completed: " + jobId);
                    }
                    else
                    {
                        await queue.AbandonAsync(ct);
                        Log("[WARN] Job failed (abandoned): " + jobId);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    LogError("[ERROR] Loop error: " + ex.Message);
                    // small backoff
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }

            Log("Worker stopped.");
        }

        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

        private static void LogError(string message)
        {
            Console.Error.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }

    }
}
