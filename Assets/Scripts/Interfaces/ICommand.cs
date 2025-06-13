using UnityEngine;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for command pattern to encapsulate actions
    /// </summary>
    public interface ICommand
    {
        void Execute();
        void Undo();
        bool CanExecute();
        string GetDescription();
    }
}