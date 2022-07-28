using System.Collections.Generic;

namespace Hack2022
{
    public static class Helper
    {
        public static List<string> GetRequiredDirectoryNames()
        {
            return new List<string> { Constants.EssentialCPDirectoryName, Constants.RiposteCoreDirectoryName };
        }

        public static string CombinePath(string directory, string childPathOrNode)
        {
            string first = directory.TrimEnd('\\');
            string second = childPathOrNode.TrimStart('\\').TrimEnd('\\');
            return first + @"\" + second;
        }
    }
}
