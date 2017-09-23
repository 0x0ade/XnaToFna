using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public static class FileSystemHelper {

        public readonly static bool MONO_IOMAP_ALL = Environment.GetEnvironmentVariable("MONO_IOMAP") == "all";

        // '/' is invalid in file and directory names on Windows.
        public readonly static char[] DirectorySeparatorChars = { '/', '\\' };

        // Right now, XnaToFna doesn't make use of this... but it should.
        public static string FixPath(string path) {
            // Can't trust File.Exists if MONO_IOMAP_ALL is set.
            if (!MONO_IOMAP_ALL && (Directory.Exists(path) || File.Exists(path)))
                return path;

            string[] pathSplit = path.Split(DirectorySeparatorChars);

            StringBuilder builder = new StringBuilder();

            bool unixRooted = false;

            if (Path.IsPathRooted(path)) {
                // The first element in a rooted path will always be correct.
                // On Windows, this will be the drive letter.
                // On Unix and Unix-like systems, this will be empty.
                if (unixRooted = (builder.Length == 0))
                    // Path is rooted, but the path separator is the root.
                    builder.Append(Path.DirectorySeparatorChar);
                else
                    builder.Append(pathSplit[0]);
            }

            for (int i = 1; i < pathSplit.Length; i++) {
                string[] possible;
                if (i < pathSplit.Length - 1)
                    possible = Directory.GetDirectories(builder.ToString());
                else
                    possible = Directory.GetFileSystemEntries(builder.ToString());

                // Add proper / fixed directory separator after getting list of possibilities.
                if (i != 1 || !unixRooted)
                    builder.Append(Path.DirectorySeparatorChar);

                // Fix case sensitivity, add next element.
                string next = pathSplit[i];
                for (int pi = 0; pi < possible.Length; pi++) {
                    string possibleName = Path.GetFileName(possible[pi]);
                    if (string.Equals(pathSplit[i], possibleName, StringComparison.InvariantCultureIgnoreCase)) {
                        next = possibleName;
                        break;
                    }
                }
                builder.Append(next);
            }

            return builder.ToString();
        }

    }
}
