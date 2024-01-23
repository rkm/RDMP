﻿// Copyright (c) The University of Dundee 2024-2024
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NLog;
using Rdmp.Core.ReusableLibraryCode.Settings;
using System;
using System.IO;

namespace Rdmp.Core.Logging;


/// <summary>
/// Singleton logger for writing logs to file
/// </summary>
public class FileSystemLogger
{

    private static readonly Lazy<FileSystemLogger> lazy =
         new Lazy<FileSystemLogger>(() => new FileSystemLogger());

    public static FileSystemLogger Instance { get { return lazy.Value; } }


    public enum AvailableLoggers
    {
        ProgressLog,
        DataSource,
        FatalError
    }



    private FileSystemLogger()
    {
        var location = UserSettings.FileSystemLogLocation;
        var config = new NLog.Config.LoggingConfiguration();
        string[] logs = System.Enum.GetNames(typeof(AvailableLoggers));
        foreach (var log in logs){
            using var nLogEntry = new NLog.Targets.FileTarget(log) { FileName = Path.Combine(location, $"{log}.log"), ArchiveAboveSize = UserSettings.LogFileSizeLimit };
            config.AddRule(LogLevel.Info, LogLevel.Info, nLogEntry);
        }
        NLog.LogManager.Configuration = config;
    }

    public void LogEventToFile(AvailableLoggers logType, object[] logItems)
    {
        var logMessage = $"{string.Join("|", Array.ConvertAll(logItems, item => item.ToString()))}";
        var Logger = NLog.LogManager.GetLogger(Enum.GetName(typeof(AvailableLoggers), logType));
        Logger.Info(logMessage);
    }


}