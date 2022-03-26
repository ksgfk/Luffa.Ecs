namespace Luffa.Ecs
{
    public interface ISystem
    {
        IEntityFilter Filter { get; }

        void OnUpdate();
    }

    public abstract class SystemBase : ISystem
    {
        public abstract IEntityFilter Filter { get; }

        protected SystemBase() { }

        public void OnUpdate()
        {
            foreach (var memory in Filter.MatchedEntity)
            {
                UpdateEntity(memory);
            }
        }

        protected abstract void UpdateEntity(EntityMemory memory);

        protected ComponentViewer GetComponentViewer(EntityMemory memory)
        {
            return memory.GetViewer();
        }

        protected UnmanagedComponentLocator<T> GetUnmanagedLocator<T>(EntityMemory memory) where T : unmanaged, IComponent
        {
            return memory.GetUnmanagedComponentLocator<T>();
        }

        protected ManagedComponentLocator<T> GetManagedLocator<T>(EntityMemory memory) where T : IComponent
        {
            return memory.GetManagedComponentLocator<T>();
        }
    }
}
