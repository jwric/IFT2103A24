using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Client.Managers
{
    
    public class LoadingTask
    {
        public string Message { get; set; } // Display message for this task
        public Func<Action<bool, string>, IEnumerator> Task { get; set; } // Coroutine with callback for result
        public bool IsCritical { get; set; } // If true, failure stops the loading process

        public LoadingTask(string message, Func<Action<bool, string>, IEnumerator> task, bool isCritical = true)
        {
            Message = message;
            Task = task;
            IsCritical = isCritical;
        }
    }

    
    public struct LoadingResult
    {
        public bool Success { get; }
        public string ErrorMessage { get; }

        public LoadingResult(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
    
    public class LoadingManager
    {
        private GameManager gameManager;

        public LoadingManager(GameManager gameManager)
        {
            this.gameManager = gameManager;
        }

        /// <summary>
        /// Starts the loading process with the specified tasks.
        /// </summary>
        public void StartLoading(List<LoadingTask> tasks, Action<LoadingResult> onComplete = null)
        {
            gameManager.StartCoroutine(ExecuteTasks(tasks, onComplete));
        }

        /// <summary>
        /// Executes the list of tasks sequentially.
        /// </summary>
        private IEnumerator ExecuteTasks(List<LoadingTask> tasks, Action<LoadingResult> onComplete)
        {
            // Show the loading screen
            gameManager.UIManager.ShowLoadingScreen();

            bool success = true;
            string errorMessage = string.Empty;

            foreach (var task in tasks)
            {
                // Update the UI with the task's message
                gameManager.UIManager.UpdateLoadingMessage(task.Message);

                bool taskSuccess = true;
                string taskErrorMessage = string.Empty;

                // Use a callback to get the task result
                yield return task.Task((result, message) =>
                {
                    taskSuccess = result;
                    taskErrorMessage = message;
                });

                if (!taskSuccess)
                {
                    Debug.LogError($"Task failed: {task.Message}. Error: {taskErrorMessage}");

                    if (task.IsCritical)
                    {
                        success = false;
                        errorMessage = taskErrorMessage;
                        break;
                    }
                }
            }

            // Hide the loading screen
            gameManager.UIManager.HideLoadingScreen();

            // Callback with the result
            onComplete?.Invoke(new LoadingResult(success, errorMessage));
        }

    }
}