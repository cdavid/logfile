using System;

namespace Shared
{
    public static class LogSettings
    {
        public static string FILE_PATH = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\datadog\file.log";
    }
}
