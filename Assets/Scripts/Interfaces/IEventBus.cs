using UnityEngine;
using System;

namespace SkillTreeSurvivor
{
    /// <summary>
    /// Interface for an event bus to handle event publishing and subscription
    /// </summary>
    public interface IEventBus
    {
        void Publish<T>(T eventData) where T : class;
        void Subscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action<T> handler) where T : class;
        void UnsubscribeAll();
    }
}
