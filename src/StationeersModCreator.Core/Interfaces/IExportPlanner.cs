using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IExportPlanner
{
    IReadOnlyList<ExportPlanEntry> Plan(ModProject project);
}
