//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using log4net;
using log4net.Core;
using log4net.Util;
using System;
using System.Globalization;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Extensions for log4net to expose the following additional log message severity levels:
    /// NOTICE, TRACE, and VERBOSE.
    /// </summary>
    public static class Log4NetExtensions
    {
        #region NOTICE

        /// <summary>
        /// Checks if this logger is enabled for the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <returns><c>true</c> if <c>NOTICE</c> is enabled; otherwise, <c>false</c>.</returns>
        public static bool IsNoticeEnabled(this ILog logger)
        {
            return logger.Logger.IsEnabledFor(Level.Notice);
        }

        /// <summary>
        /// Logs a message object with the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="message">The message object to log.</param>
        public static void Notice(this ILog logger, object message)
        {
            logger.Logger.Log(typeof(LogImpl), Level.Notice, message, null);
        }

        /// <summary>
        /// Logs a message object with the <c>NOTICE</c> level
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="message">The message object to log.</param>
        /// <param name="exception">The exception to log, including its stack trace.</param>
        public static void Notice(this ILog logger, object message, Exception exception)
        {
            logger.Logger.Log(typeof(LogImpl), Level.Notice, message, exception);
        }

        /// <summary>
        /// Logs a formatted message string with the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">An Object array containing zero or more objects to format</param>
        public static void NoticeFormat(this ILog logger, string format, params object[] args)
        {
            if (logger.IsNoticeEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Notice, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        public static void NoticeFormat(this ILog logger, string format, object arg0)
        {
            if (logger.IsNoticeEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Notice, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        /// <param name="arg1">An Object to format</param>
        public static void NoticeFormat(this ILog logger, string format, object arg0, object arg1)
        {
            if (logger.IsNoticeEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Notice, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        /// <param name="arg1">An Object to format</param>
        /// <param name="arg2">An Object to format</param>
        public static void NoticeFormat(this ILog logger, string format, object arg0, object arg1, object arg2)
        {
            if (logger.IsNoticeEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Notice, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>NOTICE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">An Object array containing zero or more objects to format</param>
        public static void NoticeFormat(this ILog logger, IFormatProvider provider, string format, params object[] args)
        {
            if (logger.IsNoticeEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Notice, new SystemStringFormat(provider, format, args), null);
            }
        }

        #endregion // NOTICE

        #region TRACE

        /// <summary>
        /// Checks if this logger is enabled for the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <returns><c>true</c> if <c>TRACE</c> is enabled; otherwise, <c>false</c>.</returns>
        public static bool IsTraceEnabled(this ILog logger)
        {
            return logger.Logger.IsEnabledFor(Level.Trace);
        }

        /// <summary>
        /// Logs a message object with the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="message">The message object to log.</param>
        public static void Trace(this ILog logger, object message)
        {
            logger.Logger.Log(typeof(LogImpl), Level.Trace, message, null);
        }

        /// <summary>
        /// Logs a message object with the <c>TRACE</c> level
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="message">The message object to log.</param>
        /// <param name="exception">The exception to log, including its stack trace.</param>
        public static void Trace(this ILog logger, object message, Exception exception)
        {
            logger.Logger.Log(typeof(LogImpl), Level.Trace, message, exception);
        }

        /// <summary>
        /// Logs a formatted message string with the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">An Object array containing zero or more objects to format</param>
        public static void TraceFormat(this ILog logger, string format, params object[] args)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Trace, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        public static void TraceFormat(this ILog logger, string format, object arg0)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Trace, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        /// <param name="arg1">An Object to format</param>
        public static void TraceFormat(this ILog logger, string format, object arg0, object arg1)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Trace, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        /// <param name="arg1">An Object to format</param>
        /// <param name="arg2">An Object to format</param>
        public static void TraceFormat(this ILog logger, string format, object arg0, object arg1, object arg2)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Trace, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>TRACE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">An Object array containing zero or more objects to format</param>
        public static void TraceFormat(this ILog logger, IFormatProvider provider, string format, params object[] args)
        {
            if (logger.IsTraceEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Trace, new SystemStringFormat(provider, format, args), null);
            }
        }

        #endregion // TRACE

        #region VERBOSE

        /// <summary>
        /// Checks if this logger is enabled for the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <returns><c>true</c> if <c>VERBOSE</c> is enabled; otherwise, <c>false</c>.</returns>
        public static bool IsVerboseEnabled(this ILog logger)
        {
            return logger.Logger.IsEnabledFor(Level.Verbose);
        }

        /// <summary>
        /// Logs a message object with the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="message">The message object to log.</param>
        public static void Verbose(this ILog logger, object message)
        {
            logger.Logger.Log(typeof(LogImpl), Level.Verbose, message, null);
        }

        /// <summary>
        /// Logs a message object with the <c>VERBOSE</c> level
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="message">The message object to log.</param>
        /// <param name="exception">The exception to log, including its stack trace.</param>
        public static void Verbose(this ILog logger, object message, Exception exception)
        {
            logger.Logger.Log(typeof(LogImpl), Level.Verbose, message, exception);
        }

        /// <summary>
        /// Logs a formatted message string with the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">An Object array containing zero or more objects to format</param>
        public static void VerboseFormat(this ILog logger, string format, params object[] args)
        {
            if (logger.IsVerboseEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Verbose, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        public static void VerboseFormat(this ILog logger, string format, object arg0)
        {
            if (logger.IsVerboseEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Verbose, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        /// <param name="arg1">An Object to format</param>
        public static void VerboseFormat(this ILog logger, string format, object arg0, object arg1)
        {
            if (logger.IsVerboseEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Verbose, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="arg0">An Object to format</param>
        /// <param name="arg1">An Object to format</param>
        /// <param name="arg2">An Object to format</param>
        public static void VerboseFormat(this ILog logger, string format, object arg0, object arg1, object arg2)
        {
            if (logger.IsVerboseEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Verbose, new SystemStringFormat(CultureInfo.InvariantCulture, format, new object[] { arg0, arg1, arg2 }), null);
            }
        }

        /// <summary>
        /// Logs a formatted message string with the <c>VERBOSE</c> level.
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information</param>
        /// <param name="format">A String containing zero or more format items</param>
        /// <param name="args">An Object array containing zero or more objects to format</param>
        public static void VerboseFormat(this ILog logger, IFormatProvider provider, string format, params object[] args)
        {
            if (logger.IsVerboseEnabled())
            {
                logger.Logger.Log(typeof(LogImpl), Level.Verbose, new SystemStringFormat(provider, format, args), null);
            }
        }

        #endregion // VERBOSE
    }
}
