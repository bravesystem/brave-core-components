using BRaVe_Biometric_Matching_Webjob.Exceptions;
using BRaVe_Biometric_Matching_Webjob.Helpers;
using Neurotec.Biometrics;
using Neurotec.Biometrics.Client;
using Neurotec.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Models
{
    public class MatchingServer
    {
        private static ServerParams sp;

        private static MatchingServer instance = null;

        private static readonly object padlock = new object();

        NBiometricClient biometricClient = null;

        NBiometricTask identifyTask;
        NBiometricTask enrollTask;
        NBiometricTask deleteTask;

        MatchingServer()
        {
            sp = new ServerParams();

            sp.Host = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Node.ServerHost);

            string _params = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Node.ServerParams);

            Apply(_params, sp);
            
            biometricClient = GetBiometricClient();

        }

        public static MatchingServer Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new MatchingServer();
                    }
                    return instance;
                }
            }
        }

        public string getHost()
        { 
            return sp.Host; 
        }
        public int getClientPort()
        {
            return sp.ClientPort;
        }

        public int getMinScore()
        { 
            return sp.MinMatchScore;
        }

        private void Apply(string config, ServerParams sp)
        {
            if (string.IsNullOrWhiteSpace(config) || sp is null) return;

            // Split by comma, then by colon; handle spaces and case-insensitive keys
            var pairs = config.Split(',', (char)StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var kv = pair.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToUpperInvariant();
                var valText = kv[1].Trim();

                if (!int.TryParse(valText, out var val)) continue;

                switch (key)
                {
                    case "AP":        // Admin Port
                        sp.AdminPort = val;
                        break;

                    case "CP":        // Client Port
                        sp.ClientPort = val;
                        break;

                    case "MINSCORE":  // Minimum Match Score
                        sp.MinMatchScore = val;
                        break;

                    case "THRESH":    // Matching Threshold
                        sp.matchingThreshold = val;
                        break;

                    // Optional: add more aliases if needed (e.g., "THR")
                    // case "THR":
                    //     sp.matchingThreshold = val;
                    //     break;

                    default:
                        // Unknown key: ignore
                        break;
                }
            }
        }

        public NBiometricTask.SubjectCollection getSubjects(ServerTask serverTask)
        {
            switch (serverTask)
            {
                case ServerTask.IDENTIFY:
                    return identifyTask.Subjects;
                case ServerTask.ENROLL:
                    return enrollTask.Subjects;
                case ServerTask.DELETE:
                    return deleteTask.Subjects;
            }

            return null;
        }

        public NSubject CreateSubject(Biometric biometric)
        {
            byte[] b = biometric.Template;

            if (b.Length < 25) return null; //normal FPs have length gt 20

            var subject = new NSubject();
            subject.SetTemplateBuffer(new NBuffer(b));
            subject.Id = biometric.Id;
            subject.Gender = biometric.Gender == 1 ? NGender.Male :
                biometric.Gender == 2 ? NGender.Female : NGender.Unspecified;

            subject.Properties["TenantId"] = biometric.TenantId;
            subject.Properties["MatchingAction"] = biometric.MatchingAction;

            return subject;
        }

        public void addSubject(ServerTask serverTask, NSubject subject)
        {
            switch (serverTask)
            {
                case ServerTask.IDENTIFY:
                    identifyTask.Subjects.Add(subject);
                    break;
                case ServerTask.ENROLL:
                    enrollTask.Subjects.Add(subject);
                    break;
                case ServerTask.DELETE:
                    deleteTask.Subjects.Add(subject);
                    break;
            }

        }

        public void performTask(ServerTask serverTask)
        {
            switch (serverTask)
            {
                case ServerTask.IDENTIFY:
                    biometricClient.PerformTask(identifyTask);
                    break;
                case ServerTask.ENROLL:
                    biometricClient.PerformTask(enrollTask);
                    break;
                case ServerTask.DELETE:
                    biometricClient.PerformTask(deleteTask);
                    break;
            }

        }


        public void clearTask(ServerTask serverTask)
        {
            switch (serverTask)
            {
                case ServerTask.IDENTIFY:
                    identifyTask.Subjects.Clear();
                    break;
                case ServerTask.ENROLL:
                    enrollTask.Subjects.Clear();
                    break;
                case ServerTask.DELETE:
                    deleteTask.Subjects.Clear();
                    break;
            }
            
        }

        public NBiometricStatus getStatus(ServerTask serverTask)
        {
            switch (serverTask)
            {
                case ServerTask.IDENTIFY:
                    return identifyTask.Status;
                case ServerTask.ENROLL:
                    return enrollTask.Status;
                case ServerTask.DELETE:
                    return deleteTask.Status;
            }

            throw new MatchingErrors.TaskUnknown();

        }

        public Exception getTaskError(ServerTask serverTask)
        {
            switch (serverTask)
            {
                case ServerTask.IDENTIFY:
                    return identifyTask.Error;
                case ServerTask.ENROLL:
                    return enrollTask.Error;
                case ServerTask.DELETE:
                    return deleteTask.Error;
            }

            return new MatchingErrors.TaskUnknown();
        }

        private NBiometricClient GetBiometricClient()
        { 
            NBiometricClient client = new NBiometricClient();

            var conn = new NClusterBiometricConnection { Host = sp.Host, Port = sp.ClientPort };

            client.RemoteConnections.Add(conn);

            client.FingersMatchingSpeed = NMatchingSpeed.High;
            client.FingersTemplateSize = NTemplateSize.Large;
            client.MatchingThreshold = sp.matchingThreshold;

            //NBiometric Tasks
            identifyTask = client.CreateTask(NBiometricOperations.Identify, null);
            enrollTask = client.CreateTask(NBiometricOperations.Enroll, null);
            deleteTask = client.CreateTask(NBiometricOperations.Delete, null);

            return client;
        }



        
    }
}
