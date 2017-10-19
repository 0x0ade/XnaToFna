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

        // This will eat memory, but it may be worth the tradeoff.
        private static Dictionary<string, string[]> _CachedDirectories = new Dictionary<string, string[]>();
        private static Dictionary<string, string[]> _CachedTargets = new Dictionary<string, string[]>();
        private static Dictionary<char, Dictionary<string, string>> _CachedChanges = new Dictionary<char, Dictionary<string, string>>();

        public static string[] GetDirectories(string path) {
            string[] results;
            if (_CachedDirectories.TryGetValue(path, out results))
                return results;
            try {
                results = Directory.GetDirectories(path);
            } catch {
                results = null;
            }
            _CachedDirectories[path] = results;
            return results;
        }

        public static string[] GetTargets(string path) {
            string[] results;
            if (_CachedTargets.TryGetValue(path, out results))
                return results;
            try {
                results = Directory.GetFileSystemEntries(path);
            }
            catch {
                results = null;
            }
            _CachedTargets[path] = results;
            return results;
        }

        public static string GetDirectory(string path, string next) => GetNext(GetDirectories(path), next);
        public static string GetTarget(string path, string next) => GetNext(GetTargets(path), next);
        public static string GetNext(string[] possible, string next) {
            if (possible == null)
                return null;
            for (int pi = 0; pi < possible.Length; pi++) {
                string possibleName = Path.GetFileName(possible[pi]);
                if (string.Equals(next, possibleName, StringComparison.InvariantCultureIgnoreCase))
                    return possibleName;
            }
            return null;
        }

        public static string FixPath(string path) => ChangePath(path, Path.DirectorySeparatorChar);
        public static string BreakPath(string path) => ChangePath(path, '\\');
        public static string ChangePath(string path, char separator) {
            // Can't trust File.Exists if MONO_IOMAP_ALL is set.
            if (!MONO_IOMAP_ALL) {
                string pathMaybe = path;
                // Check if target exists in the first place.
                if (Directory.Exists(path) || File.Exists(path))
                    return pathMaybe;

                // Try a simpler fix first: Maybe the casing is already correct...
                pathMaybe = path.Replace('/', separator).Replace('\\', separator);
                if (Directory.Exists(pathMaybe) || File.Exists(pathMaybe))
                    return pathMaybe;

                // Fall back to the slow rebuild.
            }

            // Check if the path has been rebuilt before.
            Dictionary<string, string> cachedPaths;
            if (!_CachedChanges.TryGetValue(separator, out cachedPaths))
                _CachedChanges[separator] = cachedPaths = new Dictionary<string, string>();
            string cachedPath;
            if (cachedPaths.TryGetValue(path, out cachedPath))
                return cachedPath;

            // Split and rebuild path.

            string[] pathSplit = path.Split(DirectorySeparatorChars);

            StringBuilder builder = new StringBuilder();

            bool unixRooted = false;

            if (Path.IsPathRooted(path)) {
                // The first element in a rooted path will always be correct.
                // On Windows, this will be the drive letter.
                // On Unix and Unix-like systems, this will be empty.
                if (unixRooted = (builder.Length == 0))
                    // Path is rooted, but the path separator is the root.
                    builder.Append(separator);
                else
                    builder.Append(pathSplit[0]);
            }

            for (int i = 1; i < pathSplit.Length; i++) {
                string next;
                if (i < pathSplit.Length - 1)
                    next = GetDirectory(builder.ToString(), pathSplit[i]);
                else
                    next = GetTarget(builder.ToString(), pathSplit[i]);
                next = next ?? pathSplit[i];

                if (i != 1 || !unixRooted)
                    builder.Append(separator);

                builder.Append(next);
            }

            return cachedPaths[path] = builder.ToString();
        }

    }
}
