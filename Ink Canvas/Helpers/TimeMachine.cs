using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
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
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
            _currentIndex = _currentStrokeHistory.Count - 1;
            OnUndoStateChanged?.Invoke(true);
            OnRedoStateChanged?.Invoke(false);
        }
        
        public void CommitStrokeShapeHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke)
        {
            _currentStrokeHistory.Add(new TimeMachineHistory(generatedStroke,
                TimeMachineHistoryType.ShapeRecognition,
                false,
                strokeToBeReplaced));
            _currentIndex = _currentStrokeHistory.Count - 1;
            OnUndoStateChanged?.Invoke(true);
            OnRedoStateChanged?.Invoke(false);
        }

        public void CommitStrokeEraseHistory(StrokeCollection stroke)
        {
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
            _currentIndex = _currentStrokeHistory.Count - 1;
            OnUndoStateChanged?.Invoke(true);
            OnRedoStateChanged?.Invoke(false);
        }

        public void ClearStrokeHistory()
        {
            _currentStrokeHistory.Clear();
            _currentIndex = -1;
            OnUndoStateChanged?.Invoke(true);
            OnRedoStateChanged?.Invoke(false);
        }
        public TimeMachineHistory Undo()
        {
            var item = _currentStrokeHistory[_currentIndex];
            item.IsReversed = !item.IsReversed;
            _currentIndex--;
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
            return item;
        }

        public TimeMachineHistory Redo()
        {
            var item = _currentStrokeHistory[++_currentIndex];
            item.IsReversed = !item.IsReversed;
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            if (_currentIndex != -1) OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
            return item;
        }
        public List<TimeMachineHistory> ExportTimeMachineHistory()
        {
            throw new System.Exception();
        }
        public bool ImportTimeMachineHistory()
        {
            throw new System.Exception();
        }
    }

    public class TimeMachineHistory
    {
        public TimeMachineHistoryType CommitType;
        public bool IsReversed;
        public StrokeCollection CurrentStroke;
        public StrokeCollection ShapeRecognitionReplacedStroke;
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool isReversed)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            IsReversed = isReversed;
            ShapeRecognitionReplacedStroke = null;
        }
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool isReversed , StrokeCollection shapeRecognitionReplacedStroke)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            IsReversed = isReversed;
            ShapeRecognitionReplacedStroke = shapeRecognitionReplacedStroke;
        }
    }

    public enum TimeMachineHistoryType
    {
        UserInput,
        ShapeRecognition
    }
}