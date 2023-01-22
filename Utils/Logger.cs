using System;
using Playnite.SDK;

namespace Bangumi.Utils
{
    public class Logger : ILogger
    {
        private ILogger logger;
        private bool enabled;
        
        public Logger(ILogger logger, bool enabled)
        {
            this.logger = logger;
            this.enabled = enabled;
        }
        
        public void Info(string message)
        {
            if(enabled) logger.Info(message);
        }

        public void Info(Exception exception, string message)
        {
            if(enabled) logger.Info(exception, message);
        }

        public void Debug(string message)
        {
            if(enabled) logger.Debug(message);
        }

        public void Debug(Exception exception, string message)
        {
            if(enabled) logger.Debug(exception, message);
        }

        public void Warn(string message)
        {
            if(enabled) logger.Warn(message);
        }

        public void Warn(Exception exception, string message)
        {
            if(enabled) logger.Warn(exception, message);
        }

        public void Error(string message)
        {
            if(enabled) logger.Error(message);
        }

        public void Error(Exception exception, string message)
        {
            if(enabled) logger.Error(exception, message);
        }

        public void Trace(string message)
        {
            if(enabled) logger.Trace(message);
        }

        public void Trace(Exception exception, string message)
        {
            if(enabled) logger.Trace(exception, message);
        }
    }
}