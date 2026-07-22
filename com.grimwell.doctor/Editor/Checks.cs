using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Grimwell.Doctor
{
    public enum Status
    {
        Pass,
        Warn,
        Fail
    }

    public class CheckResult
    {
        public string Name;
        public Status Status;
        public string Message;
        public Action Fix; // null unless the check offers an automatic fix
    }

    public static class Checks
    {
        const string ExpectedUnityVersion = "6000.3.20f1";

        public static List<CheckResult> RunAll()
        {
            return new List<CheckResult>
            {
                CheckUnityVersion(),
                CheckGit(),
                CheckGitLfs(),
                CheckGitHubDesktop(),
                CheckTextSerialization(),
                CheckVisibleMetaFiles(),
                CheckProjectInGitRepo(),
            };
        }

        static CheckResult CheckUnityVersion()
        {
            var current = Application.unityVersion;
            if (current == ExpectedUnityVersion)
            {
                return new CheckResult
                {
                    Name = "UNITY VERSION",
                    Status = Status.Pass,
                    Message = "Running " + current + "."
                };
            }

            return new CheckResult
            {
                Name = "UNITY VERSION",
                Status = Status.Fail,
                Message = "You're on " + current + " — install " + ExpectedUnityVersion + " via Unity Hub"
            };
        }

        static CheckResult CheckGit()
        {
            if (TryRunVersion("git", "--version", out var output) ||
                TryRunVersion("/usr/bin/git", "--version", out output))
            {
                return new CheckResult
                {
                    Name = "GIT",
                    Status = Status.Pass,
                    Message = output.Trim()
                };
            }

            return new CheckResult
            {
                Name = "GIT",
                Status = Status.Fail,
                Message = "Git not found — install GitHub Desktop, it includes it"
            };
        }

        static CheckResult CheckGitLfs()
        {
            if (TryRunVersion("git", "lfs version", out var output) ||
                TryRunVersion("/usr/bin/git", "lfs version", out output))
            {
                return new CheckResult
                {
                    Name = "GIT LFS",
                    Status = Status.Pass,
                    Message = output.Trim()
                };
            }

            return new CheckResult
            {
                Name = "GIT LFS",
                Status = Status.Fail,
                Message = "Git LFS missing — install GitHub Desktop or `git lfs install`"
            };
        }

        static CheckResult CheckGitHubDesktop()
        {
            bool found;
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                found = Directory.Exists("/Applications/GitHub Desktop.app");
            }
            else
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                found = Directory.Exists(Path.Combine(localAppData, "GitHubDesktop"));
            }

            if (found)
            {
                return new CheckResult
                {
                    Name = "GITHUB DESKTOP",
                    Status = Status.Pass,
                    Message = "GitHub Desktop is installed."
                };
            }

            return new CheckResult
            {
                Name = "GITHUB DESKTOP",
                Status = Status.Warn,
                Message = "GitHub Desktop not found — recommended for sending/receiving changes"
            };
        }

        static CheckResult CheckTextSerialization()
        {
            if (EditorSettings.serializationMode == SerializationMode.ForceText)
            {
                return new CheckResult
                {
                    Name = "TEXT SERIALIZATION",
                    Status = Status.Pass,
                    Message = "Asset serialization is set to Force Text."
                };
            }

            return new CheckResult
            {
                Name = "TEXT SERIALIZATION",
                Status = Status.Fail,
                Message = "Asset serialization must be Force Text",
                Fix = () => EditorSettings.serializationMode = SerializationMode.ForceText
            };
        }

        static CheckResult CheckVisibleMetaFiles()
        {
            if (VersionControlSettings.mode == "Visible Meta Files")
            {
                return new CheckResult
                {
                    Name = "VISIBLE META FILES",
                    Status = Status.Pass,
                    Message = "Meta files are visible."
                };
            }

            return new CheckResult
            {
                Name = "VISIBLE META FILES",
                Status = Status.Fail,
                Message = "Version control mode must be Visible Meta Files",
                Fix = () => VersionControlSettings.mode = "Visible Meta Files"
            };
        }

        static CheckResult CheckProjectInGitRepo()
        {
            var dir = new DirectoryInfo(Application.dataPath).Parent;
            string repoRoot = null;
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    repoRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            if (repoRoot == null)
            {
                return new CheckResult
                {
                    Name = "PROJECT IN GIT REPO",
                    Status = Status.Warn,
                    Message = "Project isn't under version control yet"
                };
            }

            if (!File.Exists(Path.Combine(repoRoot, ".gitignore")))
            {
                return new CheckResult
                {
                    Name = "PROJECT IN GIT REPO",
                    Status = Status.Warn,
                    Message = "Repo found, but no .gitignore next to it — build artifacts may get committed"
                };
            }

            return new CheckResult
            {
                Name = "PROJECT IN GIT REPO",
                Status = Status.Pass,
                Message = "Project is under version control."
            };
        }

        static bool TryRunVersion(string fileName, string arguments, out string output)
        {
            output = null;
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using (var process = Process.Start(psi))
                {
                    output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(2000);
                    return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
