using System.Collections.Generic;

namespace Luffa.Ecs
{
    public enum WorldCmd
    {
        Create,
        Destroy,
        AddComponent,
        RemoveComponent
    }

    public class CmdEntry
    {
        public WorldCmd Cmd { get; internal set; }
        public EntityArchetype? CreateType { get; internal set; }
        public EntityHandle Target { get; internal set; }
        public ComponentType AddType { get; internal set; }
        public ComponentType RemoveType { get; internal set; }
    }

    public class CmdBuffer
    {
        private readonly List<CmdEntry> _buffer;

        public List<CmdEntry> Buffer => _buffer;

        public CmdBuffer()
        {
            _buffer = new List<CmdEntry>();
        }

        public void CreateEntity(EntityArchetype arch)
        {
            _buffer.Add(new CmdEntry() { Cmd = WorldCmd.Create, CreateType = arch });
        }

        public void DestroyEntity(EntityHandle entity)
        {
            _buffer.Add(new CmdEntry() { Cmd = WorldCmd.Destroy, Target = entity });
        }

        public void AddComponent(EntityHandle entity, ComponentType type)
        {
            _buffer.Add(new CmdEntry() { Cmd = WorldCmd.AddComponent, Target = entity, AddType = type });
        }

        public void RemoveComponent(EntityHandle entity, ComponentType type)
        {
            _buffer.Add(new CmdEntry() { Cmd = WorldCmd.RemoveComponent, Target = entity, AddType = type });
        }

        public void Clear()
        {
            _buffer.Clear();
        }
    }
}
