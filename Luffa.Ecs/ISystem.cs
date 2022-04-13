namespace Luffa.Ecs
{
    public interface ISystem
    {
        IEntityFilter Filter { get; }

        void OnUpdate(World world, CmdBuffer cmd);
    }

    public abstract class SystemBase : ISystem
    {
        public abstract IEntityFilter Filter { get; }

        protected SystemBase() { }

        public void OnUpdate(World world, CmdBuffer cmd)
        {
            foreach (var arch in Filter.MatchedArchetype)
            {
                if (world.TryGetEntityMemory(arch, out EntityMemory memory))
                {
                    UpdateEntity(world, memory, cmd);
                }
            }
        }

        protected abstract void UpdateEntity(World world, EntityMemory memory, CmdBuffer cmd);

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

        protected UnmanagedComponentLocator<EntityMemory.Info> GetEntityLocator(EntityMemory memory)
        {
            return memory.GetEntityLocator();
        }
    }
}
