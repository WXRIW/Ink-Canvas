using System.Collections.Generic;
using System.Windows.Ink;

namespace Ink_Canvas.Helpers
{
    public class TimeMachine
    {
        private readonly List<TimeMachineHistory> _currentStrokeHistory = new List<TimeMachineHistory>();

        private int _currentIndex = -1;

        public delegate void OnUndoStateChange(bool status);

        public event OnUndoStateChange OnUndoStateChanged;

        public delegate void OnRedoStateChange(bool status);

        public event OnRedoStateChange OnRedoStateChanged;

        public void CommitStrokeUserInputHistory(StrokeCollection stroke)
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, (_currentStrokeHistory.Count - 1) - _currentIndex);
            }
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeShapeHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke)
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, (_currentStrokeHistory.Count - 1) - _currentIndex);
            }
            _currentStrokeHistory.Add(new TimeMachineHistory(generatedStroke,
                TimeMachineHistoryType.ShapeRecognition,
                false,
                strokeToBeReplaced));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeManipulationHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke)
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, (_currentStrokeHistory.Count - 1) - _currentIndex);
            }
            _currentStrokeHistory.Add(new TimeMachineHistory(generatedStroke,
                TimeMachineHistoryType.Manipulation,
                false,
                strokeToBeReplaced));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeEraseHistory(StrokeCollection stroke, StrokeCollection sourceStroke = null)
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, (_currentStrokeHistory.Count - 1) - _currentIndex);
            }
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.Clear, true, sourceStroke));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void ClearStrokeHistory()
        {
            _currentStrokeHistory.Clear();
            _currentIndex = -1;
            NotifyUndoRedoState();
        }

        public TimeMachineHistory Undo()
        {
            var item = _currentStrokeHistory[_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            _currentIndex--;
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
            return item;
        }

        public TimeMachineHistory Redo()
        {
            var item = _currentStrokeHistory[++_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            NotifyUndoRedoState();
            return item;
        }

        public TimeMachineHistory[] ExportTimeMachineHistory()
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, (_currentStrokeHistory.Count - 1) - _currentIndex);
            }
            return _currentStrokeHistory.ToArray();
        }

        public bool ImportTimeMachineHistory(TimeMachineHistory[] sourceHistory)
        {
            _currentStrokeHistory.Clear();
            _currentStrokeHistory.AddRange(sourceHistory);
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
            return true;
        }
        private void NotifyUndoRedoState()
        {
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
        }
    }

    public class TimeMachineHistory
    {
        public TimeMachineHistoryType CommitType;
        public bool StrokeHasBeenCleared;
        public StrokeCollection CurrentStroke;
        public StrokeCollection ReplacedStroke;
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = null;
        }
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared, StrokeCollection replacedStroke)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = replacedStroke;
        }
    }

    public enum TimeMachineHistoryType
    {
        UserInput,
        ShapeRecognition,
        Clear,
        Manipulation
    }
}