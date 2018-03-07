using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Uploader.Core.Managers.Common;

namespace Uploader.Core.Models
{
    internal class ProcessItem
    {
        public ProcessItem(FileItem fileItem, ILogger logger)
        {
            CurrentStep = ProcessStep.Init;
            FileItem = fileItem;
            Logger = logger;
            CreationDate = DateTime.UtcNow;
        }

        public FileItem FileItem
        {
            get;
            private set;
        }

        public ILogger Logger
        {
            get;
            private set;
        }

        public bool CantCascadeCancel
        {
            get;
            set;
        }
        
        public DateTime CreationDate
        {
            get;
        }

        /// <summary>
        /// Date d'inscription dans la queue à son enregistrement
        /// </summary>
        /// <returns></returns>
        public DateTime DateInQueue
        {
            get;
            private set;
        }

        /// <summary>
        /// Numéro attribué dans la queue à son enregistrement
        /// </summary>
        /// <returns></returns>
        public int PositionInQueue
        {
            get;
            private set;
        }

        /// <summary>
        /// Position d'attente dans la queue à son enregistrement
        /// </summary>
        /// <returns></returns>
        public int OriginWaitingPositionInQueue
        {
            get;
            private set;
        }

        public long? WaitingTime
        {
            get 
            {
                if(CurrentStep == ProcessStep.Init || CurrentStep == ProcessStep.Canceled)
                    return null;

                if(CurrentStep == ProcessStep.Waiting)
                    return (long)(DateTime.UtcNow - DateInQueue).TotalSeconds;

                return (long)(StartProcess - DateInQueue).TotalSeconds;
            }
        }

        public long? ProcessTime
        {
            get 
            {
                if(CurrentStep < ProcessStep.Started)
                    return null;

                if(CurrentStep == ProcessStep.Started)
                    return (long)(DateTime.UtcNow - StartProcess).TotalSeconds;

                return (long)(EndProcess - StartProcess).TotalSeconds;
            }
        }

        public DateTime LastActivityDateTime => Tools.Max(CreationDate, DateInQueue, StartProcess, EndProcess);

        public DateTime StartProcess
        {
            get;
            private set;
        }

        public DateTime EndProcess
        {
            get;
            private set;
        }

        public string Progress
        {
            get;
            private set;
        }

        public DateTime? LastTimeProgressChanged
        {
            get;
            private set;
        }

        public ProcessStep CurrentStep
        {
            get;
            private set;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }

        public void SavePositionInQueue(int totalAddToQueue, int currentPositionInQueue)
        {
            PositionInQueue = totalAddToQueue;
            OriginWaitingPositionInQueue = totalAddToQueue - currentPositionInQueue;

            DateInQueue = DateTime.UtcNow;
            CurrentStep = ProcessStep.Waiting;
        }

        public bool Unstarted()
        {
            return CurrentStep == ProcessStep.Init || CurrentStep == ProcessStep.Waiting;
        }

        public bool Finished()
        {
            return CurrentStep == ProcessStep.Success || CurrentStep == ProcessStep.Error || CurrentStep == ProcessStep.Canceled;
        }

        public bool CanProcess()
        {
            return !FileItem.FileContainer.MustAbort() && CurrentStep == ProcessStep.Waiting;
        }

        public void CancelCascade(string shortMessage, string longMessage)
        {
            Cancel(shortMessage, longMessage);
            FileItem.FileContainer.CancelAll(shortMessage);
        }

        public void Cancel(string shortMessage, string longMessage)
        {           
            LogManager.Log(Logger, LogLevel.Warning, longMessage, "Annulation");
            Cancel(shortMessage);
        }

        public void CancelUnstarted(string message)
        {
            if(!Unstarted())
                return;
                
            Cancel(message);
        }

        private void Cancel(string message)
        {
            Progress = null;
            LastTimeProgressChanged = null;
            ErrorMessage = message;
            CurrentStep = ProcessStep.Canceled;
        }

        public void StartProcessDateTime()
        {
            SetProgress("0.00%");

            StartProcess = DateTime.UtcNow;
            CurrentStep = ProcessStep.Started;
        }

        public void SetProgress(string progress, bool initMessage = false)
        {
            Progress = progress;

            if(!initMessage)
                LastTimeProgressChanged = DateTime.UtcNow;
        }

        public void SetErrorMessage(string shortMessage, string longMessage)
        {
            LogManager.Log(Logger, LogLevel.Error, longMessage, "Error");

            ErrorMessage = shortMessage;

            EndProcess = DateTime.UtcNow;
            CurrentStep = ProcessStep.Error;

            FileItem.FileContainer.CancelAll(shortMessage);
        }

        public void SetErrorMessage(string shortMessage, string longMessage, Exception exception)
        {
            LogManager.Log(Logger, LogLevel.Critical, longMessage, "Exception", exception);

            ErrorMessage = shortMessage;

            EndProcess = DateTime.UtcNow;
            CurrentStep = ProcessStep.Error;

            FileItem.FileContainer.ExceptionDetail = exception.ToString();
            FileItem.FileContainer.CancelAll(shortMessage);
        }
        
        public void EndProcessDateTime()
        {
            SetProgress("100.00%");

            EndProcess = DateTime.UtcNow;
            CurrentStep = ProcessStep.Success;
        }
    }
}