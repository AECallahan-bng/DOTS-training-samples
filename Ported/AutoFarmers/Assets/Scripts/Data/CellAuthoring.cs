using Unity.Entities;

namespace AutoFarmers
{
    [GenerateAuthoringComponent]
    struct Cell : IComponentData
    {
        public CellType Type;
    }
}