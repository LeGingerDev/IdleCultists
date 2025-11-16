using System.Collections;

namespace LGD.Tasks
{
    public interface ITask
    {
        public IEnumerator ExecuteInternal();
        public bool CanExecute();
        public bool IsFinished();
    }
}