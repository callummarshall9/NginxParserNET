using System;
using System.Diagnostics;

namespace LinuxPrivilegeEscalation
{

    public enum AuthenticationSchemes
    {
        PasswordInSudoCommand,
        PolicyKitExecution
    }
    
    public class LinuxPrivilegeEscalation
    {
        public AuthenticationSchemes Scheme { get; set; }
        public string PolicyKitExecPath { get; set; }
        public string SudoPath { get; set; }
        
        public string EchoPath { get; set; }
        
        public LinuxPrivilegeEscalation(AuthenticationSchemes scheme, string policyKitPath=null, string sudoPath=null, string echoPath=null)
        {
            Scheme = scheme;
            PolicyKitExecPath = policyKitPath ?? "/usr/bin/pkexec";
            SudoPath = sudoPath ?? "/usr/bin/sudo";
            EchoPath = echoPath ?? "/usr/bin/echo";
        }

        public void EscalateSignedInUser(string command, string password=null)
        {
            if (Scheme == AuthenticationSchemes.PolicyKitExecution)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = PolicyKitExecPath,
                    Arguments = command,
                    WorkingDirectory = "/usr/bin/",
                    RedirectStandardOutput = true
                })
                    ?.WaitForExit();

            } else if (Scheme == AuthenticationSchemes.PasswordInSudoCommand)
            {
                Process.Start(new ProcessStartInfo
                    {
                        FileName = PolicyKitExecPath,
                        Arguments = command,
                        WorkingDirectory = "/usr/bin/",
                        RedirectStandardOutput = true
                    })
                    ?.WaitForExit();
            }
        }
    }
}