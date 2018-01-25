using System;

namespace Uploader.Models
{
    public class ProcessItem
    {
        public ProcessItem()
        {
            CurrentStep = ProcessStep.Init;
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
                if(CurrentStep < ProcessStep.Waiting)
                    return null;

                if(StartProcess == DateTime.MinValue)
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

                if(EndProcess == DateTime.MinValue)
                    return (long)(DateTime.UtcNow - StartProcess).TotalSeconds;

                return (long)(EndProcess - StartProcess).TotalSeconds;
            }
        }

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

        public void Cancel()
        {
            Progress = null;
            LastTimeProgressChanged = null;
            
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

        public void SetErrorMessage(string message)
        {
            ErrorMessage = message;

            EndProcess = DateTime.UtcNow;
            CurrentStep = ProcessStep.Error;
        }
        
        public void EndProcessDateTime()
        {
            SetProgress("100.00%");

            EndProcess = DateTime.UtcNow;
            CurrentStep = ProcessStep.Success;
        }
    }
}