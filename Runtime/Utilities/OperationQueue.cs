using System;
using System.Collections.Generic;

namespace Aviad
{
    /// <summary>
    /// Operation queue that ensures actions run in strict order
    /// and only advance when their associated callback with a matching ID is called.
    /// </summary>
    public class OperationQueue
    {
        private Queue<(Guid Id, Action Action)> _operationQueue = new Queue<(Guid, Action)>();
        private bool _isProcessing = false;
        private Guid? _currentActionId = null;

        public bool IsProcessing => _isProcessing || _operationQueue.Count > 0;

        /// <summary>
        /// Get a new unique ID for linking an action and its completion callback.
        /// </summary>
        public Guid GetNewId()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        /// Wraps a callback so that it only triggers the next operation if its ID matches the current operation.
        /// Works with callbacks that take parameters.
        /// </summary>
        public Action<T> WrapAction<T>(Guid id, Action<T> callback = null)
        {
            return (arg) =>
            {
                try
                {
                    callback?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    PackageLogger.Error($"Exception in wrapped callback for ID {id}: {ex}");
                }
                finally
                {
                    // Always try to process next, even if callback failed
                    ProcessNext(id);
                }
            };
        }

        /// <summary>
        /// Wraps a parameterless callback so that it only triggers the next operation if its ID matches the current operation.
        /// </summary>
        public Action WrapAction(Guid id, Action callback = null)
        {
            return () =>
            {
                try
                {
                    callback?.Invoke();
                }
                catch (Exception ex)
                {
                    PackageLogger.Error($"Exception in wrapped callback for ID {id}: {ex}");
                }
                finally
                {
                    // Always try to process next, even if callback failed
                    ProcessNext(id);
                }
            };
        }

        /// <summary>
        /// Adds an action to the queue, ensuring strict order of execution.
        /// </summary>
        public void HandleOrderedAction(Guid id, Action action)
        {
            if (action == null)
            {
                PackageLogger.Error("Cannot handle null action in OperationQueue");
                return;
            }

            if (id == Guid.Empty)
            {
                PackageLogger.Warning("Using empty GUID for operation queue action");
            }

            if (IsProcessing)
            {
                _operationQueue.Enqueue((id, action));
            }
            else
            {
                _isProcessing = true;
                _currentActionId = id;
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    PackageLogger.Error($"Exception executing action with ID {id}: {ex}");
                    ProcessNext(id);
                }
            }
        }

        private void ProcessNext(Guid id)
        {
            if (_currentActionId != id)
            {
                // Wrong ID, ignore
                PackageLogger.Warning("OperationQueue received callbacks out of order. This may cause a stall or out-of-order execution.");
                return;
            }

            _isProcessing = false;
            _currentActionId = null;

            if (_operationQueue.Count > 0)
            {
                var (nextId, nextAction) = _operationQueue.Dequeue();
                _isProcessing = true;
                _currentActionId = nextId;
                try
                {
                    nextAction();
                }
                catch (Exception ex)
                {
                    PackageLogger.Error($"Exception executing action with ID {id}: {ex}");
                    ProcessNext(nextId);
                }
            }
        }
    }
}