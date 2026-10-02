using StationeersModCreator.Core.Models;

namespace StationeersModCreator.Core.Interfaces;
public interface IProjectValidator
{
    void Validate(ModProject project, bool requireChanges);
}
