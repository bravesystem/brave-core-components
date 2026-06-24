using BRaVe_Biometric_Matching_Webjob.Extensions;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using BRaVe_Biometric_Matching_Webjob.Models;
using Microsoft.Identity.Client;
using Neurotec.Biometrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Services
{

    public class JobProcessor
    {

        public async Task<bool> ProcessAsync(IMatchingServerService matchingService, string jobId, CancellationToken ct)
        {
            try
            {
                // TODO: Implement your processing for the jobId
                Log("Processing jobId: " + jobId);

                // Simulate work
                await Task.Delay(TimeSpan.FromSeconds(1), ct);

                IBiometricService biometricService = new BiometricService();

                List<Biometric> biometrics = await biometricService.GetBiometrics(jobId, ct);

                List<BiometricMatch> biometricMatches = await FindMatchedBiometrics(matchingService, biometrics, batchSize: 256);

                // Group by Status
                var groupedByStatus = biometricMatches
                    .GroupBy(b => b.Status)
                    .ToDictionary(g => g.Key, g => g.ToList());

                //Process the ones to be removed (NBiometricStatus.Canceled) first from Matching server
                if (groupedByStatus != null && groupedByStatus.Keys.Contains(NBiometricStatus.Canceled))
                    ProcessMatchingResults(matchingService, biometricService, NBiometricStatus.Canceled, groupedByStatus[NBiometricStatus.Canceled]);

                //Process the remainings ( Status != NBiometricStatus.Canceled )
                foreach (var group in groupedByStatus)
                {
                    if (group.Key != NBiometricStatus.Canceled)
                    {
                        ProcessMatchingResults(matchingService, biometricService, group.Key, groupedByStatus[group.Key]);
                    }
                }

                //process the remaining

                // Return true on success; false to abandon/retry later
                return true;
            }
            catch (Exception ex)
            {
                LogError("Processing error for " + jobId + ": " + ex.Message);
                return false;
            }
        }

        private void ProcessMatchingResults(IMatchingServerService matchingService, IBiometricService biometricService, NBiometricStatus status, List<BiometricMatch> biometricMatches)
        {
            switch (status)
            {
                case NBiometricStatus.Canceled:
                    RemoveFromMatchingServer(matchingService, biometricMatches); //ok
                    BatchMarkAsReplaced(biometricService, biometricMatches);
                    break;
                case NBiometricStatus.DuplicateId:
                case NBiometricStatus.DuplicateFound:
                case NBiometricStatus.TooFewFeatures:
                    BatchMarkAsDuplicates(biometricService, status, biometricMatches);
                    break;
                case NBiometricStatus.MatchNotFound:
                    EnrollToMatchingServer(matchingService, biometricMatches);
                    BatchMarkAsEnrolled(biometricService, biometricMatches);
                    break;
                case NBiometricStatus.None:
                default:
                    LogAllUnknownStatuses(biometricService, biometricMatches);
                    break;
            }
        }

        private void LogAllUnknownStatuses(IBiometricService biometricService, List<BiometricMatch> biometricMatches)
        {
            biometricService.LogUnknownAllStatuses(biometricMatches);
        }

        private void BatchMarkAsReplaced(IBiometricService biometricService, List<BiometricMatch> biometricMatches)
        {
            biometricService.UpdateMatchStatuses(biometricMatches, NBiometricStatus.Canceled);
        }

        private void BatchMarkAsEnrolled(IBiometricService biometricService, List<BiometricMatch> biometricMatches)
        {
            biometricService.UpdateMatchStatuses(biometricMatches, NBiometricStatus.MatchNotFound);
        }

        private async void EnrollToMatchingServer(IMatchingServerService matchingService, List<BiometricMatch> _biometricMatches, int batchSize = 256)
        {
            List<BiometricMatch> biometricMatches = _biometricMatches.Where(b => b.MatchingAction != "V").ToList();

            int count = biometricMatches.Count;

            if (count < batchSize)
                batchSize = count;


            int success = 0, failure = 0;

            for (int k = 0; k < count; k += batchSize)
            {
                matchingService.clearTask(ServerTask.ENROLL);

                int upper = Math.Min(k + batchSize, count);

                for (int i = k; i < upper; i++)
                {
                    var subject = biometricMatches.ElementAt(i).Subject;

                    if (subject != null)
                        matchingService.AddSubject(ServerTask.ENROLL, subject);
                }

                do
                {
                    matchingService.PerformTask(ServerTask.ENROLL);

                    int retries = 0;

                    if (matchingService.getTaskError(ServerTask.ENROLL) != null)
                    {
                        LogError(matchingService.getTaskError(ServerTask.ENROLL).ToString());
                        retries++;
                    }

                    if (retries > 5)
                    {
                        LogError("Problem while enrolling templates to MegaMatcher.");
                        LogError("Enrollment task cancelled, too many retries..");
                        return;
                    }

                } while (matchingService.getTaskError(ServerTask.ENROLL) != null);

                NBiometricStatus
                    result = await matchingService.GetStatus(ServerTask.ENROLL);

                if (result == NBiometricStatus.Ok)
                {
                    foreach (var subject in await matchingService.GetSubjects(ServerTask.ENROLL))
                    {
                        LogError(string.Format("Subject Id-{0}, enrollement status: {1}", subject.Id, subject.Status.ToString()));

                        if (subject.Status == NBiometricStatus.Ok)
                            success++;
                        else
                            failure++;

                    }
                }
                else
                {
                    failure += (await matchingService.GetSubjects(ServerTask.ENROLL)).Count;
                }
            }

            LogError($"Enroll result: Success {success}, Failure {failure}");
        }

        private void BatchMarkAsDuplicates(IBiometricService biometricService, NBiometricStatus status, List<BiometricMatch> biometricMatches)
        {
            biometricService.UpdateMatchStatuses(biometricMatches, status);
        }

        private async void RemoveFromMatchingServer(IMatchingServerService matchingService, List<BiometricMatch> biometricMatches, int batchSize = 256)
        {

            int count = biometricMatches.Count;

            if (count < batchSize)
                batchSize = count;

            int success = 0, failure = 0;

            for (int k = 0; k < count; k += batchSize)
            {
                matchingService.clearTask(ServerTask.DELETE);

                int upper = Math.Min(k + batchSize, count);

                for (int i = k; i < upper; i++)
                {
                    var subject = new NSubject
                    {
                        Id = biometricMatches.ElementAt(i).Id
                    };

                    matchingService.AddSubject(ServerTask.DELETE, subject);
                }

                //matching.performDeleteTask();

                do
                {
                    matchingService.PerformTask(ServerTask.DELETE);

                    int retries = 0;

                    if (matchingService.getTaskError(ServerTask.DELETE) != null)
                    {
                        LogError(matchingService.getTaskError(ServerTask.DELETE).ToString());
                        retries++;
                    }

                    if (retries > 5)
                    {
                        LogError("Problem while identifying templates from MegaMatcher's database.");
                        LogError("Deletion task cancelled, too many retries..");
                    }

                } while (matchingService.getTaskError(ServerTask.DELETE) != null);

                NBiometricStatus result = await matchingService.GetStatus(ServerTask.DELETE);

                if (result == NBiometricStatus.Ok)
                {
                    foreach (var subject in await matchingService.GetSubjects(ServerTask.DELETE))
                    {
                        LogError(string.Format("Subject Id-{0}, Deletion status: {1}", subject.Id, subject.Status.ToString()));

                        if (subject.Status == NBiometricStatus.Ok)
                            success++;
                        else
                            failure++;
                    }
                }
                else
                {
                    failure += (await matchingService.GetSubjects(ServerTask.DELETE)).Count;
                }
            }


            LogError($"Deletion result: Success {success}, Failure {failure}");

        }

        private async Task<List<BiometricMatch>> FindMatchedBiometrics(IMatchingServerService matchingService, List<Biometric> templates, int batchSize = 256)
        {
            int count = templates.Count;

            if (count < batchSize)
                batchSize = count;

            List<BiometricMatch> biometricMatches = new List<BiometricMatch>();


            for (int k = 0; k < count; k += batchSize)
            {
                matchingService.ClearTask(ServerTask.IDENTIFY);

                int upper = Math.Min(k + batchSize, count);

                for (int i = k; i < upper; i++)
                {

                    if ("R".Equals(templates.ElementAt(i).MatchingAction, StringComparison.OrdinalIgnoreCase)) //to remove from MMA
                    {
                        biometricMatches.Add(new BiometricMatch
                        {
                            Id = templates.ElementAt(i).Id,
                            TenantId = templates.ElementAt(i).TenantId,
                            Status = NBiometricStatus.Canceled
                        });

                        continue;
                    }

                    Biometric biometric = templates.ElementAt(i);

                    var subject = await matchingService.CreateSubject(biometric);

                    if (subject != null)
                    {
                        subject.QueryString = $"TenantId={biometric.TenantId}";
                        await matchingService.AddSubject(ServerTask.IDENTIFY, subject);
                    }

                }

                do
                {
                    matchingService.PerformTask(ServerTask.IDENTIFY);

                    int retries = 0;

                    if (matchingService.getTaskError(ServerTask.IDENTIFY) != null)
                    {
                        LogError(matchingService.getTaskError(ServerTask.IDENTIFY).ToString());
                        retries++;
                    }

                    if (retries > 5)
                    {
                        LogError("Problem while identifying templates to MegaMatcher.");
                        LogError("Identification task cancelled, too many retries..");
                        throw new Exception();
                    }

                }
                while (matchingService.getTaskError(ServerTask.ENROLL) != null);



                foreach (var sbj in await matchingService.GetSubjects(ServerTask.IDENTIFY))
                {
                    BiometricMatch biometricMatch = new BiometricMatch
                    {
                        Id = sbj.Id,
                        TenantId = int.Parse(sbj.GetProperty("TenantId").ToString()),
                        MatchingAction = sbj.GetProperty("MatchingAction").ToString()
                    };

                    bool breakSecondLoop = false; //to break from second loop

                    if (sbj.Status == NBiometricStatus.Ok)
                    {
                        int score = matchingService.GetMinScore();

                        int best_lowest_score = 0;

                        NMatchingResult tmp = null;

                        bool firstHit = true;

                        foreach (var matchingResult in sbj.MatchingResults)
                        {
                            // keep_scores.Add(matchingResult.Score);

                            if (sbj.Id.Equals(matchingResult.Id))
                            {
                                biometricMatch.Status = NBiometricStatus.DuplicateId;
                                biometricMatch.MatchingResult = matchingResult;
                                biometricMatches.Add(biometricMatch);
                                Log("[INFO] Subject with the same ID found. Id : " + sbj.Id);
                                breakSecondLoop = true;
                                break;
                            }

                            if (firstHit && matchingResult.Score >= score)
                            {
                                score = matchingResult.Score;
                                biometricMatch.Status = NBiometricStatus.DuplicateFound;
                                biometricMatch.MatchingResult = matchingResult;
                                firstHit = false;
                                //Console.WriteLine($"[INFO] Match Found. Id : {sbj.Id}, Matching Id : {matchingResult.Id}, Score : {score}"); 
                                continue;
                            }
                            //var test = matchingResult.GetProperty("TenantId");
                            if (!firstHit && matchingResult.Score > score)
                            {
                                score = matchingResult.Score;
                                biometricMatch.Status = NBiometricStatus.DuplicateFound;
                                biometricMatch.MatchingResult = matchingResult;
                                //Console.WriteLine($"[INFO] Match Found. Id : {sbj.Id}, Matching Id : {matchingResult.Id}, Score : {score}");
                                continue;
                            }

                            if (best_lowest_score < matchingResult.Score && matchingResult.Score < score)
                            {
                                best_lowest_score = matchingResult.Score;
                                tmp = matchingResult;
                            }

                        }

                        if (breakSecondLoop)
                        {
                            breakSecondLoop = false; //to break from second loop
                            break;
                        }

                        if (best_lowest_score > 0)
                        {
                            biometricMatch.Status = NBiometricStatus.TooFewFeatures;
                            biometricMatch.MatchingResult = tmp;
                            biometricMatches.Add(biometricMatch);
                            Log($"[INFO] Match Found (Low score, likely Gender mismatch). Id : {sbj.Id}, Matching Id : {tmp.Id}, Score : {best_lowest_score}");
                            continue;
                        }

                        //very likely to happen, match found
                        if (!firstHit)
                        {
                            Log($"[INFO] Match Found. Id : {sbj.Id}, Matching Id : {biometricMatch.MatchingResult.Id}, Score : {score}");
                            biometricMatches.Add(biometricMatch);
                            continue;
                        }

                        //less likely to happen, match found but score too low
                        if (firstHit && sbj.Status == NBiometricStatus.TooFewFeatures)
                        {
                            Log($"[INFO] Match Found. Id : {sbj.Id}, Matching Id : {biometricMatch.MatchingResult.Id}, Score : {score}");
                            biometricMatch.Status = NBiometricStatus.TooFewFeatures;
                            biometricMatches.Add(biometricMatch);
                            continue;
                        }

                        Log($"[WARN] Match Found (Anomaly). Id, Status : {sbj.Id}, {sbj.Status}");

                        if (sbj.Status == NBiometricStatus.TooFewFeatures)
                            biometricMatch.Status = NBiometricStatus.None;
                        else
                            biometricMatch.Status = sbj.Status;

                        biometricMatches.Add(biometricMatch);

                    }
                    else if (sbj.Status == NBiometricStatus.MatchNotFound)
                    {
                        Log($"[INFO] Match NOT Found. Id : {sbj.Id}");
                        biometricMatch.Status = NBiometricStatus.MatchNotFound;
                        biometricMatch.Subject = sbj;
                        biometricMatches.Add(biometricMatch);
                        continue;
                    }
                    else
                    {
                        Log($"[WARN] Match NOT Found (Anomaly). Id, Status : {sbj.Id}, {sbj.Status}");

                        if (sbj.Status == NBiometricStatus.TooFewFeatures)
                            biometricMatch.Status = NBiometricStatus.None;
                        else
                            biometricMatch.Status = sbj.Status;

                        biometricMatches.Add(biometricMatch);
                    }

                }


            }

            return biometricMatches;

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
