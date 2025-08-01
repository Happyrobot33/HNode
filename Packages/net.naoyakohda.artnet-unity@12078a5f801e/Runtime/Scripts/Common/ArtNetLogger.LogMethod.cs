﻿// <auto-generated>
//     This code was auto generated by T4 template.
//     DO NOT EDIT THIS FILE MANUALLY.
// </auto-generated>

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ArtNet.Common
{
    public static partial class ArtNetLogger
    {

        // Debug
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug(string message)
        {
            InternalLog(LogLevel.Debug, DefaultTag, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug(string tag, string message)
        {
            InternalLog(LogLevel.Debug, tag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogDebug(string message)
        {
            InternalLog(LogLevel.Debug, DefaultTag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogDebug(string tag, string message)
        {
            InternalLog(LogLevel.Debug, tag, message);
        }

        // Info
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogInfo(string message)
        {
            InternalLog(LogLevel.Info, DefaultTag, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogInfo(string tag, string message)
        {
            InternalLog(LogLevel.Info, tag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogInfo(string message)
        {
            InternalLog(LogLevel.Info, DefaultTag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogInfo(string tag, string message)
        {
            InternalLog(LogLevel.Info, tag, message);
        }

        // Warn
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarn(string message)
        {
            InternalLog(LogLevel.Warn, DefaultTag, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarn(string tag, string message)
        {
            InternalLog(LogLevel.Warn, tag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogWarn(string message)
        {
            InternalLog(LogLevel.Warn, DefaultTag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogWarn(string tag, string message)
        {
            InternalLog(LogLevel.Warn, tag, message);
        }

        // Error
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string message)
        {
            InternalLog(LogLevel.Error, DefaultTag, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string tag, string message)
        {
            InternalLog(LogLevel.Error, tag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogError(string message)
        {
            InternalLog(LogLevel.Error, DefaultTag, message);
        }

        [DebuggerStepThrough, Conditional("ART_NET_DEVELOP_LOG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DevLogError(string tag, string message)
        {
            InternalLog(LogLevel.Error, tag, message);
        }
    }
}
