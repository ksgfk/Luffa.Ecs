namespace Luffa.Ecs
{
    public readonly struct EntityHandle
    {
        public readonly int Index;
        public readonly int Version;

        public EntityHandle(int index, int version)
        {
            Index = index;
            Version = version;
        }
    }
}
