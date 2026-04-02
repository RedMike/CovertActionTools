using System.IO;

namespace CovertActionTools.IntegrationTests.Core.Parsers.Data
{
    internal static class ExecutableTestDataGenerator
    {
        public static readonly string[] KnownExecutables = { "BUG", "CHASE", "CODE", "FINAL", "GAME", "TAC" };

        /// <summary>
        /// Attempts to locate the scratch folder containing original EXE files.
        /// Returns null if not found.
        /// </summary>
        public static string FindScratchDirectory()
        {
            // Walk up from the test output directory to find the repo root
            var dir = Directory.GetCurrentDirectory();
            for (var i = 0; i < 10; i++)
            {
                var candidate = Path.Combine(dir, "scratch");
                if (Directory.Exists(candidate))
                {
                    // Verify at least one known EXE exists
                    if (File.Exists(Path.Combine(candidate, "TAC.EXE")))
                    {
                        return candidate;
                    }
                }

                var parent = Directory.GetParent(dir);
                if (parent == null)
                {
                    break;
                }
                dir = parent.FullName;
            }

            return null;
        }

        /// <summary>
        /// Gets the path to a specific EXE file in the scratch directory.
        /// Returns null if the scratch directory or file is not found.
        /// </summary>
        public static string GetExePath(string name)
        {
            var scratchDir = FindScratchDirectory();
            if (scratchDir == null)
            {
                return null;
            }

            var path = Path.Combine(scratchDir, $"{name}.EXE");
            return File.Exists(path) ? path : null;
        }
    }
}
